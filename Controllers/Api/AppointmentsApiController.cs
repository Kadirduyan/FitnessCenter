using FitnessCenter.Data;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AppointmentsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AppointmentsApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/AppointmentsApi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAppointments(
            [FromQuery] string? memberId = null,
            [FromQuery] int? trainerId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] AppointmentStatus? status = null)
        {
            var user = await _userManager.GetUserAsync(User);
            var query = _context.Appointments
                .Include(a => a.Member)
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .AsQueryable();

            // LINQ filtering
            if (!User.IsInRole("Admin"))
            {
                query = query.Where(a => a.MemberId == user!.Id);
            }
            else if (!string.IsNullOrEmpty(memberId))
            {
                query = query.Where(a => a.MemberId == memberId);
            }

            if (trainerId.HasValue)
            {
                query = query.Where(a => a.TrainerId == trainerId.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(a => a.AppointmentDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.AppointmentDate <= endDate.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(a => a.Status == status.Value);
            }

            var appointments = await query
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .Select(a => new
                {
                    a.Id,
                    Member = new
                    {
                        a.Member!.Id,
                        a.Member.UserName,
                        a.Member.FirstName,
                        a.Member.LastName,
                        a.Member.Email
                    },
                    Trainer = new
                    {
                        a.Trainer!.Id,
                        FullName = a.Trainer.FullName,
                        a.Trainer.Email
                    },
                    Service = new
                    {
                        a.Service!.Id,
                        a.Service.Name,
                        a.Service.DurationMinutes,
                        a.Service.Price
                    },
                    a.AppointmentDate,
                    a.AppointmentTime,
                    a.Status,
                    a.Notes,
                    a.CreatedAt,
                    a.UpdatedAt
                })
                .ToListAsync();

            return Ok(appointments);
        }

        // GET: api/AppointmentsApi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetAppointment(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Member)
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && appointment.MemberId != user!.Id)
            {
                return Forbid();
            }

            var result = new
            {
                appointment.Id,
                Member = new
                {
                    appointment.Member!.Id,
                    appointment.Member.UserName,
                    appointment.Member.FirstName,
                    appointment.Member.LastName,
                    appointment.Member.Email
                },
                Trainer = new
                {
                    appointment.Trainer!.Id,
                    FullName = appointment.Trainer.FullName,
                    appointment.Trainer.Email,
                },
                Service = new
                {
                    appointment.Service!.Id,
                    appointment.Service.Name,
                    appointment.Service.DurationMinutes,
                    appointment.Service.Price
                },
                appointment.AppointmentDate,
                appointment.AppointmentTime,
                appointment.Status,
                appointment.Notes,
                appointment.CreatedAt,
                appointment.UpdatedAt
            };

            return Ok(result);
        }
    }
}

