using System.ComponentModel.DataAnnotations;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FitnessCenter.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    DateOfBirth = model.DateOfBirth,
                    Address = model.Address,
                    CreatedAt = DateTime.Now
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Assign Member role
                    await _userManager.AddToRoleAsync(user, "Member");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null, bool? isAdminLogin = null)
        {
            // adminLoginValue'yu daha geniş scope'a taşı
            bool adminLoginValue = false;
            LoginViewModel safeModel = model ?? new LoginViewModel();

            // Tüm action'ı try-catch ile sarmala - HTTP 400 hatalarını yakalamak için
            try
            {
                // ViewBag'i en başta set et - güvenli şekilde
                try
                {
                    if (isAdminLogin.HasValue)
                    {
                        adminLoginValue = isAdminLogin.Value;
                    }
                    else if (Request?.Form != null && Request.Form.ContainsKey("IsAdminLogin"))
                    {
                        var isAdminValue = Request.Form["IsAdminLogin"].ToString();
                        adminLoginValue = isAdminValue.ToLower() == "true" || isAdminValue == "1";
                    }
                }
                catch
                {
                    // Hata durumunda false olarak kal
                    adminLoginValue = false;
                }

                safeModel.IsAdminLogin = adminLoginValue;
                ViewBag.IsAdminLogin = adminLoginValue;
                ViewData["ReturnUrl"] = returnUrl;

                if (ModelState.IsValid && !string.IsNullOrEmpty(safeModel.Email) && !string.IsNullOrEmpty(safeModel.Password))
                {
                    Console.WriteLine($"Giriş denemesi: Email={safeModel.Email}, Password Length={safeModel.Password?.Length}");

                    // Password null kontrolü
                    if (string.IsNullOrEmpty(safeModel.Password))
                    {
                        ModelState.AddModelError(string.Empty, "Yanlış e-posta veya şifre.");
                        ViewBag.IsAdminLogin = adminLoginValue;
                        safeModel.IsAdminLogin = adminLoginValue;
                        return View(safeModel);
                    }

                    var result = await _signInManager.PasswordSignInAsync(
                        safeModel.Email, safeModel.Password, safeModel.RememberMe, lockoutOnFailure: false);

                    Console.WriteLine($"PasswordSignIn result: {result}");

                    if (result.Succeeded)
                    {
                        Console.WriteLine("Giriş başarılı, kullanıcı bulunuyor...");

                        try
                        {
                            // Giriş başarılı - kullanıcının rolünü kontrol et
                            var currentUser = await _userManager.FindByEmailAsync(safeModel.Email);
                            Console.WriteLine($"Current user: {currentUser?.Email}");

                            if (currentUser != null)
                            {
                                var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
                                Console.WriteLine($"Is Admin: {isAdmin}");

                                // Form tipi kontrolü - Admin formundan geliyorsa sadece admin giriş yapabilir
                                if (safeModel.IsAdminLogin && !isAdmin)
                                {
                                    try
                                    {
                                        await _signInManager.SignOutAsync();
                                    }
                                    catch { }
                                    ModelState.AddModelError(string.Empty, "Yanlış e-posta veya şifre.");
                                    ViewBag.IsAdminLogin = safeModel.IsAdminLogin;
                                    safeModel.IsAdminLogin = adminLoginValue;
                                    return View(safeModel);
                                }

                                // Üye formundan geliyorsa admin giriş yapamaz
                                if (!safeModel.IsAdminLogin && isAdmin)
                                {
                                    try
                                    {
                                        await _signInManager.SignOutAsync();
                                    }
                                    catch { }
                                    ModelState.AddModelError(string.Empty, "Yanlış e-posta veya şifre.");
                                    ViewBag.IsAdminLogin = safeModel.IsAdminLogin;
                                    safeModel.IsAdminLogin = adminLoginValue;
                                    return View(safeModel);
                                }

                                if (isAdmin)
                                {
                                    // Admin kullanıcısı - Admin paneline yönlendir
                                    Console.WriteLine("Admin giriş başarılı - yönlendirme yapılıyor!");
                                    return RedirectToAction("Index", "Admin");
                                }
                                else
                                {
                                    // Normal üye - ana sayfaya yönlendir
                                    Console.WriteLine("Normal üye girişi - ana sayfaya yönlendirme...");
                                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                                    {
                                        return Redirect(returnUrl);
                                    }
                                    return RedirectToAction("Index", "Home");
                                }
                            }
                            else
                            {
                                // Kullanıcı bulunamadı - çıkış yap ve hata göster
                                Console.WriteLine("Kullanıcı bulunamadı!");
                                try
                                {
                                    await _signInManager.SignOutAsync();
                                }
                                catch { }
                                ModelState.AddModelError(string.Empty, "Yanlış e-posta veya şifre.");
                                ViewBag.IsAdminLogin = adminLoginValue;
                                safeModel.IsAdminLogin = adminLoginValue;
                                return View(safeModel);
                            }
                        }
                        catch (Exception userCheckEx)
                        {
                            // Kullanıcı kontrolü sırasında hata
                            Console.WriteLine($"Kullanıcı kontrolü hatası: {userCheckEx.Message}");
                            try
                            {
                                await _signInManager.SignOutAsync();
                            }
                            catch { }
                            ModelState.AddModelError(string.Empty, "Yanlış e-posta veya şifre.");
                            ViewBag.IsAdminLogin = adminLoginValue;
                            safeModel.IsAdminLogin = adminLoginValue;
                            return View(safeModel);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Geçersiz giriş bilgileri");
                        ModelState.AddModelError(string.Empty, "Yanlış e-posta veya şifre.");
                    }
                }
                else
                {
                    // ModelState geçersizse veya boş alanlar varsa
                    if (string.IsNullOrEmpty(safeModel.Email) || string.IsNullOrEmpty(safeModel.Password))
                    {
                        ModelState.AddModelError(string.Empty, "Lütfen e-posta ve şifre alanlarını doldurun.");
                    }
                    else if (!ModelState.IsValid)
                    {
                        ModelState.AddModelError(string.Empty, "Lütfen tüm alanları doğru şekilde doldurun.");
                    }
                }
            }
            catch (Microsoft.AspNetCore.Antiforgery.AntiforgeryValidationException ex)
            {
                // Anti-forgery token hatası - sayfayı yeniden göster
                Console.WriteLine($"Anti-forgery token hatası: {ex.Message}");
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, "Yanlış e-posta veya şifre.");
                ViewBag.IsAdminLogin = adminLoginValue;
                safeModel.IsAdminLogin = adminLoginValue;
                return View(safeModel);
            }
            catch (Exception ex)
            {
                // Hata durumunda logla ve kullanıcıya bilgi ver
                Console.WriteLine($"Login hatası: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, "Yanlış e-posta veya şifre.");
            }
            finally
            {
                // Her durumda ViewBag'i garantile
                ViewBag.IsAdminLogin = adminLoginValue;
                safeModel.IsAdminLogin = adminLoginValue;
            }

            // Her durumda Login view'ını döndür - ViewBag zaten set edildi
            return View(safeModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Ad gereklidir.")]
        [Display(Name = "Ad")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad gereklidir.")]
        [Display(Name = "Soyad")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta gereklidir.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre gereklidir.")]
        [StringLength(100, ErrorMessage = "Şifre en az {2} karakter uzunluğunda olmalıdır.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Şifre Tekrar")]
        [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Doğum Tarihi")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Adres")]
        public string? Address { get; set; }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "E-posta gereklidir.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre gereklidir.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Beni Hatırla")]
        public bool RememberMe { get; set; }

        [Display(Name = "Admin Girişi")]
        public bool IsAdminLogin { get; set; } = false;
    }
}

