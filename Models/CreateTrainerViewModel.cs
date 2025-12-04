using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Models
{
    public class CreateTrainerViewModel
    {
        [Required(ErrorMessage = "Ad gereklidir.")]
        [Display(Name = "Ad")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad gereklidir.")]
        [Display(Name = "Soyad")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "E-posta")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Uzmanlık alanları gereklidir.")]
        [Display(Name = "Uzmanlık Alanları")]
        public string? Specializations { get; set; }

        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Spor salonu seçimi gereklidir.")]
        [Display(Name = "Spor Salonu")]
        public int FitnessCenterId { get; set; }

        [Required(ErrorMessage = "Çalışma başlangıç saati gereklidir.")]
        [Display(Name = "Çalışma Başlangıç Saati")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; } = new TimeSpan(6, 0, 0); // 06:00

        [Required(ErrorMessage = "Çalışma bitiş saati gereklidir.")]
        [Display(Name = "Çalışma Bitiş Saati")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; } = new TimeSpan(22, 0, 0); // 22:00

        [Display(Name = "Verdiği Hizmetler")]
        public int[]? SelectedServices { get; set; }
    }
}
