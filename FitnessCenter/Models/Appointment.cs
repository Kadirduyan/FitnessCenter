using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitnessCenter.Models
{
    public class Appointment
    {
        public int Id { get; set; }

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

        // Kart Bilgileri
        [Required(ErrorMessage = "Kart sahibi adı gereklidir.")]
        [Display(Name = "Kart Sahibi Adı")]
        [StringLength(100, ErrorMessage = "Kart sahibi adı en fazla 100 karakter olabilir.")]
        public string CardHolderName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kart numarası gereklidir.")]
        [Display(Name = "Kart Numarası")]
        [RegularExpression(@"^\d{16}$", ErrorMessage = "Kart numarası 16 haneli olmalıdır.")]
        public string CardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Son kullanma tarihi gereklidir.")]
        [Display(Name = "Son Kullanma Tarihi (AA/YY)")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$", ErrorMessage = "Geçerli bir tarih formatı girin (örn: 12/25).")]
        public string CardExpiryDate { get; set; } = string.Empty;

        [Required(ErrorMessage = "CVV kodu gereklidir.")]
        [Display(Name = "CVV Kodu")]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV kodu 3-4 haneli olmalıdır.")]
        public string CardCVV { get; set; } = string.Empty;

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

