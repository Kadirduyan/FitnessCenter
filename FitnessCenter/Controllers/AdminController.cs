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
                Members = _context.Users.Count(),
                PendingAppointments = _context.Appointments.Count(a => a.Status == AppointmentStatus.Pending)
            };

            ViewBag.Stats = stats;
            return View();
        }

        // GET: Admin/Appointments - Randevu Yönetimi
        public async Task<IActionResult> Appointments(string status = null)
        {
            var appointments = _context.Appointments
                .Include(a => a.Member)
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<AppointmentStatus>(status, out var appointmentStatus))
                {
                    appointments = appointments.Where(a => a.Status == appointmentStatus);
                }
            }

            ViewBag.CurrentStatus = status;
            ViewBag.PendingCount = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Pending);
            return View(await appointments.OrderByDescending(a => a.CreatedAt).ToListAsync());
        }

        // POST: Admin/ApproveAppointment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            appointment.Status = AppointmentStatus.Approved;
            appointment.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Randevu başarıyla onaylandı.";
            return RedirectToAction("Appointments");
        }

        // POST: Admin/RejectAppointment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            appointment.Status = AppointmentStatus.Rejected;
            appointment.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Randevu reddedildi.";
            return RedirectToAction("Appointments");
        }

        // POST: Admin/CompleteAppointment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            appointment.Status = AppointmentStatus.Completed;
            appointment.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Randevu tamamlandı olarak işaretlendi.";
            return RedirectToAction("Appointments");
        }
    }
}

