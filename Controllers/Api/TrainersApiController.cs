using FitnessCenter.Data;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TrainersApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TrainersApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/TrainersApi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetTrainers(
            [FromQuery] int? fitnessCenterId = null,
            [FromQuery] string? specialization = null,
            [FromQuery] bool? isActive = null)
        {
            var query = _context.Trainers
                .Include(t => t.FitnessCenter)
                .Include(t => t.Services)
                .AsQueryable();

            // LINQ filtering
            if (fitnessCenterId.HasValue)
            {
                query = query.Where(t => t.FitnessCenterId == fitnessCenterId.Value);
            }

            if (!string.IsNullOrEmpty(specialization))
            {
                query = query.Where(t => t.Specializations != null && t.Specializations.Contains(specialization));
            }

            if (isActive.HasValue)
            {
                query = query.Where(t => t.IsActive == isActive.Value);
            }

            var trainers = await query
                .Select(t => new
                {
                    t.Id,
                    t.FirstName,
                    t.LastName,
                    FullName = t.FullName,
                    t.Email,
                    t.Specializations,
                    t.Description,
                    t.IsActive,
                    FitnessCenter = new
                    {
                        t.FitnessCenter!.Id,
                        t.FitnessCenter.Name
                    },
                    Services = t.Services.Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.DurationMinutes,
                        s.Price
                    }).ToList()
                })
                .ToListAsync();

            return Ok(trainers);
        }

        // GET: api/TrainersApi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetTrainer(int id)
        {
            var trainer = await _context.Trainers
                .Include(t => t.FitnessCenter)
                .Include(t => t.Services)
                .Include(t => t.Availabilities)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trainer == null)
            {
                return NotFound();
            }

            var result = new
            {
                trainer.Id,
                trainer.FirstName,
                trainer.LastName,
                FullName = trainer.FullName,
                trainer.Email,
                trainer.Specializations,
                trainer.Description,
                trainer.IsActive,
                FitnessCenter = trainer.FitnessCenter != null ? new
                {
                    trainer.FitnessCenter.Id,
                    trainer.FitnessCenter.Name
                } : null,
                Services = trainer.Services.Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.DurationMinutes,
                    s.Price
                }).ToList(),
                Availabilities = trainer.Availabilities.Select(a => new
                {
                    a.DayOfWeek,
                    a.StartTime,
                    a.EndTime
                }).ToList()
            };

            return Ok(result);
        }

        // GET: api/TrainersApi/available?date=2025-01-15
        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<object>>> GetAvailableTrainers(
            [FromQuery] DateTime date,
            [FromQuery] int? fitnessCenterId = null,
            [FromQuery] int? serviceId = null)
        {
            var dayOfWeek = date.DayOfWeek;

            var query = _context.Trainers
                .Include(t => t.Availabilities)
                .Include(t => t.Services)
                .Where(t => t.IsActive
                    && t.Availabilities.Any(a => a.DayOfWeek == dayOfWeek));

            // LINQ filtering
            if (fitnessCenterId.HasValue)
            {
                query = query.Where(t => t.FitnessCenterId == fitnessCenterId.Value);
            }

            if (serviceId.HasValue)
            {
                query = query.Where(t => t.Services.Any(s => s.Id == serviceId.Value));
            }

            // Check if trainer has appointments at this date
            var trainersWithAppointments = await _context.Appointments
                .Where(a => a.AppointmentDate.Date == date.Date
                    && a.Status != AppointmentStatus.Cancelled
                    && a.Status != AppointmentStatus.Rejected)
                .Select(a => a.TrainerId)
                .Distinct()
                .ToListAsync();

            var availableTrainers = await query
                .Where(t => !trainersWithAppointments.Contains(t.Id))
                .Select(t => new
                {
                    t.Id,
                    t.FirstName,
                    t.LastName,
                    FullName = t.FullName,
                    t.Email,
                    t.Specializations,
                    Availability = t.Availabilities
                        .Where(a => a.DayOfWeek == dayOfWeek)
                        .Select(a => new
                        {
                            a.StartTime,
                            a.EndTime
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(availableTrainers);
        }
    }
}

