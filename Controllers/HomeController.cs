using System.Diagnostics;
using FitnessCenter.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FitnessCenter.Models;

namespace FitnessCenter.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("Admin"))
        {
            // Admin için istatistik verileri
            ViewBag.TotalAppointments = await _context.Appointments.CountAsync();
            ViewBag.TotalTrainers = await _context.Trainers.CountAsync(t => t.IsActive);
            ViewBag.TotalServices = await _context.Services.CountAsync();
            ViewBag.TotalMembers = await _context.Users.CountAsync();
        }
        else
        {
            // Normal üye için fitness center verileri
            var fitnessCenters = await _context.FitnessCenters
                .Include(fc => fc.Services)
                .Include(fc => fc.Trainers)
                .ToListAsync();

            ViewBag.FitnessCenters = fitnessCenters;
        }

        return View();
    }

    [AllowAnonymous]
    public async Task<IActionResult> Services()
    {
        var services = await _context.Services
            .Include(s => s.FitnessCenter)
            .Include(s => s.Trainers)
            .OrderBy(s => s.ServiceType)
            .ThenBy(s => s.Name)
            .ToListAsync();

        ViewBag.ServiceTypes = Enum.GetValues(typeof(ServiceType))
            .Cast<ServiceType>()
            .ToList();

        return View(services);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult FitnessBenefits()
    {
        return View();
    }

    public IActionResult BiohackingBenefits()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
