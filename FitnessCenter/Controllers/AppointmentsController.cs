using FitnessCenter.Data;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Controllers
{
    [Authorize]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AppointmentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Appointments
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<Appointment> appointments;

            if (User.IsInRole("Admin"))
            {
                appointments = _context.Appointments
                    .Include(a => a.Member)
                    .Include(a => a.Trainer)
                    .Include(a => a.Service)
                    .OrderByDescending(a => a.AppointmentDate);
            }
            else
            {
                appointments = _context.Appointments
                    .Include(a => a.Member)
                    .Include(a => a.Trainer)
                    .Include(a => a.Service)
                    .Where(a => a.MemberId == user!.Id)
                    .OrderByDescending(a => a.AppointmentDate);
            }

            return View(await appointments.ToListAsync());
        }

        // GET: Appointments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Member)
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            // Check authorization
            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && appointment.MemberId != user!.Id)
            {
                return Forbid();
            }

            return View(appointment);
        }

        // GET: Appointments/Create
        public async Task<IActionResult> Create()
        {
            var fitnessCenters = await _context.FitnessCenters.ToListAsync();
            ViewData["FitnessCenterId"] = new SelectList(fitnessCenters, "Id", "Name");

            return View();
        }

        // POST: Appointments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TrainerId,ServiceId,AppointmentDate,AppointmentTime,Notes")] Appointment appointment)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                appointment.MemberId = user!.Id;
                appointment.Status = AppointmentStatus.Pending;
                appointment.CreatedAt = DateTime.Now;

                // Check if trainer is available at this time
                var isAvailable = await IsTrainerAvailable(appointment.TrainerId, appointment.AppointmentDate, appointment.AppointmentTime);
                if (!isAvailable)
                {
                    ModelState.AddModelError("", "Seçilen saatte antrenör müsait değil veya başka bir randevu var.");
                    await PopulateCreateViewData(appointment.TrainerId, appointment.ServiceId);
                    return View(appointment);
                }

                // Check if there's a conflicting appointment
                var conflictingAppointment = await _context.Appointments
                    .Where(a => a.TrainerId == appointment.TrainerId
                        && a.AppointmentDate.Date == appointment.AppointmentDate.Date
                        && a.AppointmentTime == appointment.AppointmentTime
                        && a.Status != AppointmentStatus.Cancelled
                        && a.Status != AppointmentStatus.Rejected)
                    .FirstOrDefaultAsync();

                if (conflictingAppointment != null)
                {
                    ModelState.AddModelError("", "Bu saatte başka bir randevu bulunmaktadır.");
                    await PopulateCreateViewData(appointment.TrainerId, appointment.ServiceId);
                    return View(appointment);
                }

                _context.Add(appointment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Randevunuz başarıyla oluşturuldu. Onay bekleniyor.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateCreateViewData(appointment.TrainerId, appointment.ServiceId);
            return View(appointment);
        }

        // GET: Appointments/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            ViewData["MemberId"] = new SelectList(_context.Users, "Id", "UserName", appointment.MemberId);
            ViewData["TrainerId"] = new SelectList(_context.Trainers, "Id", "FullName", appointment.TrainerId);
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Name", appointment.ServiceId);
            return View(appointment);
        }

        // POST: Appointments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,MemberId,TrainerId,ServiceId,AppointmentDate,AppointmentTime,Status,Notes,CreatedAt")] Appointment appointment)
        {
            if (id != appointment.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    appointment.UpdatedAt = DateTime.Now;
                    _context.Update(appointment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppointmentExists(appointment.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["MemberId"] = new SelectList(_context.Users, "Id", "UserName", appointment.MemberId);
            ViewData["TrainerId"] = new SelectList(_context.Trainers, "Id", "FullName", appointment.TrainerId);
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Name", appointment.ServiceId);
            return View(appointment);
        }

        // GET: Appointments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Member)
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            // Check authorization
            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && appointment.MemberId != user!.Id)
            {
                return Forbid();
            }

            return View(appointment);
        }

        // POST: Appointments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                var user = await _userManager.GetUserAsync(User);
                if (!User.IsInRole("Admin") && appointment.MemberId != user!.Id)
                {
                    return Forbid();
                }

                appointment.Status = AppointmentStatus.Cancelled;
                appointment.UpdatedAt = DateTime.Now;
                _context.Update(appointment);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // AJAX: Get fitness centers
        [HttpGet]
        public async Task<IActionResult> GetFitnessCenters()
        {
            var fitnessCenters = await _context.FitnessCenters
                .Select(fc => new { fc.Id, fc.Name })
                .ToListAsync();
            return Json(fitnessCenters);
        }

        // AJAX: Get trainers by fitness center
        [HttpGet]
        public async Task<IActionResult> GetTrainersByFitnessCenter(int fitnessCenterId)
        {
            var trainers = await _context.Trainers
                .Where(t => t.FitnessCenterId == fitnessCenterId && t.IsActive)
                .Select(t => new { t.Id, Name = t.FullName })
                .ToListAsync();

            return Json(trainers);
        }

        // AJAX: Get services by trainer
        [HttpGet]
        public async Task<IActionResult> GetServicesByTrainer(int trainerId)
        {
            var trainer = await _context.Trainers
                .Include(t => t.Services)
                .FirstOrDefaultAsync(t => t.Id == trainerId);

            if (trainer == null)
            {
                return Json(new List<object>());
            }

            var services = trainer.Services.Select(s => new { s.Id, s.Name, s.DurationMinutes, s.Price }).ToList();
            return Json(services);
        }

        // AJAX: Get available time slots
        [HttpGet]
        public async Task<IActionResult> GetAvailableTimeSlots(int trainerId, DateTime date)
        {
            var trainer = await _context.Trainers
                .Include(t => t.Availabilities)
                .FirstOrDefaultAsync(t => t.Id == trainerId);

            if (trainer == null)
            {
                return Json(new List<object>());
            }

            var dayOfWeek = date.DayOfWeek;
            var availability = trainer.Availabilities
                .FirstOrDefault(a => a.DayOfWeek == dayOfWeek);

            if (availability == null)
            {
                return Json(new List<object>());
            }

            // Get existing appointments for this date
            var existingAppointments = await _context.Appointments
                .Where(a => a.TrainerId == trainerId
                    && a.AppointmentDate.Date == date.Date
                    && a.Status != AppointmentStatus.Cancelled
                    && a.Status != AppointmentStatus.Rejected)
                .Select(a => a.AppointmentTime)
                .ToListAsync();

            // Generate time slots
            var timeSlots = new List<object>();
            var startTime = availability.StartTime;
            var endTime = availability.EndTime;

            while (startTime < endTime)
            {
                if (!existingAppointments.Contains(startTime))
                {
                    timeSlots.Add(new
                    {
                        Time = startTime.ToString(@"hh\:mm"),
                        Value = startTime.ToString(@"hh\:mm")
                    });
                }
                startTime = startTime.Add(TimeSpan.FromMinutes(30));
            }

            return Json(timeSlots);
        }

        private async Task<bool> IsTrainerAvailable(int trainerId, DateTime date, TimeSpan time)
        {
            var trainer = await _context.Trainers
                .Include(t => t.Availabilities)
                .FirstOrDefaultAsync(t => t.Id == trainerId);

            if (trainer == null || !trainer.IsActive)
            {
                return false;
            }

            var dayOfWeek = date.DayOfWeek;
            var availability = trainer.Availabilities
                .FirstOrDefault(a => a.DayOfWeek == dayOfWeek);

            if (availability == null)
            {
                return false;
            }

            return time >= availability.StartTime && time < availability.EndTime;
        }

        private async Task PopulateCreateViewData(int? trainerId = null, int? serviceId = null)
        {
            var fitnessCenters = await _context.FitnessCenters.ToListAsync();
            ViewData["FitnessCenterId"] = new SelectList(fitnessCenters, "Id", "Name");

            if (trainerId.HasValue)
            {
                var trainer = await _context.Trainers
                    .Include(t => t.Services)
                    .FirstOrDefaultAsync(t => t.Id == trainerId.Value);

                if (trainer != null)
                {
                    ViewData["TrainerId"] = new SelectList(
                        new[] { trainer },
                        "Id",
                        "FullName",
                        trainerId);

                    ViewData["ServiceId"] = new SelectList(
                        trainer.Services,
                        "Id",
                        "Name",
                        serviceId);
                }
            }
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.Id == id);
        }
    }
}

