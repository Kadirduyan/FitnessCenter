using FitnessCenter.Data;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Controllers
{
    [Authorize(Roles = "Admin")]
    public class FitnessCentersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FitnessCentersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: FitnessCenters
        public async Task<IActionResult> Index()
        {
            return View(await _context.FitnessCenters.ToListAsync());
        }

        // GET: FitnessCenters/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fitnessCenter = await _context.FitnessCenters
                .Include(fc => fc.Services)
                .Include(fc => fc.Trainers)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (fitnessCenter == null)
            {
                return NotFound();
            }

            return View(fitnessCenter);
        }

        // GET: FitnessCenters/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: FitnessCenters/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Address,Phone,Email,OpenTime,CloseTime")] FitnessCenterModel fitnessCenter)
        {
            if (ModelState.IsValid)
            {
                fitnessCenter.CreatedAt = DateTime.Now;
                _context.Add(fitnessCenter);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(fitnessCenter);
        }

        // GET: FitnessCenters/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fitnessCenter = await _context.FitnessCenters.FindAsync(id);
            if (fitnessCenter == null)
            {
                return NotFound();
            }
            return View(fitnessCenter);
        }

        // POST: FitnessCenters/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Address,Phone,Email,OpenTime,CloseTime,CreatedAt")] FitnessCenterModel fitnessCenter)
        {
            if (id != fitnessCenter.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(fitnessCenter);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FitnessCenterExists(fitnessCenter.Id))
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
            return View(fitnessCenter);
        }

        // GET: FitnessCenters/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fitnessCenter = await _context.FitnessCenters
                .FirstOrDefaultAsync(m => m.Id == id);
            if (fitnessCenter == null)
            {
                return NotFound();
            }

            return View(fitnessCenter);
        }

        // POST: FitnessCenters/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var fitnessCenter = await _context.FitnessCenters.FindAsync(id);
            if (fitnessCenter != null)
            {
                _context.FitnessCenters.Remove(fitnessCenter);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FitnessCenterExists(int id)
        {
            return _context.FitnessCenters.Any(e => e.Id == id);
        }
    }
}

