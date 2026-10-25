<div align="center">

# FitnessCenter — Yapay Zeka Destekli Spor Salonu Yönetim ve Randevu Platformu

**ASP.NET Core MVC 9.0, Entity Framework Core, RESTful API ve OpenAI Entegrasyonlu Kurumsal Spor Merkezi Otomasyonu**

[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13.0-239120?style=for-the-badge&logo=csharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET_Core-MVC_&_REST_API-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet)
[![EF Core](https://img.shields.io/badge/EF_Core-9.0_ORM-68217A?style=for-the-badge&logo=nuget&logoColor=white)](https://learn.microsoft.com/en-us/ef/core/)
[![SQL Server](https://img.shields.io/badge/SQL_Server-Relational_DB-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![OpenAI](https://img.shields.io/badge/OpenAI-AI_Coach_API-412991?style=for-the-badge&logo=openai&logoColor=white)](https://openai.com/)
[![Bootstrap 5](https://img.shields.io/badge/Bootstrap-5.3-7952B3?style=for-the-badge&logo=bootstrap&logoColor=white)](https://getbootstrap.com/)

</div>

---

## 1. Proje Özeti ve Mimari Yaklaşım

**FitnessCenter**, spor salonlarının hizmet, antrenör (eğitmen), üye ve randevu süreçlerini uçtan uca dijitalleştiren; aynı zamanda üyelerin fiziksel parametrelerine göre **yapay zeka destekli kişiselleştirilmiş antrenman ve beslenme programları** oluşturan katmanlı bir **ASP.NET Core MVC 9.0** web uygulamasıdır.

Sistem; **ASP.NET Core Identity** tabanlı rol yetkilendirme (`Admin` / `Member`), **Entity Framework Core 9.0** Code-First veritabanı mimarisi ve dinamik **LINQ** filtreleme yeteneklerine sahip **RESTful API** uç noktaları ile kurumsal standartlarda yapılandırılmıştır.

---

## 2. Sistem Mimarisi ve Öne Çıkan Özellikler

### 2.1. Temel Modüller
- **Spor Salonu & Hizmet Yönetimi:** Çalışma saatleri, branş/hizmet tanımları (Fitness, Pilates, Yoga, Personal Training vb.), seans süreleri ve dinamik ücretlendirme yönetimi.
- **Antrenör (Trainer) ve Müsaitlik Planlama:** Eğitmen uzmanlık alanları, çalışma takvimleri ve çakışma kontrollü randevu eşleştirme motoru.
- **Randevu ve Onay İş Akışı:** Üyelerin uygun antrenör ve saat dilimlerine randevu oluşturması, yönetici/eğitmen onay mekanizması ve durum takibi.
- **Yapay Zeka Antrenör & Diyetisyen Modülü (OpenAI API):** Kullanıcının boy, kilo, vücut tipi, hedef ve diyet kısıtlamalarını analiz ederek kişiye özel egzersiz ve beslenme programı üreten servis katmanı.
- **Rol Bazlı Yetkilendirme (RBAC):** `Admin` ve `Member` rolleri için özelleştirilmiş kontrol panelleri ve güvenlik filtreleri.

### 2.2. RESTful API Uç Noktaları (LINQ Filtreleme Destekli)

| HTTP Metodu | Endpoint | Açıklama ve Sorgu Parametreleri |
| :---: | :--- | :--- |
| **`GET`** | `/api/TrainersApi` | Tüm antrenörleri listeler (`fitnessCenterId`, `specialization`, `isActive` filtreleri) |
| **`GET`** | `/api/TrainersApi/{id}` | Belirtilen ID'ye sahip antrenörün detaylı profil ve uzmanlık bilgilerini döner |
| **`GET`** | `/api/TrainersApi/available?date={date}` | Seçilen tarih ve saatte müsait olan antrenörleri LINQ sorgusuyla filtreler |
| **`GET`** | `/api/AppointmentsApi` | Randevuları listeler (`memberId`, `trainerId`, `startDate`, `endDate`, `status`) |
| **`GET`** | `/api/AppointmentsApi/{id}` | Belirli bir randevunun tüm ilişkisel detaylarını JSON formatında döner |

---

## 3. Klasör Mimarisi (Directory Tree)

```text
FitnessCenter/
├── Controllers/                # MVC ve REST API Controller sınıfları (Home, Account, Admin, API)
├── Data/
│   └── ApplicationDbContext.cs # EF Core DbContext, Identity ve ilişkisel tablo yapılandırmaları
├── Filters/                    # Özel yetkilendirme ve doğrulama (Action Filter) sınıfları
├── Migrations/                 # EF Core Code-First veritabanı göç (migration) dosyaları
├── Models/                     # Varlık (Entity) ve ViewModel sınıfları
│   ├── ApplicationUser.cs      # Genişletilmiş ASP.NET Core Identity kullanıcı modeli
│   ├── FitnessCenter.cs        # Spor salonu varlık modeli
│   ├── Trainer.cs              # Antrenör ve uzmanlık alanı modeli
│   ├── Service.cs              # Hizmet, süre ve fiyatlandırma modeli
│   └── Appointment.cs          # Randevu ve onay durumu modeli
├── Services/                   # İş mantığı ve OpenAI entegrasyon servisleri
├── Views/                      # Razor View şablonları (Home, Account, Admin, Shared)
├── wwwroot/                    # Statik varlıklar (CSS, JavaScript, Bootstrap 5, Görseller)
├── appsettings.json            # Veritabanı bağlantı dizesi (Connection String) ve API ayarları
├── Program.cs                  # Dependency Injection (DI), Middleware ve Identity yapılandırması
└── FitnessCenter.csproj        # .NET 9.0 proje ve NuGet paket tanımları
```

---

## 4. Kurulum ve Çalıştırma

### 4.1. Ön Gereksinimler
- **.NET 9.0 SDK**
- **SQL Server** (LocalDB veya SQL Server Express)
- **Visual Studio 2022** veya **VS Code**

### 4.2. Adım Adım Kurulum

```bash
# 1. Depoyu klonlayın
git clone https://github.com/Kadirduyan/FitnessCenter.git
cd FitnessCenter

# 2. NuGet paketlerini geri yükleyin ve EF Core göçlerini veritabanına uygulayın
dotnet restore
dotnet ef database update

# 3. Uygulamayı başlatın
dotnet run
```

> **Not:** Yapay zeka öneri modülünü aktif etmek için `appsettings.json` içerisindeki `"OpenAI": { "ApiKey": "..." }` alanına kendi API anahtarınızı ekleyebilirsiniz.

---

## 5. Geliştirici (Author)

**Kadir Duyan** — *Sakarya Üniversitesi Bilgisayar Mühendisliği*  
- **GitHub:** [github.com/Kadirduyan](https://github.com/Kadirduyan)
- **LinkedIn:** [linkedin.com/in/kadir-duyan4](https://www.linkedin.com/in/kadir-duyan4)
- **E-posta:** [kadirduyan482@gmail.com](mailto:kadirduyan482@gmail.com)
