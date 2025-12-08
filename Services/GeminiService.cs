using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace FitnessCenter.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
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

        public async Task<string> GenerateTextResponse(string prompt)
        {
            // Önce mevcut modelleri al
            List<string>? availableModels = null;
            try
            {
                availableModels = await GetAvailableModelNames();
            }
            catch (Exception ex)
            {
                // Model listesi alınamazsa logla ve devam et
                System.Diagnostics.Debug.WriteLine($"Model listesi alınamadı: {ex.Message}");
            }

            // Mevcut modellere göre kombinasyonları oluştur
            var modelConfigs = new List<(string apiVersion, string modelName)>();
            
            if (availableModels != null && availableModels.Count > 0)
            {
                // Mevcut modellere göre kombinasyonlar oluştur - önce v1beta, sonra v1
                foreach (var model in availableModels)
                {
                    if (model.Contains("gemini"))
                    {
                        // Önce v1beta dene (genellikle daha yeni modeller için)
                        modelConfigs.Add(("v1beta", model));
                        // Sonra v1 dene
                        modelConfigs.Add(("v1", model));
                    }
                }
            }
            
            // Eğer hiç model bulunamadıysa, en yaygın çalışan kombinasyonları dene
            if (modelConfigs.Count == 0)
            {
                modelConfigs = new List<(string apiVersion, string modelName)>
                {
                    // En yaygın çalışan kombinasyonlar (öncelik sırasına göre)
                    ("v1beta", "gemini-1.5-flash"),
                    ("v1beta", "gemini-1.5-pro"),
                    ("v1", "gemini-1.5-flash"),
                    ("v1", "gemini-1.5-pro"),
                    ("v1beta", "gemini-pro"),
                    ("v1", "gemini-pro")
                };
            }

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
            // Görüntü analizi için desteklenen modeller
            var modelConfigs = new[]
            {
                ("v1beta", "gemini-1.5-flash"),
                ("v1beta", "gemini-1.5-pro"),
                ("v1", "gemini-1.5-flash"),
                ("v1", "gemini-1.5-pro"),
                ("v1", "gemini-pro")
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