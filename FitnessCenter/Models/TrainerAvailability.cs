using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitnessCenter.Models
{
    public class TrainerAvailability
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Antrenör gereklidir.")]
        [Display(Name = "Antrenör")]
        public int TrainerId { get; set; }

        [Required(ErrorMessage = "Gün gereklidir.")]
        [Display(Name = "Gün")]
        [Range(0, 6, ErrorMessage = "Gün 0-6 arasında olmalıdır (0=Pazar, 6=Cumartesi).")]
        public DayOfWeek DayOfWeek { get; set; }

        [Required(ErrorMessage = "Başlangıç saati gereklidir.")]
        [Display(Name = "Başlangıç Saati")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Bitiş saati gereklidir.")]
        [Display(Name = "Bitiş Saati")]
        public TimeSpan EndTime { get; set; }

        // Navigation properties
        [ForeignKey("TrainerId")]
        public virtual Trainer? Trainer { get; set; }
    }
}

