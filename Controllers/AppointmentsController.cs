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
        // GÜNCELLENDİ: trainerId parametresi ile antrenör takvimi gösterimi
        public async Task<IActionResult> Create(int? trainerId)
        {
            if (!trainerId.HasValue)
            {
                // Trainer seçilmemişse, genel randevu alma sayfasına yönlendir
                return RedirectToAction("List", "Trainers");
            }

            var trainer = await _context.Trainers
                .Include(t => t.FitnessCenter)
                .Include(t => t.Services)
                .FirstOrDefaultAsync(t => t.Id == trainerId.Value);

            if (trainer == null || !trainer.IsActive)
            {
                TempData["ErrorMessage"] = "Seçilen antrenör bulunamadı veya aktif değil.";
                return RedirectToAction("List", "Trainers");
            }

            // View'a trainer bilgilerini gönder
            ViewBag.Trainer = trainer;
            ViewBag.SelectedTrainerId = trainerId;
            ViewBag.Services = trainer.Services.Where(s => s.FitnessCenterId == trainer.FitnessCenterId).ToList();

            var appointment = new Appointment
            {
                TrainerId = trainerId.Value,
                AppointmentDate = DateTime.Today.AddDays(1) // Default olarak yarın
            };

            return View(appointment);
        }

        // POST: Appointments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TrainerId,ServiceId,AppointmentDate,AppointmentTime,Notes,CardHolderName,CardNumber,CardExpiryDate,CardCVV")] Appointment appointment)
        {
            // Debug: Gelen verileri logla
            Console.WriteLine($"=== RANDEVU ALMA BAŞLATILDI ===");
            Console.WriteLine($"TrainerId: {appointment.TrainerId}");
            Console.WriteLine($"ServiceId: {appointment.ServiceId}");
            Console.WriteLine($"AppointmentDate: {appointment.AppointmentDate}");
            Console.WriteLine($"AppointmentTime: {appointment.AppointmentTime}");
            Console.WriteLine($"Notes: {appointment.Notes}");
            Console.WriteLine($"User Authenticated: {User.Identity?.IsAuthenticated}");
            Console.WriteLine($"User Name: {User.Identity?.Name}");

            // Debug: Kullanıcı giriş kontrolü
            if (!User.Identity!.IsAuthenticated)
            {
                Console.WriteLine("HATA: Kullanıcı giriş yapmamış!");
                ModelState.AddModelError("", "Randevu almak için giriş yapmanız gerekir.");
                await PopulateCreateViewData(appointment.TrainerId, appointment.ServiceId);
                return View(appointment);
            }

            // Debug: ModelState hatalarını kontrol et
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                Console.WriteLine($"ModelState Hataları: {string.Join(", ", errors)}");
                ModelState.AddModelError("", "Form verileri geçersiz: " + string.Join(", ", errors));
                await PopulateCreateViewData(appointment.TrainerId, appointment.ServiceId);
                return View(appointment);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("HATA: Kullanıcı bulunamadı!");
                ModelState.AddModelError("", "Kullanıcı bilgileri alınamadı. Lütfen tekrar giriş yapın.");
                await PopulateCreateViewData(appointment.TrainerId, appointment.ServiceId);
                return View(appointment);
            }

            Console.WriteLine($"Kullanıcı bulundu: {user.Id} - {user.FirstName} {user.LastName}");

            // Form verilerini doğrula
            if (appointment.TrainerId == 0)
            {
                Console.WriteLine("HATA: TrainerId 0!");
                ModelState.AddModelError("", "Antrenör seçilmedi.");
                await PopulateCreateViewData(appointment.TrainerId, appointment.ServiceId);
                return View(appointment);
            }

            if (appointment.ServiceId == 0)
            {
                Console.WriteLine("HATA: ServiceId 0!");
                ModelState.AddModelError("", "Hizmet seçilmedi.");
                await PopulateCreateViewData(appointment.TrainerId, appointment.ServiceId);
                return View(appointment);
            }

            if (appointment.AppointmentDate == default(DateTime))
            {
                Console.WriteLine("HATA: AppointmentDate boş!");
                ModelState.AddModelError("", "Tarih seçilmedi.");
                await PopulateCreateViewData(appointment.TrainerId, appointment.ServiceId);
                return View(appointment);
            }

            if (appointment.AppointmentTime == default(TimeSpan))
            {
                Console.WriteLine("HATA: AppointmentTime boş!");
                ModelState.AddModelError("", "Saat seçilmedi.");
                await PopulateCreateViewData(appointment.TrainerId, appointment.ServiceId);
                return View(appointment);
            }

            // Veritabanında trainer ve service var mı kontrol et
            var trainer = await _context.Trainers.FindAsync(appointment.TrainerId);
            if (trainer == null)
            {
                Console.WriteLine($"HATA: Trainer {appointment.TrainerId} bulunamadı!");
                ModelState.AddModelError("", "Seçilen antrenör bulunamadı.");
                await PopulateCreateViewData(appointment.TrainerId, appointment.ServiceId);
                return View(appointment);
            }

            var service = await _context.Services.FindAsync(appointment.ServiceId);
            if (service == null)
            {
                Console.WriteLine($"HATA: Service {appointment.ServiceId} bulunamadı!");
                ModelState.AddModelError("", "Seçilen hizmet bulunamadı.");
                await PopulateCreateViewData(appointment.TrainerId, appointment.ServiceId);
                return View(appointment);
            }

            Console.WriteLine($"Trainer: {trainer.FullName}, Service: {service.Name}");

            appointment.MemberId = user.Id;
            appointment.Status = AppointmentStatus.Pending;
            appointment.CreatedAt = DateTime.Now;

            // Antrenör müsaitlik kontrolü
            var isAvailable = await IsTrainerAvailable(appointment.TrainerId, appointment.AppointmentDate, appointment.AppointmentTime);
            if (!isAvailable)
            {
                TempData["ErrorMessage"] = "Seçilen saatte antrenör müsait değil.";
                await PopulateCreateViewData(appointment.TrainerId, appointment.ServiceId);
                return View(appointment);
            }

            // Aynı gün aynı antrenör kontrolü
            var existingAppointmentForDay = await _context.Appointments
                .AnyAsync(a => a.MemberId == user.Id
                    && a.TrainerId == appointment.TrainerId
                    && a.AppointmentDate.Date == appointment.AppointmentDate.Date
                    && a.Status != AppointmentStatus.Cancelled
                    && a.Status != AppointmentStatus.Rejected);

            if (existingAppointmentForDay)
            {
                TempData["ErrorMessage"] = "Bu antrenörden aynı gün içinde sadece bir randevu alabilirsiniz.";
                await PopulateCreateViewData(appointment.TrainerId, appointment.ServiceId);
                return View(appointment);
            }

            // Saat çakışma kontrolü
            var conflictingAppointment = await _context.Appointments
                .AnyAsync(a => a.TrainerId == appointment.TrainerId
                    && a.AppointmentDate.Date == appointment.AppointmentDate.Date
                    && a.AppointmentTime == appointment.AppointmentTime
                    && a.Status != AppointmentStatus.Cancelled
                    && a.Status != AppointmentStatus.Rejected);

            if (conflictingAppointment)
            {
                TempData["ErrorMessage"] = "Bu saatte başka bir randevu bulunmaktadır.";
                await PopulateCreateViewData(appointment.TrainerId, appointment.ServiceId);
                return View(appointment);
            }

            try
            {
                _context.Add(appointment);
                await _context.SaveChangesAsync();

                // Kaydedildiğini doğrula
                var savedAppointment = await _context.Appointments.FindAsync(appointment.Id);
                if (savedAppointment == null)
                {
                    TempData["ErrorMessage"] = "Randevu kaydedilemedi. Lütfen tekrar deneyin.";
                    await PopulateCreateViewData(appointment.TrainerId, appointment.ServiceId);
                    return View(appointment);
                }

                return RedirectToAction("Confirm", new { id = appointment.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Randevu kaydedilirken bir hata oluştu: {ex.Message}";
                await PopulateCreateViewData(appointment.TrainerId, appointment.ServiceId);
                return View(appointment);
            }
        }

        // GET: Appointments/Confirm/5
        public async Task<IActionResult> Confirm(int? id)
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

        // POST: Appointments/Cancel/5 - Üye randevuyu iptal eder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                var user = await _userManager.GetUserAsync(User);
                if (!User.IsInRole("Admin") && appointment.MemberId != user!.Id)
                {
                    return Forbid();
                }

                // Sadece beklemede olan randevular iptal edilebilir
                if (appointment.Status != AppointmentStatus.Pending)
                {
                    TempData["ErrorMessage"] = "Bu randevu zaten işleme alınmış.";
                    return RedirectToAction(nameof(Index));
                }

                // Admin değilse, randevu saati geçmemiş ve son 24 saat içinde alınmış olmalı
                if (!User.IsInRole("Admin"))
                {
                    var now = DateTime.Now;
                    var appointmentDateTime = appointment.AppointmentDate.Date + appointment.AppointmentTime;

                    // Randevu saati geçmişse iptal edilemez
                    if (appointmentDateTime <= now)
                    {
                        TempData["ErrorMessage"] = "Geçmiş tarihli randevular iptal edilemez.";
                        return RedirectToAction(nameof(Index));
                    }

                    // Randevu alındıktan 24 saat geçmişse iptal edilemez
                    var timeSinceCreated = now - appointment.CreatedAt;
                    if (timeSinceCreated.TotalHours > 24)
                    {
                        TempData["ErrorMessage"] = "Randevu alındıktan 24 saat sonra iptal edilemez.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                appointment.Status = AppointmentStatus.Cancelled;
                appointment.UpdatedAt = DateTime.Now;
                _context.Update(appointment);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Randevu başarıyla iptal edildi. Para iadesi 3 iş günü içinde ödenecektir.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Appointments/Delete/5 - Admin randevuyu siler
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                // Sadece onaylanmış, reddedilmiş, tamamlanmış veya iptal edilmiş randevular silinebilir
                if (appointment.Status != AppointmentStatus.Approved && appointment.Status != AppointmentStatus.Rejected &&
                    appointment.Status != AppointmentStatus.Completed && appointment.Status != AppointmentStatus.Cancelled)
                {
                    TempData["ErrorMessage"] = "Bu randevu henüz işleme alınmamış.";
                    return RedirectToAction(nameof(Index));
                }

                // Veritabanından tamamen kaldır
                _context.Appointments.Remove(appointment);
            }

            await _context.SaveChangesAsync();

            // Para iadesi mesajını durumuna göre ayarla
            string refundMessage = "";
            if (appointment!.Status == AppointmentStatus.Approved)
            {
                refundMessage = "Onaylanmış randevu silindi. Üyeye 3 iş günü içinde para iadesi yapılacaktır.";
            }
            else if (appointment.Status == AppointmentStatus.Rejected)
            {
                refundMessage = "Reddedilmiş randevu silindi. Üyeye 3 iş günü içinde para iadesi yapılacaktır.";
            }
            else if (appointment.Status == AppointmentStatus.Cancelled)
            {
                refundMessage = "İptal edilmiş randevu silindi.";
            }
            else
            {
                refundMessage = "Tamamlanmış randevu silindi.";
            }

            TempData["SuccessMessage"] = $"Randevu başarıyla silindi. {refundMessage}";
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
                // Varsayılan olarak boş döndür (veya demo için default saatler eklenebilir)
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
                startTime = startTime.Add(TimeSpan.FromMinutes(60)); // 1 saatlik aralıklar
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
            if (trainerId.HasValue)
            {
                var trainer = await _context.Trainers
                    .Include(t => t.FitnessCenter)
                    .Include(t => t.Services)
                    .FirstOrDefaultAsync(t => t.Id == trainerId.Value);

                if (trainer != null)
                {
                    ViewBag.Trainer = trainer;
                    ViewBag.SelectedTrainerId = trainerId;
                    ViewBag.Services = trainer.Services.Where(s => s.FitnessCenterId == trainer.FitnessCenterId).ToList();
                }
            }
        }

        // Debug: Veritabanı bağlantısını test etmek için
        [HttpGet]
        public async Task<IActionResult> TestDb()
        {
            try
            {
                var count = await _context.Appointments.CountAsync();
                var trainers = await _context.Trainers.CountAsync();
                var services = await _context.Services.CountAsync();

                return Content($"Veritabanı bağlantısı çalışıyor. Randevular: {count}, Antrenörler: {trainers}, Hizmetler: {services}");
            }
            catch (Exception ex)
            {
                return Content($"Veritabanı hatası: {ex.Message}");
            }
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.Id == id);
        }
    }
}