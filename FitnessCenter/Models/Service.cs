using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitnessCenter.Models
{
    public class Service
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Hizmet adı gereklidir.")]
        [Display(Name = "Hizmet Adı")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Süre gereklidir.")]
        [Display(Name = "Süre (Dakika)")]
        [Range(1, 480, ErrorMessage = "Süre 1-480 dakika arasında olmalıdır.")]
        public int DurationMinutes { get; set; }

        [Required(ErrorMessage = "Ücret gereklidir.")]
        [Display(Name = "Ücret (TL)")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Ücret 0'dan büyük olmalıdır.")]
        public decimal Price { get; set; }

        [Display(Name = "Spor Salonu")]
        public int FitnessCenterId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("FitnessCenterId")]
        public virtual FitnessCenterModel? FitnessCenter { get; set; }
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<Trainer> Trainers { get; set; } = new List<Trainer>();
    }
}

