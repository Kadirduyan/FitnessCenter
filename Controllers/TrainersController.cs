using FitnessCenter.Data;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TrainersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrainersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- EKLENEN METOD: Üyeler için Antrenör Vitrini ---
        [AllowAnonymous] // Giriş yapmayanlar da görebilir
        public async Task<IActionResult> List()
        {
            var trainers = await _context.Trainers
                .Include(t => t.FitnessCenter)
                .Include(t => t.Services)
                .Where(t => t.IsActive) // Sadece aktif hocalar
                .ToListAsync();

            return View(trainers);
        }
        // ----------------------------------------------------

        // GET: Trainers (Admin Paneli)
        public async Task<IActionResult> Index()
        {
            var trainers = await _context.Trainers
                .Include(t => t.FitnessCenter)
                .Include(t => t.Services)
                .ToListAsync();
            return View(trainers);
        }

        // GET: Trainers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var trainer = await _context.Trainers
                .Include(t => t.FitnessCenter)
                .Include(t => t.Services)
                // Müsaitlikleri güne ve saate göre sıralı getir
                .Include(t => t.Availabilities.OrderBy(a => a.DayOfWeek).ThenBy(a => a.StartTime))
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trainer == null) return NotFound();

            return View(trainer);
        }

        // GET: Trainers/Create
        public IActionResult Create()
        {
            ViewData["FitnessCenterId"] = new SelectList(_context.FitnessCenters, "Id", "Name");
            // Hizmetleri ViewBag'e dolduruyoruz (View'da checkbox listesi için)
            ViewBag.Services = _context.Services.ToList(); 
            return View();
        }

        // POST: Trainers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Trainer trainer, int[] selectedServices)
        {
            if (ModelState.IsValid)
            {
                // Seçilen hizmetleri ilişkilendir
                if (selectedServices != null)
                {
                    foreach (var serviceId in selectedServices)
                    {
                        var service = await _context.Services.FindAsync(serviceId);
                        if (service != null) trainer.Services.Add(service);
                    }
                }

                _context.Add(trainer);
                await _context.SaveChangesAsync();

                // Yeni antrenör için müsaitlik saatleri ekle
                var availabilities = new List<FitnessCenter.Models.TrainerAvailability>();

                // Pazartesi- Cuma: 06:00-22:00, Cumartesi: 08:00-20:00, Pazar: 10:00-18:00
                // Pazartesi - Cuma
                for (int day = 1; day <= 5; day++)
                {
                    availabilities.Add(new FitnessCenter.Models.TrainerAvailability
                    {
                        TrainerId = trainer.Id,
                        DayOfWeek = (DayOfWeek)day,
                        StartTime = new TimeSpan(6, 0, 0),
                        EndTime = new TimeSpan(22, 0, 0)
                    });
                }

                // Cumartesi
                availabilities.Add(new FitnessCenter.Models.TrainerAvailability
                {
                    TrainerId = trainer.Id,
                    DayOfWeek = DayOfWeek.Saturday,
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(20, 0, 0)
                });

                // Pazar
                availabilities.Add(new FitnessCenter.Models.TrainerAvailability
                {
                    TrainerId = trainer.Id,
                    DayOfWeek = DayOfWeek.Sunday,
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(18, 0, 0)
                });

                _context.TrainerAvailabilities.AddRange(availabilities);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            
            // Hata durumunda listeleri tekrar doldur
            ViewData["FitnessCenterId"] = new SelectList(_context.FitnessCenters, "Id", "Name", trainer.FitnessCenterId);
            ViewBag.Services = _context.Services.ToList();
            return View(trainer);
        }

        // GET: Trainers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var trainer = await _context.Trainers
                .Include(t => t.Services)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trainer == null) return NotFound();

            ViewData["FitnessCenterId"] = new SelectList(_context.FitnessCenters, "Id", "Name", trainer.FitnessCenterId);
            ViewBag.Services = _context.Services.ToList();
            
            return View(trainer);
        }

        // POST: Trainers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Trainer trainer, int[] selectedServices)
        {
            if (id != trainer.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var trainerToUpdate = await _context.Trainers
                        .Include(t => t.Services)
                        .FirstOrDefaultAsync(t => t.Id == id);

                    if (trainerToUpdate == null) return NotFound();

                    // Alanları güncelle
                    trainerToUpdate.FirstName = trainer.FirstName;
                    trainerToUpdate.LastName = trainer.LastName;
                    trainerToUpdate.Email = trainer.Email;
                    trainerToUpdate.Specializations = trainer.Specializations;
                    trainerToUpdate.Description = trainer.Description;
                    trainerToUpdate.FitnessCenterId = trainer.FitnessCenterId;
                    trainerToUpdate.IsActive = trainer.IsActive;

                    // İlişkileri güncelle (Önce temizle, sonra ekle)
                    trainerToUpdate.Services.Clear();
                    if (selectedServices != null)
                    {
                        foreach (var serviceId in selectedServices)
                        {
                            var service = await _context.Services.FindAsync(serviceId);
                            if (service != null) trainerToUpdate.Services.Add(service);
                        }
                    }

                    _context.Update(trainerToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TrainerExists(trainer.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            
            ViewData["FitnessCenterId"] = new SelectList(_context.FitnessCenters, "Id", "Name", trainer.FitnessCenterId);
            ViewBag.Services = _context.Services.ToList();
            return View(trainer);
        }

        // GET: Trainers/AddAvailability/5
        public async Task<IActionResult> AddAvailability(int id)
        {
            var trainer = await _context.Trainers.FindAsync(id);
            if(trainer == null) return NotFound();

            ViewBag.TrainerName = trainer.FirstName + " " + trainer.LastName;
            var model = new TrainerAvailability { TrainerId = id };
            return View(model);
        }

        // POST: Trainers/AddAvailability
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAvailability(TrainerAvailability availability)
        {
            if (ModelState.IsValid)
            {
                if (availability.EndTime <= availability.StartTime)
                {
                    ModelState.AddModelError("EndTime", "Bitiş saati başlangıçtan sonra olmalıdır.");
                }
                else
                {
                    _context.TrainerAvailabilities.Add(availability);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Details", new { id = availability.TrainerId });
                }
            }
            
            var trainer = await _context.Trainers.FindAsync(availability.TrainerId);
            ViewBag.TrainerName = trainer?.FirstName + " " + trainer?.LastName;
            return View(availability);
        }

        // POST: Trainers/DeleteAvailability/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAvailability(int id)
        {
            var availability = await _context.TrainerAvailabilities.FindAsync(id);
            if (availability != null)
            {
                int trainerId = availability.TrainerId;
                _context.TrainerAvailabilities.Remove(availability);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", new { id = trainerId });
            }
            return RedirectToAction(nameof(Index));
        }
        
        // GET: Trainers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var trainer = await _context.Trainers
                .Include(t => t.FitnessCenter)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (trainer == null) return NotFound();
            return View(trainer);
        }

        // POST: Trainers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trainer = await _context.Trainers
                .Include(t => t.Services)
                .Include(t => t.Appointments)
                .FirstOrDefaultAsync(t => t.Id == id);
            
            if (trainer != null) 
            {
                // Aktif randevuları kontrol et
                if (trainer.Appointments.Any(a => a.Status == AppointmentStatus.Approved || a.Status == AppointmentStatus.Pending))
                {
                    TempData["ErrorMessage"] = "Bu antrenörün aktif randevuları olduğu için silinemez.";
                    return RedirectToAction("Trainers", "Admin");
                }

                // TrainerService ilişkilerini temizle (many-to-many)
                trainer.Services.Clear();

                _context.Trainers.Remove(trainer);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Antrenör başarıyla silindi.";
            }
            // Admin panelinden geldiği için Admin/Trainers'a yönlendir
            return RedirectToAction("Trainers", "Admin");
        }

        private bool TrainerExists(int id) => _context.Trainers.Any(e => e.Id == id);
    }
}