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
            // Sadece randevu ve antrenör istatistikleri
            var stats = new
            {
                Trainers = _context.Trainers.Count(),
                Appointments = _context.Appointments.Count(),
                PendingAppointments = _context.Appointments.Count(a => a.Status == AppointmentStatus.Pending),
                CompletedAppointments = _context.Appointments.Count(a => a.Status == AppointmentStatus.Completed)
            };

            ViewBag.Stats = stats;
            ViewBag.IsAdminPanel = true;
            return View();
        }


        // GET: Admin/Appointments - Randevu Yönetimi
        public async Task<IActionResult> Appointments(string? status = null)
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

        // GET: Admin/Trainers - Antrenör Yönetimi
        public async Task<IActionResult> Trainers()
        {
            var trainers = await _context.Trainers
                .Include(t => t.FitnessCenter)
                .Include(t => t.Services)
                .OrderBy(t => t.FirstName)
                .ToListAsync();

            return View(trainers);
        }

        // GET: Admin/CreateTrainer
        public async Task<IActionResult> CreateTrainer()
        {
            ViewBag.FitnessCenters = new SelectList(await _context.FitnessCenters.ToListAsync(), "Id", "Name");
            ViewBag.Services = await _context.Services.ToListAsync();
            return View();
        }

        // POST: Admin/CreateTrainer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTrainer(CreateTrainerViewModel model)
        {
            // Spor salonu çalışma saatlerini kontrol et
            var fitnessCenter = await _context.FitnessCenters.FindAsync(model.FitnessCenterId);
            if (fitnessCenter != null)
            {
                if (model.StartTime < fitnessCenter.OpenTime || model.EndTime > fitnessCenter.CloseTime)
                {
                    ModelState.AddModelError("", $"Antrenör çalışma saatleri spor salonu çalışma saatleri ({fitnessCenter.OpenTime:hh\\:mm} - {fitnessCenter.CloseTime:hh\\:mm}) içinde olmalıdır.");
                }
            }

            if (ModelState.IsValid)
            {
                var trainer = new Trainer
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    Specializations = model.Specializations,
                    Description = model.Description,
                    FitnessCenterId = model.FitnessCenterId,
                    CreatedAt = DateTime.Now
                };

                _context.Trainers.Add(trainer);
                await _context.SaveChangesAsync();

                // Seçilen hizmetleri antrenöre ekle
                if (model.SelectedServices != null && model.SelectedServices.Length > 0)
                {
                    foreach (var serviceId in model.SelectedServices)
                    {
                        var service = await _context.Services.FindAsync(serviceId);
                        if (service != null)
                        {
                            trainer.Services.Add(service);
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                // Antrenör çalışma saatlerini ekle
                var availabilities = new List<TrainerAvailability>();

                // Pazartesi- Cuma: Kullanıcı tarafından belirlenen saatlerde
                for (int day = 1; day <= 5; day++)
                {
                    availabilities.Add(new TrainerAvailability
                    {
                        TrainerId = trainer.Id,
                        DayOfWeek = (DayOfWeek)day,
                        StartTime = model.StartTime,
                        EndTime = model.EndTime
                    });
                }

                // Cumartesi: Kullanıcı tarafından belirlenen saatlerde
                availabilities.Add(new TrainerAvailability
                {
                    TrainerId = trainer.Id,
                    DayOfWeek = DayOfWeek.Saturday,
                    StartTime = model.StartTime,
                    EndTime = model.EndTime
                });

                // Pazar: Kullanıcı tarafından belirlenen saatlerde
                availabilities.Add(new TrainerAvailability
                {
                    TrainerId = trainer.Id,
                    DayOfWeek = DayOfWeek.Sunday,
                    StartTime = model.StartTime,
                    EndTime = model.EndTime
                });

                _context.TrainerAvailabilities.AddRange(availabilities);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Antrenör başarıyla eklendi.";
                return RedirectToAction("Trainers");
            }

            ViewBag.FitnessCenters = new SelectList(await _context.FitnessCenters.ToListAsync(), "Id", "Name");
            ViewBag.Services = await _context.Services.ToListAsync();
            return View(model);
        }

        // POST: Admin/DeleteTrainer/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTrainer(int id)
        {
            var trainer = await _context.Trainers
                .Include(t => t.Appointments)
                .Include(t => t.Services)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trainer == null)
            {
                return NotFound();
            }

            // Aktif randevuları kontrol et
            if (trainer.Appointments.Any(a => a.Status == AppointmentStatus.Approved || a.Status == AppointmentStatus.Pending))
            {
                TempData["ErrorMessage"] = "Bu antrenörün aktif randevuları olduğu için silinemez.";
                return RedirectToAction("Trainers");
            }

            // TrainerService ilişkilerini temizle (many-to-many)
            trainer.Services.Clear();

            _context.Trainers.Remove(trainer);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Antrenör başarıyla silindi.";
            return RedirectToAction("Trainers");
        }

        // GET: Admin/Services - Hizmet Yönetimi
        public async Task<IActionResult> Services()
        {
            var services = await _context.Services
                .Include(s => s.FitnessCenter)
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View(services);
        }

        // GET: Admin/CreateService
        public async Task<IActionResult> CreateService()
        {
            ViewBag.FitnessCenters = new SelectList(await _context.FitnessCenters.ToListAsync(), "Id", "Name");
            ViewBag.ServiceTypes = new SelectList(Enum.GetValues(typeof(ServiceType)).Cast<ServiceType>()
                .Select(e => new { Value = e.ToString(), Text = e.GetDisplayName() }), "Value", "Text");
            return View();
        }

        // POST: Admin/CreateService
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateService(Service service)
        {
            if (ModelState.IsValid)
            {
                service.CreatedAt = DateTime.Now;
                _context.Services.Add(service);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Hizmet başarıyla eklendi.";
                return RedirectToAction("Services");
            }

            ViewBag.FitnessCenters = new SelectList(await _context.FitnessCenters.ToListAsync(), "Id", "Name");
            ViewBag.ServiceTypes = new SelectList(Enum.GetValues(typeof(ServiceType)).Cast<ServiceType>()
                .Select(e => new { Value = e.ToString(), Text = e.GetDisplayName() }), "Value", "Text");
            return View(service);
        }

        // GET: Admin/EditService/5
        public async Task<IActionResult> EditService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }

            ViewBag.FitnessCenters = new SelectList(await _context.FitnessCenters.ToListAsync(), "Id", "Name", service.FitnessCenterId);
            ViewBag.ServiceTypes = new SelectList(Enum.GetValues(typeof(ServiceType)).Cast<ServiceType>()
                .Select(e => new { Value = e.ToString(), Text = e.GetDisplayName() }), "Value", "Text", service.ServiceType.ToString());
            return View(service);
        }

        // POST: Admin/EditService/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditService(int id, Service service)
        {
            if (id != service.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(service);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Hizmet başarıyla güncellendi.";
                    return RedirectToAction("Services");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceExists(service.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            ViewBag.FitnessCenters = new SelectList(await _context.FitnessCenters.ToListAsync(), "Id", "Name", service.FitnessCenterId);
            ViewBag.ServiceTypes = new SelectList(Enum.GetValues(typeof(ServiceType)).Cast<ServiceType>()
                .Select(e => new { Value = e.ToString(), Text = e.GetDisplayName() }), "Value", "Text", service.ServiceType.ToString());
            return View(service);
        }

        // POST: Admin/DeleteService/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteService(int id)
        {
            var service = await _context.Services
                .Include(s => s.Appointments)
                .Include(s => s.Trainers)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (service == null)
            {
                return NotFound();
            }

            // Aktif randevuları kontrol et
            if (service.Appointments.Any(a => a.Status == AppointmentStatus.Approved || a.Status == AppointmentStatus.Pending))
            {
                TempData["ErrorMessage"] = "Bu hizmetin aktif randevuları olduğu için silinemez.";
                return RedirectToAction("Services");
            }

            // TrainerService ilişkilerini temizle (many-to-many)
            foreach (var trainer in service.Trainers.ToList())
            {
                trainer.Services.Remove(service);
            }

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Hizmet başarıyla silindi.";
            return RedirectToAction("Services");
        }

        private bool ServiceExists(int id)
        {
            return _context.Services.Any(e => e.Id == id);
        }

        // GET: Admin/Memberships - Üyelik Yönetimi
        public async Task<IActionResult> Memberships()
        {
            var memberships = await _context.Memberships
                .Include(m => m.Member)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return View(memberships);
        }

        // POST: Admin/DeleteMembership/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMembership(int id)
        {
            var membership = await _context.Memberships.FindAsync(id);
            if (membership == null)
            {
                return NotFound();
            }

            _context.Memberships.Remove(membership);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Üyelik başarıyla silindi.";
            return RedirectToAction("Memberships");
        }

        // GET: Admin/ExtendMembership/5
        public async Task<IActionResult> ExtendMembership(int id)
        {
            var membership = await _context.Memberships.FindAsync(id);
            if (membership == null)
            {
                return NotFound();
            }

            ViewBag.MembershipTypes = new SelectList(Enum.GetValues(typeof(MembershipType)).Cast<MembershipType>()
                .Select(e => new { Value = e.ToString(), Text = e.GetDisplayName() }), "Value", "Text", membership.MembershipType.ToString());
            return View(membership);
        }

        // POST: Admin/ExtendMembership/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExtendMembership(int id, MembershipType membershipType)
        {
            var membership = await _context.Memberships.FindAsync(id);
            if (membership == null)
            {
                return NotFound();
            }

            // Mevcut bitiş tarihinden itibaren uzat
            var extensionMonths = (int)membershipType;
            membership.EndDate = membership.EndDate.AddMonths(extensionMonths);
            membership.MembershipType = membershipType;
            membership.UpdatedAt = DateTime.Now;
            membership.IsActive = true;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Üyelik {extensionMonths} ay uzatıldı. Yeni bitiş tarihi: {membership.EndDate:dd.MM.yyyy}";
            return RedirectToAction("Memberships");
        }

    }
}

