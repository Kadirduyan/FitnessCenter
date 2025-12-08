using FitnessCenter.Data;
using FitnessCenter.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace FitnessCenter.Services
{
    public class AppointmentAutoApprovalService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AppointmentAutoApprovalService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Her 1 dakikada bir kontrol et

        public AppointmentAutoApprovalService(
            IServiceProvider serviceProvider,
            ILogger<AppointmentAutoApprovalService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Randevu Otomatik Onay Servisi başlatıldı.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndApproveAppointments();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Randevu otomatik onay kontrolü sırasında hata oluştu.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Randevu Otomatik Onay Servisi durduruldu.");
        }

        private async Task CheckAndApproveAppointments()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var now = DateTime.Now;

            // Beklemede olan ve randevu tarihi/saati geçmiş randevuları bul
            var appointmentsToApprove = await context.Appointments
                .Where(a => a.Status == AppointmentStatus.Pending
                    && a.AppointmentDate.Date <= now.Date
                    && (a.AppointmentDate.Date < now.Date || 
                        (a.AppointmentDate.Date == now.Date && a.AppointmentTime <= now.TimeOfDay)))
                .ToListAsync();

            if (appointmentsToApprove.Any())
            {
                _logger.LogInformation($"{appointmentsToApprove.Count} randevu otomatik onaylanacak.");

                foreach (var appointment in appointmentsToApprove)
                {
                    appointment.Status = AppointmentStatus.Approved;
                    appointment.UpdatedAt = DateTime.Now;
                    _logger.LogInformation($"Randevu #{appointment.Id} otomatik olarak onaylandı. (Tarih: {appointment.AppointmentDate:dd.MM.yyyy}, Saat: {appointment.AppointmentTime:hh\\:mm})");
                }

                await context.SaveChangesAsync();
                _logger.LogInformation($"{appointmentsToApprove.Count} randevu başarıyla onaylandı.");
            }
        }
    }
}

