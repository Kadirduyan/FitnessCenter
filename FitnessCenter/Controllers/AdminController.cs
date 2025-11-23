using FitnessCenter.Data;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var stats = new
            {
                FitnessCenters = _context.FitnessCenters.Count(),
                Trainers = _context.Trainers.Count(),
                Services = _context.Services.Count(),
                Appointments = _context.Appointments.Count(),
                Members = _context.Users.Count()
            };

            ViewBag.Stats = stats;
            return View();
        }
    }
}

