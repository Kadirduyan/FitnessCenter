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

        // GET: Trainers
        public async Task<IActionResult> Index()
        {
            var trainers = await _context.Trainers
                .Include(t => t.FitnessCenter)
                .ToListAsync();
            return View(trainers);
        }

        // GET: Trainers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trainer = await _context.Trainers
                .Include(t => t.FitnessCenter)
                .Include(t => t.Services)
                .Include(t => t.Availabilities)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trainer == null)
            {
                return NotFound();
            }

            return View(trainer);
        }

        // GET: Trainers/Create
        public IActionResult Create()
        {
            ViewData["FitnessCenterId"] = new SelectList(_context.FitnessCenters, "Id", "Name");
            ViewData["ServiceIds"] = new MultiSelectList(_context.Services, "Id", "Name");
            return View();
        }

        // POST: Trainers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,Email,Phone,Specializations,Description,FitnessCenterId,IsActive")] Trainer trainer, int[] selectedServices)
        {
            if (ModelState.IsValid)
            {
                trainer.CreatedAt = DateTime.Now;
                _context.Add(trainer);
                await _context.SaveChangesAsync();

                if (selectedServices != null && selectedServices.Length > 0)
                {
                    trainer.Services = await _context.Services
                        .Where(s => selectedServices.Contains(s.Id))
                        .ToListAsync();
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }
            ViewData["FitnessCenterId"] = new SelectList(_context.FitnessCenters, "Id", "Name", trainer.FitnessCenterId);
            ViewData["ServiceIds"] = new MultiSelectList(_context.Services, "Id", "Name", selectedServices);
            return View(trainer);
        }

        // GET: Trainers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trainer = await _context.Trainers
                .Include(t => t.Services)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trainer == null)
            {
                return NotFound();
            }

            ViewData["FitnessCenterId"] = new SelectList(_context.FitnessCenters, "Id", "Name", trainer.FitnessCenterId);
            ViewData["ServiceIds"] = new MultiSelectList(_context.Services, "Id", "Name", trainer.Services.Select(s => s.Id));
            return View(trainer);
        }

        // POST: Trainers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Email,Phone,Specializations,Description,FitnessCenterId,IsActive,CreatedAt")] Trainer trainer, int[] selectedServices)
        {
            if (id != trainer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingTrainer = await _context.Trainers
                        .Include(t => t.Services)
                        .FirstOrDefaultAsync(t => t.Id == id);

                    if (existingTrainer == null)
                    {
                        return NotFound();
                    }

                    existingTrainer.FirstName = trainer.FirstName;
                    existingTrainer.LastName = trainer.LastName;
                    existingTrainer.Email = trainer.Email;
                    existingTrainer.Phone = trainer.Phone;
                    existingTrainer.Specializations = trainer.Specializations;
                    existingTrainer.Description = trainer.Description;
                    existingTrainer.FitnessCenterId = trainer.FitnessCenterId;
                    existingTrainer.IsActive = trainer.IsActive;

                    // Update services
                    if (selectedServices != null)
                    {
                        existingTrainer.Services = await _context.Services
                            .Where(s => selectedServices.Contains(s.Id))
                            .ToListAsync();
                    }
                    else
                    {
                        existingTrainer.Services.Clear();
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TrainerExists(trainer.Id))
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
            ViewData["FitnessCenterId"] = new SelectList(_context.FitnessCenters, "Id", "Name", trainer.FitnessCenterId);
            ViewData["ServiceIds"] = new MultiSelectList(_context.Services, "Id", "Name", selectedServices);
            return View(trainer);
        }

        // GET: Trainers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trainer = await _context.Trainers
                .Include(t => t.FitnessCenter)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (trainer == null)
            {
                return NotFound();
            }

            return View(trainer);
        }

        // POST: Trainers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trainer = await _context.Trainers.FindAsync(id);
            if (trainer != null)
            {
                _context.Trainers.Remove(trainer);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TrainerExists(int id)
        {
            return _context.Trainers.Any(e => e.Id == id);
        }
    }
}

