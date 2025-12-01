using FitnessCenter.Data;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Controllers
{
    [Authorize]
    public class MembershipController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MembershipController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Membership
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var membership = await _context.Memberships
                .Include(m => m.Member)
                .FirstOrDefaultAsync(m => m.MemberId == user.Id && m.IsActive);

            if (membership == null)
            {
                // Üyelik yoksa satın alma sayfasına yönlendir
                return RedirectToAction("Purchase");
            }

            return View(membership);
        }

        // GET: Membership/Purchase
        public IActionResult Purchase()
        {
            var membershipTypes = Enum.GetValues(typeof(MembershipType))
                .Cast<MembershipType>()
                .Select(mt => new SelectListItem
                {
                    Value = ((int)mt).ToString(),
                    Text = mt.GetDisplayName(),
                    Selected = mt == MembershipType.Monthly
                })
                .ToList();

            ViewBag.MembershipTypes = membershipTypes;

            var membership = new Membership
            {
                Price = 3000, // Aylık 3000 TL
                MembershipType = MembershipType.Monthly,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(1)
            };

            return View(membership);
        }

        // POST: Membership/Purchase
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Purchase([Bind("MembershipType,CardHolderName,CardNumber,CardExpiryDate,CardCVV,AutoRenew")] Membership membership)
        {
            Console.WriteLine($"=== ÜYELİK SATIN ALMA BAŞLATILDI ===");
            Console.WriteLine($"MembershipType: {membership.MembershipType}");
            Console.WriteLine($"CardHolderName: {membership.CardHolderName}");
            Console.WriteLine($"CardNumber: {membership.CardNumber}");
            Console.WriteLine($"CardExpiryDate: {membership.CardExpiryDate}");
            Console.WriteLine($"CardCVV: {membership.CardCVV}");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("HATA: Kullanıcı bulunamadı");
                return RedirectToAction("Login", "Account");
            }

            Console.WriteLine($"Kullanıcı: {user.Id} - {user.FirstName} {user.LastName}");

            // MemberId'yi manuel olarak valid olarak işaretle (form'dan gelmiyor)
            ModelState.Remove("MemberId");
            membership.MemberId = user.Id;

            // Kullanıcının zaten aktif üyeliği var mı kontrol et
            var existingMembership = await _context.Memberships
                .FirstOrDefaultAsync(m => m.MemberId == user.Id && m.IsActive);

            if (existingMembership != null)
            {
                Console.WriteLine("HATA: Kullanıcının zaten aktif üyeliği var");
                TempData["ErrorMessage"] = "Zaten aktif bir üyeliğiniz bulunmaktadır.";
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                Console.WriteLine($"ModelState HATALARI: {string.Join(", ", errors)}");

                // Hata mesajlarını detaylı göster
                foreach (var error in errors)
                {
                    Console.WriteLine($"HATA: {error}");
                }

                // İlk hatayı TempData'ya koy
                TempData["ErrorMessage"] = "Form verileri geçersiz: " + string.Join(", ", errors);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Üyelik bilgilerini ayarla - ŞİMDİ BAŞLASIN
                    membership.MemberId = user.Id;
                    membership.StartDate = DateTime.Now; // Hemen başlasın
                    membership.EndDate = CalculateEndDate(membership.MembershipType);
                    membership.IsActive = true;
                    membership.CreatedAt = DateTime.Now;

                    // Fiyat hesapla
                    membership.Price = CalculatePrice(membership.MembershipType);

                    Console.WriteLine($"ÜYELİK OLUŞTURULUYOR:");
                    Console.WriteLine($"UserId: {membership.MemberId}");
                    Console.WriteLine($"MembershipType: {membership.MembershipType}");
                    Console.WriteLine($"StartDate: {membership.StartDate}");
                    Console.WriteLine($"EndDate: {membership.EndDate}");
                    Console.WriteLine($"Price: {membership.Price}");

                    _context.Add(membership);
                    var result = await _context.SaveChangesAsync();

                    Console.WriteLine($"SaveChanges sonucu: {result}");
                    Console.WriteLine($"Membership ID: {membership.Id}");

                    // Kaydedildiğini doğrula
                    var savedMembership = await _context.Memberships.FindAsync(membership.Id);
                    if (savedMembership == null)
                    {
                        Console.WriteLine("HATA: Üyelik kaydedilemedi!");
                        ModelState.AddModelError("", "Üyelik kaydedilemedi. Lütfen tekrar deneyin.");
                    }
                    else
                    {
                        Console.WriteLine($"BAŞARILI: Üyelik kaydedildi - ID: {savedMembership.Id}, EndDate: {savedMembership.EndDate}");
                        TempData["SuccessMessage"] = $"Üye olunmuştur. Sağlıklı günler dileriz! Üyeliğiniz {membership.EndDate.ToString("dd.MM.yyyy")} tarihine kadar geçerlidir.";
                        return RedirectToAction("Index");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ÜYELİK HATASI: {ex.Message}");
                    ModelState.AddModelError("", $"Üyelik oluşturulurken bir hata oluştu: {ex.Message}");
                }
            }

            // Hata durumunda dropdown'ı tekrar doldur
            var membershipTypes = Enum.GetValues(typeof(MembershipType))
                .Cast<MembershipType>()
                .Select(mt => new SelectListItem
                {
                    Value = ((int)mt).ToString(),
                    Text = mt.GetDisplayName(),
                    Selected = mt == membership.MembershipType
                })
                .ToList();

            ViewBag.MembershipTypes = membershipTypes;
            return View(membership);
        }

        // POST: Membership/Renew
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Renew(int id)
        {
            var membership = await _context.Memberships.FindAsync(id);
            if (membership == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null || membership.MemberId != user.Id)
            {
                return Forbid();
            }

            try
            {
                // Üyeliği yenile
                membership.EndDate = membership.EndDate.AddMonths((int)membership.MembershipType);
                membership.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Üyeliğiniz başarıyla yenilendi! Yeni bitiş tarihi: {membership.EndDate.ToString("dd.MM.yyyy")}";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Üyelik yenilenirken bir hata oluştu: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: Membership/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var membership = await _context.Memberships.FindAsync(id);
            if (membership == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null || membership.MemberId != user.Id)
            {
                return Forbid();
            }

            try
            {
                membership.IsActive = false;
                membership.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Üyeliğiniz başarıyla iptal edildi.";
                return RedirectToAction("Purchase");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Üyelik iptal edilirken bir hata oluştu: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        private DateTime CalculateEndDate(MembershipType membershipType)
        {
            var startDate = DateTime.Now; // Şu anki zamanı baz al
            return membershipType switch
            {
                MembershipType.Monthly => startDate.AddMonths(1),
                MembershipType.Quarterly => startDate.AddMonths(3),
                MembershipType.SemiAnnual => startDate.AddMonths(6),
                MembershipType.Annual => startDate.AddYears(1),
                _ => startDate.AddMonths(1)
            };
        }

        private decimal CalculatePrice(MembershipType membershipType)
        {
            return membershipType switch
            {
                MembershipType.Monthly => 3000,
                MembershipType.Quarterly => 8000, // 3000 * 3 * 0.9 (indirim)
                MembershipType.SemiAnnual => 15000, // 3000 * 6 * 0.85 (indirim)
                MembershipType.Annual => 28000, // 3000 * 12 * 0.8 (indirim)
                _ => 3000
            };
        }
    }
}
