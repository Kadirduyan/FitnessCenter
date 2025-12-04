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

        [NotMapped]
        public string PhotoUrl
        {
            get
            {
                switch (FirstName)
                {
                    case "Ahmet":
                        return "https://c.superprof.com/i/m/32540448/600/20240827210733/32540448.webp";
                    case "Mehmet":
                        return "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQ5dVF5ccEAYr5RZLpMXxyp6xCzybwTuF_e4g&s";
                    case "Burak":
                        return "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcStSJ2c_4nm0fuA8Q7RMHE9yqo8naKLSiYBLA&s";
                    case "Can":
                        return "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTnk7HMwsjezVPG6qJpzkB8fS1N472Pgs7bdw&s";
                    case "Zafer":
                        return "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTCfitLZii4Vi3GtYgubK1csGdK-fm2xynoFA&s";
                    case "Aras":
                        return "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTk7K1xKtK-7znRZPm1KF-Km0bd0VTuINzISQ&s";
                    case "Rüzgar":
                        return "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSrFrDiz8JdfcvHw1fCA2dkydO-9z0sO8kKjg&s";
                    case "Merve":
                        return "https://www.ozeldersalani.com/images/teachers/1740636903.jpg";
                    case "Sahra":
                        return "https://foto.haberler.com/haber/2020/09/25/alica-schmidt-borussia-dortmund-un-yeni-fitness-13627211_3187_m.jpg";
                    case "Eda":
                        return "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcT0B8ZEcrGxMxuS1fwt5UXqIxK-g7mEiWMdTg&s";
                    case "Selin":
                        return "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQ-QPj9vg-hfYp7gqAKnYSUWto_2rLbme0OWg&s";
                    case "Gamze":
                        return "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTV3mTudTNmRSYTcBbdT9k7mpIUHCRwazBEMA&s";
                    default:
                        return string.Empty;
                }
            }
        }
    }
}

