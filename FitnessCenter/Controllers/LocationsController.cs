using Microsoft.AspNetCore.Mvc;

namespace FitnessCenter.Controllers
{
    public class LocationsController : Controller
    {
        // GET: Locations
        public ActionResult Index()
        {
            ViewBag.LocationName = "KADIR Fitness İstanbul";
            ViewBag.Address = "Beşiktaş, İstanbul, Türkiye";
            ViewBag.Description = "İstanbul Boğaz kenarında, Beşiktaş'ta bulunan modern fitness salonumuz.";
            ViewBag.Latitude = "41.0422";  // Beşiktaş koordinatları
            ViewBag.Longitude = "29.0086";

            return View();
        }

        // GET: Locations/Details
        public ActionResult Details()
        {
            // Detaylı lokasyon bilgileri
            ViewBag.LocationName = "KADIR Fitness İstanbul";
            ViewBag.Address = "Beşiktaş, İstanbul, Türkiye";
            ViewBag.Phone = "(555) 123-4567";
            ViewBag.Email = "info@kadirfitness.com";
            ViewBag.WorkingHours = "Her gün 05:00 - 22:00";
            ViewBag.Description = "Türkiye'nin ilk tam ölçekli yapay zeka destekli spor salonu. İstanbul Boğaz kenarında, Beşiktaş'ta bulunan modern fitness salonumuz.";

            return View();
        }
    }
}
