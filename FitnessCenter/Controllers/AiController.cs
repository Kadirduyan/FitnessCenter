using FitnessCenter.Data;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace FitnessCenter.Controllers
{
    [Authorize]
    public class AiController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public AiController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetExerciseRecommendation([FromBody] ExerciseRecommendationRequest request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                // OpenAI API key from configuration
                var apiKey = _configuration["OpenAI:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    return BadRequest(new { error = "OpenAI API anahtarı yapılandırılmamış. Lütfen appsettings.json dosyasına 'OpenAI:ApiKey' ekleyin." });
                }

                // Build prompt based on user input
                var prompt = BuildExercisePrompt(request, user);

                // Call OpenAI API
                var response = await CallOpenAI(apiKey, prompt);

                return Ok(new { recommendation = response });
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
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var apiKey = _configuration["OpenAI:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    return BadRequest(new { error = "OpenAI API anahtarı yapılandırılmamış." });
                }

                var prompt = BuildDietPrompt(request, user);
                var response = await CallOpenAI(apiKey, prompt);

                return Ok(new { dietPlan = response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Bir hata oluştu: {ex.Message}" });
            }
        }

        private string BuildExercisePrompt(ExerciseRecommendationRequest request, ApplicationUser user)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Sen bir fitness uzmanısın. Aşağıdaki bilgilere göre kişiselleştirilmiş bir egzersiz planı öner.");

            if (request.Height.HasValue && request.Weight.HasValue)
            {
                var bmi = request.Weight.Value / Math.Pow(request.Height.Value / 100.0, 2);
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

            if (request.Age.HasValue)
            {
                sb.AppendLine($"Yaş: {request.Age}");
            }

            sb.AppendLine("\nLütfen haftalık bir egzersiz planı oluştur. Her gün için önerilen egzersizleri, set sayılarını ve tekrar sayılarını belirt.");

            return sb.ToString();
        }

        private string BuildDietPrompt(DietPlanRequest request, ApplicationUser user)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Sen bir beslenme uzmanısın. Aşağıdaki bilgilere göre kişiselleştirilmiş bir diyet planı öner.");

            if (request.Height.HasValue && request.Weight.HasValue)
            {
                var bmi = request.Weight.Value / Math.Pow(request.Height.Value / 100.0, 2);
                sb.AppendLine($"Boy: {request.Height} cm, Kilo: {request.Weight} kg, BMI: {bmi:F2}");
            }

            if (!string.IsNullOrEmpty(request.Goal))
            {
                sb.AppendLine($"Hedef: {request.Goal}");
            }

            if (request.Age.HasValue)
            {
                sb.AppendLine($"Yaş: {request.Age}");
            }

            if (!string.IsNullOrEmpty(request.DietaryRestrictions))
            {
                sb.AppendLine($"Diyet Kısıtlamaları: {request.DietaryRestrictions}");
            }

            sb.AppendLine("\nLütfen günlük bir beslenme planı oluştur. Kahvaltı, öğle yemeği, akşam yemeği ve ara öğünler için öneriler sun.");

            return sb.ToString();
        }

        private async Task<string> CallOpenAI(string apiKey, string prompt)
        {
            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "system", content = "Sen bir fitness ve beslenme uzmanısın. Türkçe yanıt ver." },
                    new { role = "user", content = prompt }
                },
                max_tokens = 1000,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseJson = JsonDocument.Parse(responseContent);

            return responseJson.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "Yanıt alınamadı.";
        }
    }

    public class ExerciseRecommendationRequest
    {
        public int? Height { get; set; }
        public int? Weight { get; set; }
        public string? BodyType { get; set; }
        public string? Goal { get; set; }
        public string? FitnessLevel { get; set; }
        public int? Age { get; set; }
    }

    public class DietPlanRequest
    {
        public int? Height { get; set; }
        public int? Weight { get; set; }
        public string? Goal { get; set; }
        public int? Age { get; set; }
        public string? DietaryRestrictions { get; set; }
    }
}

