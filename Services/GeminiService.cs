using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;

namespace FitnessCenter.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly IMemoryCache _cache;
        private static readonly Dictionary<string, DateTime> _requestTimestamps = new();
        private static readonly object _lockObject = new();
        private const int MAX_REQUESTS_PER_MINUTE = 5; // Dakikada maksimum istek sayısı (kota hatasını önlemek için azaltıldı)

        public GeminiService(HttpClient httpClient, IConfiguration configuration, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _cache = cache;
            _apiKey = configuration["Gemini:ApiKey"] ?? throw new ArgumentNullException(nameof(configuration), "Gemini API key not found");
            
            // API anahtarının boş olmadığını kontrol et
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new ArgumentException("Gemini API key boş olamaz. Lütfen appsettings.json dosyasında 'Gemini:ApiKey' değerini kontrol edin.");
            }
        }

        public string GetApiKeyPreview()
        {
            if (string.IsNullOrEmpty(_apiKey))
                return "API anahtarı bulunamadı";
            
            // İlk 10 ve son 4 karakteri göster
            if (_apiKey.Length > 14)
                return $"{_apiKey.Substring(0, 10)}...{_apiKey.Substring(_apiKey.Length - 4)}";
            
            return "***";
        }

        public async Task<string> ListAvailableModels()
        {
            // Hem v1 hem de v1beta sürümlerini dene
            var apiVersions = new[] { "v1", "v1beta" };
            
            foreach (var apiVersion in apiVersions)
            {
                try
                {
                    var url = $"https://generativelanguage.googleapis.com/{apiVersion}/models?key={_apiKey}";
                    var response = await _httpClient.GetAsync(url);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        // v1beta başarısız olursa v1'i dene
                        if (apiVersion == "v1beta" && response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            continue;
                        }
                        throw new HttpRequestException($"Gemini API hatası ({apiVersion}): {response.StatusCode} - {errorContent}");
                    }
                }
                catch (HttpRequestException) when (apiVersion == "v1beta")
                {
                    // v1beta başarısız olursa v1'i dene
                    continue;
                }
            }
            
            throw new InvalidOperationException("Modeller listelenemedi. API anahtarınızı kontrol edin.");
        }

        public async Task<List<string>> GetAvailableModelNames()
        {
            try
            {
                var modelsJson = await ListAvailableModels();
                var jsonDoc = JsonDocument.Parse(modelsJson);
                var modelNames = new List<string>();

                if (jsonDoc.RootElement.TryGetProperty("models", out var models))
                {
                    foreach (var model in models.EnumerateArray())
                    {
                        if (model.TryGetProperty("name", out var name))
                        {
                            var modelName = name.GetString();
                            if (!string.IsNullOrEmpty(modelName))
                            {
                                // "models/gemini-pro" formatından sadece "gemini-pro" kısmını al
                                var shortName = modelName.Contains('/') 
                                    ? modelName.Split('/').Last() 
                                    : modelName;
                                modelNames.Add(shortName);
                            }
                        }
                    }
                }

                return modelNames;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Model isimleri alınamadı: {ex.Message}", ex);
            }
        }

        private string GetCacheKey(string prompt)
        {
            // Prompt'un hash'ini alarak cache key oluştur
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(prompt));
            return $"gemini_response_{Convert.ToBase64String(hashBytes)}";
        }

        private bool CheckRateLimit()
        {
            lock (_lockObject)
            {
                var now = DateTime.UtcNow;
                // Son 1 dakikadaki istekleri temizle
                var keysToRemove = _requestTimestamps
                    .Where(kvp => (now - kvp.Value).TotalMinutes > 1)
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                foreach (var key in keysToRemove)
                {
                    _requestTimestamps.Remove(key);
                }
                
                // Maksimum istek sayısını kontrol et
                if (_requestTimestamps.Count >= MAX_REQUESTS_PER_MINUTE)
                {
                    return false; // Rate limit aşıldı
                }
                
                // Yeni isteği kaydet
                _requestTimestamps[Guid.NewGuid().ToString()] = now;
                return true;
            }
        }

        public async Task<string> GenerateTextResponse(string prompt)
        {
            // 1. Cache kontrolü - Aynı prompt için cache'den döndür
            var cacheKey = GetCacheKey(prompt);
            if (_cache.TryGetValue(cacheKey, out string? cachedResponse) && !string.IsNullOrEmpty(cachedResponse))
            {
                return cachedResponse;
            }

            // 2. Rate limiting kontrolü
            if (!CheckRateLimit())
            {
                throw new HttpRequestException(
                    "API istek limiti aşıldı. Lütfen bir dakika bekleyip tekrar deneyin. " +
                    $"Dakikada maksimum {MAX_REQUESTS_PER_MINUTE} istek yapılabilir.");
            }

            // 3. Retry mekanizması ile API çağrısı (daha uzun bekleme süreleri)
            const int maxRetries = 2; // Retry sayısını azalttık
            const int baseDelayMs = 5000; // 5 saniye (kota hatasını önlemek için artırıldı)
            
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    var response = await GenerateTextResponseInternal(prompt);
                    
                    // Başarılı yanıtı cache'e kaydet (24 saat süreyle - kota hatasını önlemek için artırıldı)
                    _cache.Set(cacheKey, response, TimeSpan.FromHours(24));
                    
                    return response;
                }
                catch (HttpRequestException ex) when (
                    ex.Message.Contains("Rate Limit") || 
                    ex.Message.Contains("429") || 
                    ex.Message.Contains("TooManyRequests") ||
                    ex.Message.Contains("quota"))
                {
                    if (attempt < maxRetries - 1)
                    {
                        // Exponential backoff: 2s, 4s, 8s
                        var delay = baseDelayMs * (int)Math.Pow(2, attempt);
                        await Task.Delay(delay);
                        continue; // Tekrar dene
                    }
                    else
                    {
                        // Son deneme de başarısız oldu
                        throw new HttpRequestException(
                            "API kotası aşıldı. Lütfen bir süre bekleyip tekrar deneyin veya Google AI Studio'dan kullanım limitlerinizi kontrol edin. " +
                            $"Detaylar: {ex.Message}");
                    }
                }
            }
            
            throw new InvalidOperationException("Beklenmeyen hata oluştu.");
        }

        private async Task<string> GenerateTextResponseInternal(string prompt)
        {
            // Mevcut modelleri kullan - API anahtarınızla erişilebilir modeller
            var modelConfigs = new List<(string apiVersion, string modelName)>
            {
                ("v1", "gemini-2.5-flash"),      // En yeni ve hızlı model
                ("v1beta", "gemini-2.5-flash"),  // Alternatif API versiyonu
                ("v1", "gemini-2.0-flash"),      // Alternatif model
                ("v1beta", "gemini-2.0-flash")   // Alternatif
            };

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Exception? lastException = null;

            foreach (var (apiVersion, modelName) in modelConfigs)
            {
                try
                {
                    var url = $"https://generativelanguage.googleapis.com/{apiVersion}/models/{modelName}:generateContent?key={_apiKey}";

                    var response = await _httpClient.PostAsync(url, content);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var responseJson = JsonDocument.Parse(responseContent);

                        if (responseJson.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                        {
                            var text = candidates[0]
                                .GetProperty("content")
                                .GetProperty("parts")[0]
                                .GetProperty("text")
                                .GetString();

                            if (!string.IsNullOrEmpty(text))
                            {
                                // Başarılı! Kullanılan modeli logla
                                System.Diagnostics.Debug.WriteLine($"Başarılı model: {apiVersion}/{modelName}");
                                return text;
                            }
                        }
                        else
                        {
                            // Başarılı yanıt ama candidates yok
                            lastException = new InvalidOperationException($"API yanıt verdi ancak candidates bulunamadı: {responseContent}");
                            continue;
                        }
                    }
                    else
                    {
                        // Hata yanıtı - detayları kaydet
                        var errorMsg = $"API hatası ({apiVersion}/{modelName}): {response.StatusCode} - {responseContent}";
                        System.Diagnostics.Debug.WriteLine(errorMsg);
                        
                        // 429 (Rate Limit) hatası - özel mesaj
                        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        {
                            throw new HttpRequestException(
                                $"Rate Limit Aşıldı: API kotanız dolmuş. " +
                                $"Lütfen https://ai.dev/usage?tab=rate-limit adresinden kullanımınızı kontrol edin. " +
                                $"Detaylar: {responseContent}");
                        }
                        
                        // 404 veya 400 hatası ise bir sonraki modeli dene
                        if (response.StatusCode == System.Net.HttpStatusCode.NotFound || 
                            response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                        {
                            lastException = new HttpRequestException(errorMsg);
                            continue;
                        }
                        // Diğer hatalar için exception fırlat (401, 403 gibi)
                        throw new HttpRequestException(errorMsg);
                    }
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("Rate Limit") || ex.Message.Contains("429") || ex.Message.Contains("TooManyRequests"))
                {
                    // Rate limit hatası - direkt fırlat, diğer modelleri denemeye gerek yok
                    throw;
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("404") || ex.Message.Contains("NotFound") || ex.Message.Contains("400"))
                {
                    // Bu model bulunamadı veya geçersiz, bir sonrakini dene
                    lastException = ex;
                    continue;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    continue;
                }
            }

            // Hiçbir model çalışmadı
            var errorDetails = lastException != null 
                ? $"Son hata: {lastException.Message}" 
                : "Hiçbir model denendi ancak hata mesajı alınamadı.";
            
            throw new InvalidOperationException(
                $"Tüm model kombinasyonları denendi ancak hiçbiri çalışmadı. {errorDetails} " +
                $"Lütfen API anahtarınızın geçerli olduğundan ve modellere erişim izni olduğundan emin olun. " +
                $"API anahtarınızı kontrol etmek için /Ai/ListModels endpoint'ini kullanabilirsiniz.");
        }

        public async Task<string> AnalyzeImage(string base64Image, string prompt)
        {
            // 1. Cache kontrolü - Aynı görüntü ve prompt için cache'den döndür
            var imageCacheKey = GetCacheKey(prompt + base64Image.Substring(0, Math.Min(100, base64Image.Length)));
            if (_cache.TryGetValue(imageCacheKey, out string? cachedResponse) && !string.IsNullOrEmpty(cachedResponse))
            {
                return cachedResponse;
            }

            // 2. Rate limiting kontrolü
            if (!CheckRateLimit())
            {
                throw new HttpRequestException(
                    "API istek limiti aşıldı. Lütfen bir dakika bekleyip tekrar deneyin. " +
                    $"Dakikada maksimum {MAX_REQUESTS_PER_MINUTE} istek yapılabilir.");
            }

            // 3. Retry mekanizması ile API çağrısı (daha uzun bekleme süreleri)
            const int maxRetries = 2; // Retry sayısını azalttık
            const int baseDelayMs = 5000; // 5 saniye (kota hatasını önlemek için artırıldı)
            
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    var response = await AnalyzeImageInternal(base64Image, prompt);
                    
                    // Başarılı yanıtı cache'e kaydet (12 saat süreyle - kota hatasını önlemek için artırıldı)
                    _cache.Set(imageCacheKey, response, TimeSpan.FromHours(12));
                    
                    return response;
                }
                catch (HttpRequestException ex) when (
                    ex.Message.Contains("Rate Limit") || 
                    ex.Message.Contains("429") || 
                    ex.Message.Contains("TooManyRequests") ||
                    ex.Message.Contains("quota"))
                {
                    if (attempt < maxRetries - 1)
                    {
                        // Exponential backoff: 5s, 10s (daha uzun bekleme)
                        var delay = baseDelayMs * (int)Math.Pow(2, attempt);
                        System.Diagnostics.Debug.WriteLine($"Rate limit hatası (image), {delay/1000} saniye bekleniyor...");
                        await Task.Delay(delay);
                        continue;
                    }
                    else
                    {
                        throw new HttpRequestException(
                            "API kotası aşıldı. Lütfen bir süre bekleyip tekrar deneyin veya Google AI Studio'dan kullanım limitlerinizi kontrol edin. " +
                            $"Detaylar: {ex.Message}");
                    }
                }
            }
            
            throw new InvalidOperationException("Beklenmeyen hata oluştu.");
        }

        private async Task<string> AnalyzeImageInternal(string base64Image, string prompt)
        {
            // Görüntü analizi için mevcut modelleri kullan
            var modelConfigs = new[]
            {
                ("v1", "gemini-2.5-flash"),      // En yeni ve hızlı model
                ("v1beta", "gemini-2.5-flash"),  // Alternatif API versiyonu
                ("v1", "gemini-2.0-flash"),      // Alternatif model
                ("v1beta", "gemini-2.0-flash")   // Alternatif
            };

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = prompt },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = "image/jpeg",
                                    data = base64Image
                                }
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Exception? lastException = null;

            foreach (var (apiVersion, modelName) in modelConfigs)
            {
                try
                {
                    var url = $"https://generativelanguage.googleapis.com/{apiVersion}/models/{modelName}:generateContent?key={_apiKey}";

                    var response = await _httpClient.PostAsync(url, content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var responseJson = JsonDocument.Parse(responseContent);

                        if (responseJson.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                        {
                            var text = candidates[0]
                                .GetProperty("content")
                                .GetProperty("parts")[0]
                                .GetProperty("text")
                                .GetString();

                            if (!string.IsNullOrEmpty(text))
                            {
                                return text;
                            }
                        }
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            lastException = new HttpRequestException($"Model bulunamadı: {apiVersion}/{modelName} - {errorContent}");
                            continue;
                        }
                        throw new HttpRequestException($"Gemini API hatası: {response.StatusCode} - {errorContent}");
                    }
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("404") || ex.Message.Contains("NotFound"))
                {
                    lastException = ex;
                    continue;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    continue;
                }
            }

            var errorDetails = lastException != null 
                ? $"Son hata: {lastException.Message}" 
                : "Hiçbir model denendi ancak hata mesajı alınamadı.";
            
            throw new InvalidOperationException(
                $"Tüm model kombinasyonları denendi ancak hiçbiri çalışmadı. {errorDetails}");
        }
    }
}