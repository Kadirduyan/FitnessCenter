using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitnessCenter.Models
{
    public class Trainer
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ad gereklidir.")]
        [Display(Name = "Ad")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad gereklidir.")]
        [Display(Name = "Soyad")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "E-posta")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string? Email { get; set; }

        [Display(Name = "Telefon")]
        public string? Phone { get; set; }

        [Display(Name = "Uzmanlık Alanları")]
        public string? Specializations { get; set; }

        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        [Display(Name = "Spor Salonu")]
        public int FitnessCenterId { get; set; }

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("FitnessCenterId")]
        public virtual FitnessCenterModel? FitnessCenter { get; set; }
        public virtual ICollection<Service> Services { get; set; } = new List<Service>();
        public virtual ICollection<TrainerAvailability> Availabilities { get; set; } = new List<TrainerAvailability>();
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
}

