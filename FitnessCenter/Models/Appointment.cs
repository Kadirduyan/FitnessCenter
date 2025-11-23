using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitnessCenter.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Üye gereklidir.")]
        [Display(Name = "Üye")]
        public string MemberId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Antrenör gereklidir.")]
        [Display(Name = "Antrenör")]
        public int TrainerId { get; set; }

        [Required(ErrorMessage = "Hizmet gereklidir.")]
        [Display(Name = "Hizmet")]
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Randevu tarihi gereklidir.")]
        [Display(Name = "Randevu Tarihi")]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Randevu saati gereklidir.")]
        [Display(Name = "Randevu Saati")]
        [DataType(DataType.Time)]
        public TimeSpan AppointmentTime { get; set; }

        [Display(Name = "Durum")]
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        [Display(Name = "Notlar")]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("MemberId")]
        public virtual ApplicationUser? Member { get; set; }

        [ForeignKey("TrainerId")]
        public virtual Trainer? Trainer { get; set; }

        [ForeignKey("ServiceId")]
        public virtual Service? Service { get; set; }
    }

    public enum AppointmentStatus
    {
        [Display(Name = "Beklemede")]
        Pending = 0,
        [Display(Name = "Onaylandı")]
        Approved = 1,
        [Display(Name = "Reddedildi")]
        Rejected = 2,
        [Display(Name = "Tamamlandı")]
        Completed = 3,
        [Display(Name = "İptal Edildi")]
        Cancelled = 4
    }
}

