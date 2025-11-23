using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Models
{
    public class FitnessCenterModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Spor salonu adı gereklidir.")]
        [Display(Name = "Spor Salonu Adı")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        [Display(Name = "Adres")]
        public string? Address { get; set; }

        [Display(Name = "Telefon")]
        public string? Phone { get; set; }

        [Display(Name = "E-posta")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string? Email { get; set; }

        [Display(Name = "Açılış Saati")]
        [Required(ErrorMessage = "Açılış saati gereklidir.")]
        public TimeSpan OpenTime { get; set; }

        [Display(Name = "Kapanış Saati")]
        [Required(ErrorMessage = "Kapanış saati gereklidir.")]
        public TimeSpan CloseTime { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<Service> Services { get; set; } = new List<Service>();
        public virtual ICollection<Trainer> Trainers { get; set; } = new List<Trainer>();
    }
}

