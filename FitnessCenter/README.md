# Fitness Center - Spor Salonu Yönetim ve Randevu Sistemi

ASP.NET Core MVC kullanılarak geliştirilmiş, yapay zeka destekli spor salonu yönetim ve randevu sistemi.

## 🎯 Proje Özellikleri

### Temel Özellikler
- ✅ Spor salonu tanımlamaları (çalışma saatleri, hizmet türleri, süre ve ücretler)
- ✅ Antrenör (Eğitmen) yönetimi (uzmanlık alanları, müsaitlik saatleri)
- ✅ Üye ve randevu sistemi
- ✅ Randevu onay mekanizması
- ✅ REST API desteği (LINQ sorguları ile filtreleme)
- ✅ Yapay zeka entegrasyonu (OpenAI API - yakında eklenecek)

### Teknik Özellikler
- ✅ CRUD işlemleri (Tüm temel varlıklar için)
- ✅ Veri doğrulama (İstemci ve sunucu tarafı)
- ✅ Admin paneli
- ✅ Kullanıcı kayıt/üye olma
- ✅ Rol bazlı yetkilendirme (Admin, Member)
- ✅ Modern ve responsive tasarım (Bootstrap 5)

## 🛠️ Kullanılan Teknolojiler

- **ASP.NET Core MVC 9.0** (LTS)
- **C#**
- **SQL Server** (LocalDB)
- **Entity Framework Core 9.0** (ORM, LINQ)
- **Bootstrap 5** (Front-end framework)
- **Identity Framework** (Kimlik doğrulama ve yetkilendirme)
- **HTML5, CSS3, JavaScript, jQuery**

## 📋 Gereksinimler

- .NET 9.0 SDK
- SQL Server (LocalDB veya SQL Server Express)
- Visual Studio 2022 veya Visual Studio Code

## 🚀 Kurulum ve Çalıştırma

### 1. Projeyi Klonlayın
```bash
git clone <repository-url>
cd FitnessCenter
```

### 2. Veritabanını Oluşturun
```bash
dotnet ef database update
```

### 3. Projeyi Çalıştırın
```bash
dotnet run
```

Veya Visual Studio'da F5 tuşuna basarak çalıştırabilirsiniz.

### 4. Varsayılan Admin Girişi
- **Email:** ogrencinumarasi@sakarya.edu.tr
- **Şifre:** sau
- **Rol:** Admin

## 📁 Proje Yapısı

```
FitnessCenter/
├── Controllers/          # Controller sınıfları
│   ├── AccountController.cs
│   ├── HomeController.cs
│   └── ...
├── Models/              # Veri modelleri
│   ├── ApplicationUser.cs
│   ├── FitnessCenter.cs
│   ├── Trainer.cs
│   ├── Service.cs
│   ├── Appointment.cs
│   └── ...
├── Data/                # Veritabanı context
│   └── ApplicationDbContext.cs
├── Views/               # Razor view'lar
│   ├── Home/
│   ├── Account/
│   └── Shared/
├── wwwroot/            # Statik dosyalar (CSS, JS, images)
└── Program.cs          # Uygulama yapılandırması
```

## 🎨 Tasarım

Proje, modern ve kullanıcı dostu bir arayüze sahiptir. Tasarım [Fred Fitness](https://fredfitness.com/) web sitesinden ilham alınarak oluşturulmuştur.

### Özellikler:
- Responsive tasarım (Mobil uyumlu)
- Modern gradient renkler
- Smooth animasyonlar
- Bootstrap 5 komponentleri
- Bootstrap Icons

## 🔐 Roller ve Yetkilendirme

### Admin
- Tüm CRUD işlemlerine erişim
- Spor salonu, antrenör, hizmet yönetimi
- Randevu onaylama/reddetme
- Kullanıcı yönetimi

### Member (Üye)
- Randevu oluşturma
- Kendi randevularını görüntüleme
- Profil yönetimi

## 📝 API Endpoints

REST API endpoint'leri:

### Trainers API
- `GET /api/TrainersApi` - Tüm antrenörleri listele (filtreleme: fitnessCenterId, specialization, isActive)
- `GET /api/TrainersApi/{id}` - Belirli bir antrenör detayları
- `GET /api/TrainersApi/available?date={date}` - Belirli tarihte uygun antrenörler

### Appointments API
- `GET /api/AppointmentsApi` - Randevuları listele (filtreleme: memberId, trainerId, startDate, endDate, status)
- `GET /api/AppointmentsApi/{id}` - Belirli bir randevu detayları

Tüm API endpoint'leri LINQ sorguları ile filtreleme desteği sunar.

## 🤖 Yapay Zeka Entegrasyonu

OpenAI API entegrasyonu ile yapay zeka destekli öneriler:
- **Egzersiz Önerileri**: Boy, kilo, vücut tipi, hedef ve fitness seviyesine göre kişiselleştirilmiş egzersiz planı
- **Diyet Planı Önerileri**: Boy, kilo, hedef ve diyet kısıtlamalarına göre özel beslenme planı

### OpenAI API Yapılandırması

1. `appsettings.json` dosyasına OpenAI API anahtarınızı ekleyin:
```json
"OpenAI": {
  "ApiKey": "sk-your-api-key-here"
}
```

2. OpenAI API anahtarı almak için: https://platform.openai.com/api-keys

## 📄 Lisans

Bu proje eğitim amaçlı geliştirilmiştir.

## 👥 Geliştirici

- Öğrenci Numarası: ogrencinumarasi@sakarya.edu.tr

## 📞 İletişim

Sorularınız için: info@fitnesscenter.com

---

**Not:** Bu proje, Web Programlama dersi kapsamında geliştirilmiştir.

