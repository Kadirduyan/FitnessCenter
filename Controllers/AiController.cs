using FitnessCenter.Data;
using FitnessCenter.Models;
using FitnessCenter.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;

namespace FitnessCenter.Controllers;

public class AiController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly GeminiService _geminiService;

    public AiController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        GeminiService geminiService)
    {
        _context = context;
        _userManager = userManager;
        _geminiService = geminiService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Test()
    {
        return Json(new { status = "API çalışıyor", timestamp = DateTime.Now });
    }

    [HttpGet]
    public IActionResult TestApiKey()
    {
        try
        {
            // API anahtarının ilk ve son karakterlerini göster (güvenlik için tamamını göstermiyoruz)
            var apiKey = _geminiService.GetApiKeyPreview();
            return Json(new { 
                status = "API anahtarı yapılandırılmış", 
                apiKeyPreview = apiKey,
                timestamp = DateTime.Now 
            });
        }
        catch (Exception ex)
        {
            return Json(new { 
                status = "Hata", 
                error = ex.Message,
                timestamp = DateTime.Now 
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ListModels()
    {
        try
        {
            var models = await _geminiService.ListAvailableModels();
            return Ok(models);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Modeller listelenirken hata: {ex.Message}" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailableModels()
    {
        try
        {
            var modelNames = await _geminiService.GetAvailableModelNames();
            return Ok(new { models = modelNames, count = modelNames.Count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Model isimleri alınırken hata: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetExerciseRecommendation([FromBody] ExerciseRecommendationRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { error = "Geçersiz istek verisi." });
            }

            var prompt = BuildExercisePrompt(request);
            var response = await _geminiService.GenerateTextResponse(prompt);
            return Ok(new { recommendation = response });
        }
        catch (HttpRequestException ex)
        {
            // API hatası - detaylı mesaj döndür
            var errorMessage = ex.Message;
            
            // Rate Limit hatası
            if (errorMessage.Contains("Rate Limit") || errorMessage.Contains("429") || errorMessage.Contains("TooManyRequests") || errorMessage.Contains("quota"))
            {
                return StatusCode(429, new { 
                    error = "API kotası aşıldı. Lütfen bir süre bekleyip tekrar deneyin veya Google AI Studio'dan kullanım limitlerinizi kontrol edin.",
                    details = errorMessage,
                    helpUrl = "https://ai.dev/usage?tab=rate-limit"
                });
            }
            
            if (errorMessage.Contains("401") || errorMessage.Contains("Unauthorized"))
            {
                return StatusCode(401, new { 
                    error = "API anahtarı geçersiz veya yetkisiz. Lütfen appsettings.json dosyasındaki API anahtarını kontrol edin.",
                    details = errorMessage
                });
            }
            if (errorMessage.Contains("403") || errorMessage.Contains("Forbidden"))
            {
                return StatusCode(403, new { 
                    error = "API anahtarının bu modellere erişim izni yok. Lütfen Google AI Studio'da API anahtarınızı kontrol edin.",
                    details = errorMessage
                });
            }
            return StatusCode(500, new { error = $"API hatası: {errorMessage}" });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Bir hata oluştu: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetDietPlan([FromBody] DietPlanRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { error = "Geçersiz istek verisi." });
            }

            var prompt = BuildDietPrompt(request);
            var response = await _geminiService.GenerateTextResponse(prompt);
            return Ok(new { dietPlan = response });
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = ex.Message;
            if (errorMessage.Contains("401") || errorMessage.Contains("Unauthorized"))
            {
                return StatusCode(401, new { 
                    error = "API anahtarı geçersiz veya yetkisiz. Lütfen appsettings.json dosyasındaki API anahtarını kontrol edin.",
                    details = errorMessage
                });
            }
            if (errorMessage.Contains("403") || errorMessage.Contains("Forbidden"))
            {
                return StatusCode(403, new { 
                    error = "API anahtarının bu modellere erişim izni yok. Lütfen Google AI Studio'da API anahtarınızı kontrol edin.",
                    details = errorMessage
                });
            }
            return StatusCode(500, new { error = $"API hatası: {errorMessage}" });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Bir hata oluştu: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AnalyzePhoto(IFormFile photo, string analysisType = "body_analysis")
    {
        try
        {
            if (photo == null || photo.Length == 0)
            {
                return BadRequest(new { error = "Lütfen bir fotoğraf yükleyin." });
            }

            // Fotoğrafı base64'e çevir
            string base64Image;
            using (var memoryStream = new MemoryStream())
            {
                await photo.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();
                base64Image = Convert.ToBase64String(imageBytes);
            }

            var prompt = BuildPhotoAnalysisPrompt(analysisType);
            var response = await _geminiService.AnalyzeImage(base64Image, prompt);

            return Ok(new { analysis = response });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(500, new { error = $"API hatası: {ex.Message}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Fotoğraf analizi sırasında hata oluştu: {ex.Message}" });
        }
    }

    private string BuildPhotoAnalysisPrompt(string analysisType)
    {
        switch (analysisType)
        {
            case "body_analysis":
                return "Bu kişinin vücut tipini, fitness seviyesini ve potansiyel gelişim alanlarını analiz et. Vücut kompozisyonu, duruş ve genel kondisyon hakkında detaylı geri bildirim ver. Türkçe yanıt ver.";
            case "posture_analysis":
                return "Bu kişinin duruşunu analiz et. Sırt, omuzlar, kalça hizalama ve postür problemleri hakkında öneriler ver. Türkçe yanıt ver.";
            case "progress_tracking":
                return "Bu kişinin fiziksel ilerlemesini değerlendir. Kas gelişimi, yağ oranı değişimi ve genel gelişim hakkında yorum yap. Türkçe yanıt ver.";
            default:
                return "Bu fotoğrafı fitness açısından analiz et ve öneriler ver. Türkçe yanıt ver.";
        }
    }

    private string BuildExercisePrompt(ExerciseRecommendationRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Sen bir fitness uzmanısın. Aşağıdaki bilgilere göre kişiselleştirilmiş bir egzersiz planı öner.");

        if (request.Height > 0 && request.Weight > 0)
        {
            var bmi = request.Weight / Math.Pow(request.Height / 100.0, 2);
            sb.AppendLine($"Boy: {request.Height} cm, Kilo: {request.Weight} kg, BMI: {bmi:F2}");
        }

        if (!string.IsNullOrEmpty(request.BodyType))
        {
            sb.AppendLine($"Vücut Tipi: {request.BodyType}");
        }

        if (!string.IsNullOrEmpty(request.Goal))
        {
            sb.AppendLine($"Hedef: {request.Goal}");
        }

        if (!string.IsNullOrEmpty(request.FitnessLevel))
        {
            sb.AppendLine($"Fitness Seviyesi: {request.FitnessLevel}");
        }

        if (request.Age > 0)
        {
            sb.AppendLine($"Yaş: {request.Age}");
        }

        sb.AppendLine("\nLütfen haftalık bir egzersiz planı oluştur. Her gün için önerilen egzersizleri, set sayılarını ve tekrar sayılarını belirt. Türkçe yanıt ver.");

        return sb.ToString();
    }

    private string BuildDietPrompt(DietPlanRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Sen bir beslenme uzmanısın. Aşağıdaki bilgilere göre kişiselleştirilmiş bir diyet planı öner.");

        if (request.Height > 0 && request.Weight > 0)
        {
            var bmi = request.Weight / Math.Pow(request.Height / 100.0, 2);
            sb.AppendLine($"Boy: {request.Height} cm, Kilo: {request.Weight} kg, BMI: {bmi:F2}");
        }

        if (!string.IsNullOrEmpty(request.Goal))
        {
            sb.AppendLine($"Hedef: {request.Goal}");
        }

        if (request.Age > 0)
        {
            sb.AppendLine($"Yaş: {request.Age}");
        }

        if (!string.IsNullOrEmpty(request.DietaryRestrictions))
        {
            sb.AppendLine($"Diyet Kısıtlamaları: {request.DietaryRestrictions}");
        }

        sb.AppendLine("\nLütfen günlük bir beslenme planı oluştur. Kahvaltı, öğle yemeği, akşam yemeği ve ara öğünler için öneriler sun. Türkçe yanıt ver.");

        return sb.ToString();
    }
}

public class ExerciseRecommendationRequest
{
    public int Height { get; set; }
    public int Weight { get; set; }
    public string? BodyType { get; set; }
    public string? Goal { get; set; }
    public string? FitnessLevel { get; set; }
    public int Age { get; set; }
}

public class DietPlanRequest
{
    public int Height { get; set; }
    public int Weight { get; set; }
    public string? Goal { get; set; }
    public int Age { get; set; }
    public string? DietaryRestrictions { get; set; }
}