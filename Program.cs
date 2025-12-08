using FitnessCenter.Data;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings - Admin için basit şifre
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 3;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<FitnessCenter.Filters.AntiforgeryExceptionFilter>();
});
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<FitnessCenter.Services.GeminiService>();

// Background Service: Randevu Otomatik Onay Servisi
builder.Services.AddHostedService<FitnessCenter.Services.AppointmentAutoApprovalService>();

var app = builder.Build();

// Seed initial data
await SeedDataAsync(app);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

async Task SeedDataAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Seed Roles
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }
        if (!await roleManager.RoleExistsAsync("Member"))
        {
            await roleManager.CreateAsync(new IdentityRole("Member"));
        }

        // Seed Admin User
        var adminEmail = "G231210371@sakarya.edu.tr";
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

        Console.WriteLine($"Admin seed başlatıldı. Email: {adminEmail}");

        if (existingAdmin != null)
        {
            Console.WriteLine("Mevcut admin kullanıcısı bulundu, şifre reset ediliyor...");
            // Mevcut admin kullanıcısının şifresini reset et
            var token = await userManager.GeneratePasswordResetTokenAsync(existingAdmin);
            var resetResult = await userManager.ResetPasswordAsync(existingAdmin, token, "sau");

            if (resetResult.Succeeded)
            {
                Console.WriteLine("Admin şifresi başarıyla reset edildi");
            }
            else
            {
                Console.WriteLine($"Şifre reset hatası: {string.Join(", ", resetResult.Errors.Select(e => e.Description))}");
            }

            // Admin rolünü kontrol et
            if (!await userManager.IsInRoleAsync(existingAdmin, "Admin"))
            {
                await userManager.AddToRoleAsync(existingAdmin, "Admin");
                Console.WriteLine("Admin rolü eklendi");
            }
        }
        else
        {
            // Yeni admin kullanıcısını oluştur
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "User",
                CreatedAt = DateTime.Now
            };

            var createResult = await userManager.CreateAsync(adminUser, "sau");
            if (createResult.Succeeded)
            {
                Console.WriteLine("Admin kullanıcısı başarıyla oluşturuldu");
                // Admin rolünü ata
                if (!await roleManager.RoleExistsAsync("Admin"))
                {
                    await roleManager.CreateAsync(new IdentityRole("Admin"));
                    Console.WriteLine("Admin rolü oluşturuldu");
                }
                await userManager.AddToRoleAsync(adminUser, "Admin");
                Console.WriteLine("Admin rolü kullanıcıya atandı");
            }
            else
            {
                Console.WriteLine($"Admin oluşturma hatası: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }
        }

        // Seed Fitness Center
        if (!context.FitnessCenters.Any())
        {
            var fitnessCenter = new FitnessCenter.Models.FitnessCenterModel
            {
                Name = "KADIR Fitness İstanbul",
                Description = "Türkiye'nin ilk tam ölçekli yapay zeka destekli spor salonu. Teknolojiyi kullanarak kişiselleştirilmiş fitness ve sağlığı uygun fiyatlı hale getiriyoruz.",
                Address = "İstanbul, Türkiye",
                Phone = "(555) 123-4567",
                Email = "info@kadirfitness.com",
                OpenTime = new TimeSpan(5, 0, 0), // 05:00
                CloseTime = new TimeSpan(22, 0, 0), // 22:00
                CreatedAt = DateTime.Now
            };
            context.FitnessCenters.Add(fitnessCenter);
            await context.SaveChangesAsync();

            // Seed Services
            var servicesList = new List<FitnessCenter.Models.Service>
            {
                new FitnessCenter.Models.Service { Name = "Kişisel Antrenman", Description = "1-1 özel antrenman seansı", ServiceType = FitnessCenter.Models.ServiceType.PersonalTraining, DurationMinutes = 60, Price = 150, FitnessCenterId = fitnessCenter.Id, CreatedAt = DateTime.Now },
                new FitnessCenter.Models.Service { Name = "Grup Fitness", Description = "Grup halinde yapılan fitness antrenmanı", ServiceType = FitnessCenter.Models.ServiceType.Fitness, DurationMinutes = 45, Price = 80, FitnessCenterId = fitnessCenter.Id, CreatedAt = DateTime.Now },
                new FitnessCenter.Models.Service { Name = "Yoga", Description = "Rahatlatıcı yoga seansı", ServiceType = FitnessCenter.Models.ServiceType.Yoga, DurationMinutes = 60, Price = 100, FitnessCenterId = fitnessCenter.Id, CreatedAt = DateTime.Now },
                new FitnessCenter.Models.Service { Name = "Pilates", Description = "Kore ve esneklik geliştiren pilates dersi", ServiceType = FitnessCenter.Models.ServiceType.Pilates, DurationMinutes = 50, Price = 90, FitnessCenterId = fitnessCenter.Id, CreatedAt = DateTime.Now },
                new FitnessCenter.Models.Service { Name = "Kardiyo Antrenmanı", Description = "Kalp ve damar sağlığı için kardiyo egzersizleri", ServiceType = FitnessCenter.Models.ServiceType.Cardio, DurationMinutes = 45, Price = 70, FitnessCenterId = fitnessCenter.Id, CreatedAt = DateTime.Now },
                new FitnessCenter.Models.Service { Name = "Güç Antrenmanı", Description = "Kas geliştirme ve güç artışı odaklı antrenman", ServiceType = FitnessCenter.Models.ServiceType.Strength, DurationMinutes = 60, Price = 120, FitnessCenterId = fitnessCenter.Id, CreatedAt = DateTime.Now },
                new FitnessCenter.Models.Service { Name = "CrossFit", Description = "Yoğun ve çok yönlü kondisyon antrenmanı", ServiceType = FitnessCenter.Models.ServiceType.CrossFit, DurationMinutes = 60, Price = 130, FitnessCenterId = fitnessCenter.Id, CreatedAt = DateTime.Now },
                new FitnessCenter.Models.Service { Name = "Zumba", Description = "Eğlenceli dans temelli fitness", ServiceType = FitnessCenter.Models.ServiceType.Zumba, DurationMinutes = 45, Price = 85, FitnessCenterId = fitnessCenter.Id, CreatedAt = DateTime.Now },
                new FitnessCenter.Models.Service { Name = "Dövüş Sanatları", Description = "Kendine güven ve fitness için dövüş sanatları", ServiceType = FitnessCenter.Models.ServiceType.MartialArts, DurationMinutes = 60, Price = 110, FitnessCenterId = fitnessCenter.Id, CreatedAt = DateTime.Now },
                new FitnessCenter.Models.Service { Name = "Spinning", Description = "Bisiklet üzerinde yüksek yoğunluklu kardiyo", ServiceType = FitnessCenter.Models.ServiceType.Spinning, DurationMinutes = 45, Price = 75, FitnessCenterId = fitnessCenter.Id, CreatedAt = DateTime.Now }
            };
            context.Services.AddRange(servicesList);
            await context.SaveChangesAsync();

            // Seed Trainers
            var trainers = new List<FitnessCenter.Models.Trainer>
            {
                new FitnessCenter.Models.Trainer
                {
                    FirstName = "Ahmet",
                    LastName = "Yılmaz",
                    Email = "ahmet.yilmaz@kadirfitness.com",
                    Specializations = "Güç Antrenmanı, Kas Geliştirme, Kişisel Antrenman",
                    Description = "10 yıllık deneyimli kişisel antrenör. IFBB sertifikalı. Kas geliştirme ve güç antrenmanı uzmanı.",
                    FitnessCenterId = fitnessCenter.Id,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new FitnessCenter.Models.Trainer
                {
                    FirstName = "Mehmet",
                    LastName = "Kaya",
                    Email = "mehmet.kaya@kadirfitness.com",
                    Specializations = "Kilo Verme, Kardiyo, Yağ Yakımı",
                    Description = "Kilo verme ve kardiyo uzmanı. Beslenme danışmanlığı sertifikası var. Motivasyonel yaklaşımı ile bilinir.",
                    FitnessCenterId = fitnessCenter.Id,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new FitnessCenter.Models.Trainer
                {
                    FirstName = "Merve",
                    LastName = "Öztürk",
                    Email = "merve.ozturk@kadirfitness.com",
                    Specializations = "Yoga, Pilates, Esneklik",
                    Description = "Yoga ve pilates eğitmeni. Rahatlatıcı ve huzurlu dersleriyle bilinir. Meditasyon uzmanı.",
                    FitnessCenterId = fitnessCenter.Id,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new FitnessCenter.Models.Trainer
                {
                    FirstName = "Burak",
                    LastName = "Demir",
                    Email = "burak.demir@kadirfitness.com",
                    Specializations = "CrossFit, Fonksiyonel Antrenman, Kondisyon",
                    Description = "CrossFit Level 2 koç sertifikası. Yoğun ve etkili antrenman programları hazırlar.",
                    FitnessCenterId = fitnessCenter.Id,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new FitnessCenter.Models.Trainer
                {
                    FirstName = "Selin",
                    LastName = "Aydın",
                    Email = "selin.aydin@kadirfitness.com",
                    Specializations = "Zumba, Dans, Eğlence",
                    Description = "Zumba eğitmeni. Eğlenceli ve enerjik dersleriyle motivasyon sağlar.",
                    FitnessCenterId = fitnessCenter.Id,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new FitnessCenter.Models.Trainer
                {
                    FirstName = "Can",
                    LastName = "Şahin",
                    Email = "can.sahin@kadirfitness.com",
                    Specializations = "Dövüş Sanatları, Kendine Güven, Fitness",
                    Description = "Karate ve kick boks uzmanı. Disiplin ve kendine güven geliştirir.",
                    FitnessCenterId = fitnessCenter.Id,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new FitnessCenter.Models.Trainer
                {
                    FirstName = "Eda",
                    LastName = "Yıldız",
                    Email = "eda.yildiz@kadirfitness.com",
                    Specializations = "Pilates, Reformer, Kadın Fitness",
                    Description = "Pilates reformer uzmanı. Kadınların özel ihtiyaçlarına odaklanır.",
                    FitnessCenterId = fitnessCenter.Id,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new FitnessCenter.Models.Trainer
                {
                    FirstName = "Zafer",
                    LastName = "Koç",
                    Email = "zafer.koc@kadirfitness.com",
                    Specializations = "Spinning, Kardiyo, Grup Dersleri",
                    Description = "Spinning eğitmeni. Müzik ve ritim ile motive eden dersler verir.",
                    FitnessCenterId = fitnessCenter.Id,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new FitnessCenter.Models.Trainer
                {
                    FirstName = "Gamze",
                    LastName = "Akın",
                    Email = "gamze.akin@kadirfitness.com",
                    Specializations = "Yoga, Meditasyon, Rahatlama",
                    Description = "Yoga Alliance 500 saat sertifikalı eğitmen. Ruh ve beden dengesi üzerine uzman.",
                    FitnessCenterId = fitnessCenter.Id,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new FitnessCenter.Models.Trainer
                {
                    FirstName = "Aras",
                    LastName = "Tekin",
                    Email = "aras.tekin@kadirfitness.com",
                    Specializations = "Kişisel Antrenman, Beslenme, Yaşam Koçluğu",
                    Description = "Kişisel antrenör ve beslenme uzmanı. Bütünsel yaklaşım ile çalışır.",
                    FitnessCenterId = fitnessCenter.Id,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new FitnessCenter.Models.Trainer
                {
                    FirstName = "Rüzgar",
                    LastName = "Özdemir",
                    Email = "ruzgar.ozdemir@kadirfitness.com",
                    Specializations = "Güç Antrenmanı, Bodybuilding, Rekabet",
                    Description = "Uluslararası bodybuilding yarışmalarında derece almış antrenör. Profesyonel düzeyde antrenman.",
                    FitnessCenterId = fitnessCenter.Id,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new FitnessCenter.Models.Trainer
                {
                    FirstName = "Sahra",
                    LastName = "Çelik",
                    Email = "sahra.celik@kadirfitness.com",
                    Specializations = "Kilo Yönetimi, Beslenme Danışmanlığı, Kadın Sağlığı",
                    Description = "Diyetisyen ve fitness eğitmeni. Kadınların kilo yönetimi ve sağlık ihtiyaçlarına odaklanır.",
                    FitnessCenterId = fitnessCenter.Id,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                }
            };
            context.Trainers.AddRange(trainers);
            await context.SaveChangesAsync();

            // Assign services to trainers
            var personalTrainingService = servicesList.First(s => s.ServiceType == FitnessCenter.Models.ServiceType.PersonalTraining);
            var fitnessService = servicesList.First(s => s.ServiceType == FitnessCenter.Models.ServiceType.Fitness);
            var yogaService = servicesList.First(s => s.ServiceType == FitnessCenter.Models.ServiceType.Yoga);
            var pilatesService = servicesList.First(s => s.ServiceType == FitnessCenter.Models.ServiceType.Pilates);
            var cardioService = servicesList.First(s => s.ServiceType == FitnessCenter.Models.ServiceType.Cardio);
            var strengthService = servicesList.First(s => s.ServiceType == FitnessCenter.Models.ServiceType.Strength);
            var crossfitService = servicesList.First(s => s.ServiceType == FitnessCenter.Models.ServiceType.CrossFit);
            var zumbaService = servicesList.First(s => s.ServiceType == FitnessCenter.Models.ServiceType.Zumba);
            var martialArtsService = servicesList.First(s => s.ServiceType == FitnessCenter.Models.ServiceType.MartialArts);
            var spinningService = servicesList.First(s => s.ServiceType == FitnessCenter.Models.ServiceType.Spinning);

            // Ahmet Yılmaz - Güç antrenmanı uzmanı
            var ahmet = trainers.First(t => t.FirstName == "Ahmet");
            ahmet.Services.Add(personalTrainingService);
            ahmet.Services.Add(strengthService);
            ahmet.Services.Add(fitnessService);

            // Mehmet Kaya - Kilo verme uzmanı
            var mehmet = trainers.First(t => t.FirstName == "Mehmet");
            mehmet.Services.Add(personalTrainingService);
            mehmet.Services.Add(cardioService);
            mehmet.Services.Add(fitnessService);

            // Merve Öztürk - Yoga uzmanı
            var merve = trainers.First(t => t.FirstName == "Merve");
            merve.Services.Add(yogaService);
            merve.Services.Add(pilatesService);

            // Burak Demir - CrossFit uzmanı
            var burak = trainers.First(t => t.FirstName == "Burak");
            burak.Services.Add(crossfitService);
            burak.Services.Add(fitnessService);
            burak.Services.Add(personalTrainingService);

            // Selin Aydın - Zumba uzmanı
            var selin = trainers.First(t => t.FirstName == "Selin");
            selin.Services.Add(zumbaService);
            selin.Services.Add(cardioService);

            // Can Şahin - Dövüş sanatları uzmanı
            var can = trainers.First(t => t.FirstName == "Can");
            can.Services.Add(martialArtsService);
            can.Services.Add(fitnessService);

            // Eda Yıldız - Pilates uzmanı
            var eda = trainers.First(t => t.FirstName == "Eda");
            eda.Services.Add(pilatesService);
            eda.Services.Add(yogaService);

            // Zafer Koç - Spinning uzmanı
            var zafer = trainers.First(t => t.FirstName == "Zafer");
            zafer.Services.Add(spinningService);
            zafer.Services.Add(cardioService);

            // Gamze Akın - Yoga uzmanı
            var gamze = trainers.First(t => t.FirstName == "Gamze");
            gamze.Services.Add(yogaService);
            gamze.Services.Add(pilatesService);

            // Aras Tekin - Kişisel antrenör
            var aras = trainers.First(t => t.FirstName == "Aras");
            aras.Services.Add(personalTrainingService);
            aras.Services.Add(fitnessService);
            aras.Services.Add(strengthService);

            // Rüzgar Özdemir - Bodybuilding uzmanı
            var ruzgar = trainers.First(t => t.FirstName == "Rüzgar");
            ruzgar.Services.Add(strengthService);
            ruzgar.Services.Add(personalTrainingService);
            ruzgar.Services.Add(fitnessService);

            // Sahra Çelik - Kilo yönetimi uzmanı
            var sahra = trainers.First(t => t.FirstName == "Sahra");
            sahra.Services.Add(personalTrainingService);
            sahra.Services.Add(cardioService);
            sahra.Services.Add(fitnessService);

            await context.SaveChangesAsync();

            // Seed Trainer Availabilities
            var availabilities = new List<FitnessCenter.Models.TrainerAvailability>();

            // Pazartesi- Cuma: 06:00-22:00, Cumartesi: 08:00-20:00, Pazar: 10:00-18:00
            foreach (var trainer in trainers)
            {
                // Pazartesi - Cuma
                for (int day = 1; day <= 5; day++)
                {
                    availabilities.Add(new FitnessCenter.Models.TrainerAvailability
                    {
                        TrainerId = trainer.Id,
                        DayOfWeek = (DayOfWeek)day,
                        StartTime = new TimeSpan(6, 0, 0),
                        EndTime = new TimeSpan(22, 0, 0)
                    });
                }

                // Cumartesi
                availabilities.Add(new FitnessCenter.Models.TrainerAvailability
                {
                    TrainerId = trainer.Id,
                    DayOfWeek = DayOfWeek.Saturday,
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(20, 0, 0)
                });

                // Pazar
                availabilities.Add(new FitnessCenter.Models.TrainerAvailability
                {
                    TrainerId = trainer.Id,
                    DayOfWeek = DayOfWeek.Sunday,
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(18, 0, 0)
                });
            }

            context.TrainerAvailabilities.AddRange(availabilities);
            await context.SaveChangesAsync();
        }

        // Mevcut antrenörlere müsaitlik saatleri ekle (eğer yoksa)
        var allTrainers = await context.Trainers.ToListAsync();
        foreach (var trainer in allTrainers)
        {
            var existingAvailabilities = await context.TrainerAvailabilities
                .Where(ta => ta.TrainerId == trainer.Id)
                .ToListAsync();

            if (!existingAvailabilities.Any())
            {
                Console.WriteLine($"Antrenör {trainer.FirstName} {trainer.LastName} için müsaitlik saatleri ekleniyor...");
                var trainerAvailabilities = new List<FitnessCenter.Models.TrainerAvailability>();

                // Pazartesi- Cuma: 06:00-22:00
                for (int day = 1; day <= 5; day++)
                {
                    trainerAvailabilities.Add(new FitnessCenter.Models.TrainerAvailability
                    {
                        TrainerId = trainer.Id,
                        DayOfWeek = (DayOfWeek)day,
                        StartTime = new TimeSpan(6, 0, 0),
                        EndTime = new TimeSpan(22, 0, 0)
                    });
                }

                // Cumartesi: 08:00-20:00
                trainerAvailabilities.Add(new FitnessCenter.Models.TrainerAvailability
                {
                    TrainerId = trainer.Id,
                    DayOfWeek = DayOfWeek.Saturday,
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(20, 0, 0)
                });

                // Pazar: 10:00-18:00
                trainerAvailabilities.Add(new FitnessCenter.Models.TrainerAvailability
                {
                    TrainerId = trainer.Id,
                    DayOfWeek = DayOfWeek.Sunday,
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(18, 0, 0)
                });

                context.TrainerAvailabilities.AddRange(trainerAvailabilities);
                await context.SaveChangesAsync();
                Console.WriteLine($"Antrenör {trainer.FirstName} {trainer.LastName} için müsaitlik saatleri eklendi.");
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}
