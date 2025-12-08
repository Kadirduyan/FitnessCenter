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
            // Admin kullanıcılarını direkt Admin paneline yönlendir
            return RedirectToAction("Index", "Admin");
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

        // Filtreleri veritabanındaki hizmetlerden dinamik olarak oluştur
        // Böylece yeni eklenen hizmetlerin ServiceType'ları otomatik olarak filtrelerde görünecek
        var availableServiceTypes = services
            .Select(s => s.ServiceType)
            .Distinct()
            .Where(type => type != ServiceType.Spinning && 
                          type != ServiceType.GroupClass && 
                          type != ServiceType.Other)
            .OrderBy(type => type)
            .ToList();

        ViewBag.ServiceTypes = availableServiceTypes;

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
