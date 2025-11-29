using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitnessCenter.Models
{
    public class Membership
    {
        public int Id { get; set; }

        [Display(Name = "Üye")]
        public string MemberId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Üyelik türü gereklidir.")]
        [Display(Name = "Üyelik Türü")]
        public MembershipType MembershipType { get; set; } = MembershipType.Monthly;

        [Required(ErrorMessage = "Üyelik ücreti gereklidir.")]
        [Display(Name = "Üyelik Ücreti")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Ücret 0'dan büyük olmalıdır.")]
        public decimal Price { get; set; } = 3000; // Aylık 3000 TL

        [Required(ErrorMessage = "Başlangıç tarihi gereklidir.")]
        [Display(Name = "Üyelik Başlangıç Tarihi")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Bitiş tarihi gereklidir.")]
        [Display(Name = "Üyelik Bitiş Tarihi")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Otomatik Yenileme")]
        public bool AutoRenew { get; set; } = false;

        // Kart bilgileri (ödeme için)
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

        // Computed properties
        [NotMapped]
        public int DaysRemaining
        {
            get
            {
                if (!IsActive) return 0;
                var remaining = EndDate - DateTime.Now.Date; // DateTime.Today yerine DateTime.Now.Date kullan
                return Math.Max(0, (int)remaining.TotalDays);
            }
        }

        [NotMapped]
        public double ProgressPercentage
        {
            get
            {
                if (!IsActive) return 0;
                var totalDays = (EndDate - StartDate).TotalDays;
                var usedDays = (DateTime.Now.Date - StartDate).TotalDays;
                if (totalDays <= 0) return 0;
                return Math.Min(100, Math.Max(0, (usedDays / totalDays) * 100));
            }
        }

        [NotMapped]
        public string StatusText
        {
            get
            {
                if (!IsActive) return "Pasif";
                if (DaysRemaining == 0) return "Süresi Doldu";
                if (DaysRemaining <= 7) return $"Kritik - {DaysRemaining} gün";
                return $"Aktif - {DaysRemaining} gün kaldı";
            }
        }

        [NotMapped]
        public string StatusColor
        {
            get
            {
                if (!IsActive) return "secondary";
                if (DaysRemaining == 0) return "danger";
                if (DaysRemaining <= 7) return "warning";
                return "success";
            }
        }
    }

    public enum MembershipType
    {
        [Display(Name = "Aylık Üyelik")]
        Monthly = 1,
        [Display(Name = "3 Aylık Üyelik")]
        Quarterly = 3,
        [Display(Name = "6 Aylık Üyelik")]
        SemiAnnual = 6,
        [Display(Name = "Yıllık Üyelik")]
        Annual = 12
    }

    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum enumValue)
        {
            var displayAttribute = enumValue.GetType()
                .GetField(enumValue.ToString())
                ?.GetCustomAttributes(typeof(DisplayAttribute), false)
                .FirstOrDefault() as DisplayAttribute;

            return displayAttribute?.Name ?? enumValue.ToString();
        }
    }
}
