# Sarastya Task Manager - Backend API

REST API backend untuk aplikasi manajemen tugas Sarastya Task Manager. Dibangun dengan ASP.NET Core 8, Entity Framework Core, dan PostgreSQL untuk memberikan layanan backend yang robust dan scalable.

## 📋 Daftar Isi

- [Deskripsi Proyek](#-deskripsi-proyek)
- [Teknologi Utama](#-teknologi-utama)
- [Arsitektur & Pola Desain](#-arsitektur--pola-desain)
- [Setup & Instalasi](#-setup--instalasi)
- [Menjalankan Aplikasi](#-menjalankan-aplikasi)
- [API Endpoints](#-api-endpoints)
- [Struktur Proyek](#-struktur-proyek)
- [Deployment](#-deployment)
- [Akses API](#-akses-api)
- [Environment Variables](#-environment-variables)

---

## 🎯 Deskripsi Proyek

Backend API Sarastya Task Manager menyediakan layanan:

- **Autentikasi & Otorisasi**: Register, login, dan JWT token management
- **Manajemen Tugas**: CRUD operations untuk task items
- **User Management**: Pengelolaan data user
- **Security**: JWT authentication, password hashing dengan BCrypt
- **Database**: PostgreSQL dengan Entity Framework Core ORM
- **API Documentation**: Swagger/OpenAPI untuk dokumentasi interactive

---

## 🛠️ Teknologi Utama

| Teknologi                 | Versi  | Kegunaan                |
| ------------------------- | ------ | ----------------------- |
| **ASP.NET Core**          | 8.0    | Web framework           |
| **.NET SDK**              | 8.0    | Development environment |
| **Entity Framework Core** | 8.0.2  | ORM & Database          |
| **PostgreSQL**            | Latest | Database                |
| **JWT Bearer**            | 8.0.2  | Authentication          |
| **BCrypt.Net**            | 4.0.2  | Password hashing        |
| **Serilog**               | 8.0.1  | Logging                 |
| **Swagger**               | 6.5.0  | API documentation       |

---

## 🏗️ Arsitektur & Pola Desain

### Arsitektur Umum

```
┌─────────────────────────────────────┐
│   Client Applications               │
│   (Web, Mobile, Desktop)            │
└────────────────┬────────────────────┘
                 │
                 ▼
        ┌────────────────┐
        │  API Gateway   │
        │  (ASP.NET Core)│
        └────────┬───────┘
                 │
        ┌────────┴────────┐
        │                 │
        ▼                 ▼
   ┌─────────┐      ┌──────────┐
   │Auth     │      │Tasks     │
   │Service  │      │Service   │
   └────┬────┘      └────┬─────┘
        │                │
        └────────┬───────┘
                 │
                 ▼
        ┌────────────────┐
        │  Entity        │
        │  Framework     │
        │  Core ORM      │
        └────────┬───────┘
                 │
                 ▼
        ┌────────────────┐
        │  PostgreSQL    │
        │  Database      │
        └────────────────┘
```

### Pola Desain yang Diterapkan

#### 1. **Service Layer Pattern** (Business Logic Isolation)

Mengasingkan business logic dari controller untuk maintainability dan testability yang lebih baik:

```csharp
// Services/AuthService.cs
public class AuthService
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;

    // Business logic untuk authentication
    public async Task<AuthResult> Register(RegisterDto dto)
    {
        // Validasi email tidak duplikat
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (existingUser != null)
            throw new ApplicationException("Email already registered");

        // Hash password dengan BCrypt untuk keamanan
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new User
        {
            Email = dto.Email,
            Name = dto.Name,
            PasswordHash = hashedPassword,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Generate JWT token
        var token = _tokenService.GenerateToken(user);

        return new AuthResult
        {
            UserId = user.Id,
            Email = user.Email,
            Name = user.Name,
            Token = token
        };
    }

    // Keuntungan: Business logic terpisah dari HTTP concerns
}
```

#### 2. **Repository Pattern** (Data Access Abstraction)

Entity Framework Core digunakan sebagai repository untuk akses database:

```csharp
// Controllers/AuthController.cs
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ILogger<AuthController> _logger;

    // Dependency Injection dari service layer
    public AuthController(AuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    // Komentar: Service handle business logic, controller handle HTTP response
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        try
        {
            var result = await _authService.Register(dto);
            return Ok(new { token = result.Token, user = result });
        }
        catch (ApplicationException ex)
        {
            _logger.LogWarning($"Registration failed: {ex.Message}");
            return Conflict(new { message = ex.Message });
        }
    }
}
```

#### 3. **JWT Token Management** (Stateless Authentication)

Menggunakan JWT untuk autentikasi tanpa menyimpan session di server:

```csharp
// Services/TokenService.cs
public class TokenService
{
    private readonly IConfiguration _config;

    public string GenerateToken(User user)
    {
        // Claims berisi informasi user yang di-encode dalam token
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name)
        };

        // Key untuk signing token (harus aman & tidak di-share)
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)
        );

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Token expire dalam 24 jam
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

#### 4. **Dependency Injection Pattern** (Loose Coupling)

Service diregistrasi di Program.cs untuk dependency injection:

```csharp
// Program.cs - Configuration
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TaskService>();

// Keuntungan:
// - Loose coupling antar components
// - Mudah untuk testing dengan mock services
// - Configuration terpusat
```

#### 5. **Entity Framework Core Pattern** (ORM)

Abstraksi database menggunakan EF Core untuk type-safety dan maintainability:

```csharp
// Data/AppDbContext.cs
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<TaskItem> Tasks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Konfigurasi relasi one-to-many: One User has many Tasks
        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.User)
            .WithMany(u => u.Tasks)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Validasi data di level model
        modelBuilder.Entity<User>()
            .Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);
    }
}
```

---

## 📦 Setup & Instalasi

### Prasyarat

Pastikan Anda telah menginstal:

- **.NET 8 SDK** - Download dari https://dotnet.microsoft.com/download
- **PostgreSQL** - Download dari https://www.postgresql.org/download/
- **Git** - Version control

### Instalasi Backend API

1. **Clone repository**

```bash
git clone https://github.com/rakha-tech/taskmanager-api.git
cd TaskManager.Api
```

2. **Restore dependencies**

```bash
dotnet restore
```

3. **Setup environment variables**

Buat file `.env` di root direktori:

```bash
# Buat .env file
touch .env  # Linux/Mac
# atau
New-Item -Path .env -ItemType File  # PowerShell Windows
```

Isi file `.env`:

```
# Database
CONNECTIONSTRING=Server=localhost;Port=5432;Database=taskmanager_db;User Id=postgres;Password=your_password;

# JWT Configuration
JWT_SECRET=your_super_secret_key_min_32_characters_long
JWT_ISSUER=https://sarastya-taskmanager.com
JWT_AUDIENCE=https://sarastya-taskmanager.com
JWT_EXPIRES_HOURS=24

# Application
ASPNETCORE_ENVIRONMENT=Development
PORT=3001
ALLOW_CORS_ORIGIN=http://localhost:3000
```

4. **Setup PostgreSQL Database**

```bash
# Login ke PostgreSQL
psql -U postgres

# Di dalam psql console
CREATE DATABASE taskmanager_db;
\q
```

5. **Run Entity Framework Migrations**

```bash
# Apply pending migrations ke database
dotnet ef database update

# Atau jika migration belum exist, create baru
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

## 🚀 Menjalankan Aplikasi

### Mode Development

```bash
dotnet run
```

Backend akan running di `http://localhost:3001`

### Mode Development dengan Hot Reload

```bash
dotnet watch run
```

Kode akan di-compile otomatis saat ada perubahan.

### Build untuk Production

```bash
# Build aplikasi dalam mode Release
dotnet build -c Release

# Run production build
dotnet run -c Release
```

### Akses Swagger Documentation

Buka browser: **http://localhost:3001/swagger**

Swagger UI menyediakan dokumentasi interactive untuk semua API endpoints dengan kemampuan testing langsung.

---

## 🔌 API Endpoints

### Authentication Endpoints

#### 1. Register User

```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "name": "John Doe",
  "password": "SecurePassword123!"
}

Response (201 Created):
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "email": "user@example.com",
    "name": "John Doe"
  }
}
```

#### 2. Login User

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}

Response (200 OK):
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "email": "user@example.com",
    "name": "John Doe"
  }
}
```

### Task Endpoints

#### 3. Create Task

```http
POST /api/tasks
Authorization: Bearer {token}
Content-Type: application/json

{
  "title": "Belajar ASP.NET Core",
  "description": "Pelajari ASP.NET Core untuk backend development",
  "status": "ToDo",
  "priority": "High",
  "dueDate": "2025-12-31T23:59:59Z"
}

Response (201 Created):
{
  "id": 1,
  "title": "Belajar ASP.NET Core",
  "description": "Pelajari ASP.NET Core untuk backend development",
  "status": "ToDo",
  "priority": "High",
  "dueDate": "2025-12-31T23:59:59Z",
  "userId": 1,
  "createdAt": "2025-11-19T10:00:00Z"
}
```

#### 4. Get All Tasks

```http
GET /api/tasks
Authorization: Bearer {token}

Response (200 OK):
[
  {
    "id": 1,
    "title": "Belajar ASP.NET Core",
    "description": "Pelajari ASP.NET Core untuk backend development",
    "status": "ToDo",
    "priority": "High",
    "dueDate": "2025-12-31T23:59:59Z",
    "userId": 1,
    "createdAt": "2025-11-19T10:00:00Z"
  }
]
```

#### 5. Get Single Task

```http
GET /api/tasks/{id}
Authorization: Bearer {token}

Response (200 OK):
{
  "id": 1,
  "title": "Belajar ASP.NET Core",
  ...
}
```

#### 6. Update Task

```http
PUT /api/tasks/{id}
Authorization: Bearer {token}
Content-Type: application/json

{
  "title": "Belajar ASP.NET Core - Updated",
  "status": "InProgress",
  "priority": "High"
}

Response (200 OK): Updated task object
```

#### 7. Delete Task

```http
DELETE /api/tasks/{id}
Authorization: Bearer {token}

Response (204 No Content)
```

---

## 📁 Struktur Proyek

```
TaskManager.Api/
├── Controllers/                      # HTTP Request handlers
│   ├── AuthController.cs            # Register & Login endpoints
│   └── TasksController.cs           # Task CRUD endpoints
│
├── Services/                        # Business logic layer
│   ├── AuthService.cs              # Authentication logic
│   ├── TaskService.cs              # Task management logic
│   └── TokenService.cs             # JWT token generation
│
├── Data/                           # Database layer
│   ├── AppDbContext.cs             # Entity Framework DbContext
│   └── Migrations/                 # Database version control
│       ├── 20251116014716_InitialCreate.cs
│       └── AppDbContextModelSnapshot.cs
│
├── Entities/                       # Database models
│   ├── User.cs                     # User entity
│   └── TaskItem.cs                 # TaskItem entity
│
├── DTOs/                           # Data Transfer Objects
│   ├── RegisterDto.cs              # Register request model
│   ├── LoginDto.cs                 # Login request model
│   ├── AuthResponseDto.cs          # Auth response model
│   └── TaskDto.cs                  # Task DTO
│
├── Program.cs                      # Application configuration & startup
├── TaskManager.Api.csproj          # Project file dengan dependencies
├── appsettings.json                # Configuration (production)
├── appsettings.Development.json    # Configuration (development)
├── Dockerfile                      # Docker configuration
├── .env.example                    # Environment variables template
└── README.md                       # Dokumentasi (file ini)
```

### Penjelasan Key Files

#### Program.cs - Application Startup

```csharp
// Komentar: Konfigurasi awal aplikasi - service registration, middleware setup, etc.

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog - Logging configuration
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// 2. Service Registration - Dependency Injection
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// 3. JWT Authentication - Middleware konfigurasi
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* JWT configuration */ });

// 4. Build app dan setup middleware
var app = builder.Build();
app.UseSwagger();
app.UseCors();
app.UseAuthentication();
app.MapControllers();
app.Run();
```

#### Entities - Database Models

```csharp
// Entities/User.cs
public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; }

    // Relasi one-to-many dengan TaskItem
    public ICollection<TaskItem> Tasks { get; set; } = [];
}

// Entities/TaskItem.cs
public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public TaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }

    // Foreign key ke User
    public int UserId { get; set; }
    public User User { get; set; }
}

// Enums untuk type-safety
public enum TaskStatus { ToDo, InProgress, Done }
public enum TaskPriority { Low, Medium, High }
```

---

## 🐳 Deployment

### Docker Deployment

1. **Build Docker Image**

```bash
docker build -t taskmanager-api:latest .
```

2. **Run Container Locally**

```bash
docker run -p 3001:8080 \
  -e CONNECTIONSTRING="Server=db;Port=5432;Database=taskmanager_db;User Id=postgres;Password=postgres;" \
  -e JWT_SECRET="your_secret_key" \
  taskmanager-api:latest
```

3. **Using Docker Compose** (with PostgreSQL)

```yaml
# docker-compose.yml
version: "3.8"
services:
  db:
    image: postgres:15
    environment:
      POSTGRES_DB: taskmanager_db
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  api:
    build: .
    ports:
      - "3001:8080"
    environment:
      CONNECTIONSTRING: "Server=db;Port=5432;Database=taskmanager_db;User Id=postgres;Password=postgres;"
      JWT_SECRET: "your_secret_key"
    depends_on:
      - db
    volumes:
      - postgres_data

volumes:
  postgres_data:
```

```bash
# Run dengan docker compose
docker-compose up
```

### Railway Deployment

1. **Connect Repository ke Railway**

   - Login di https://railway.app/
   - Create New Project → GitHub Repo
   - Pilih repository TaskManager.Api

2. **Configure Environment Variables**

   - Tambah `RAILWAY_DATABASE_URL` untuk PostgreSQL
   - Tambah `JWT_SECRET` dan JWT settings lainnya

3. **Deploy**
   - Push ke GitHub → Railway automatically deploy

Backend akan accessible di `https://your-project.railway.app`

### Heroku Deployment (Legacy)

```bash
# Login ke Heroku
heroku login

# Create app
heroku create taskmanager-api

# Add PostgreSQL add-on
heroku addons:create heroku-postgresql:hobby-dev

# Set environment variables
heroku config:set JWT_SECRET=your_secret_key

# Deploy
git push heroku main
```

---

## 🔌 Akses API

### Development Lokal

```
Base URL: http://localhost:3001
Swagger UI: http://localhost:3001/swagger
API v1: http://localhost:3001/api
```

### Menggunakan API dengan cURL

```bash
# Register
curl -X POST http://localhost:3001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","name":"John","password":"Pass123!"}'

# Login
curl -X POST http://localhost:3001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"Pass123!"}'

# Get tasks (replace TOKEN dengan JWT token dari login)
curl -X GET http://localhost:3001/api/tasks \
  -H "Authorization: Bearer TOKEN"

# Create task
curl -X POST http://localhost:3001/api/tasks \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"title":"My Task","description":"Task desc","priority":"High","status":"ToDo"}'
```

### Menggunakan API dengan Postman

1. Import collection dari `TaskManager.Api.http` file
2. Set `{{baseUrl}}` variable ke `http://localhost:3001`
3. Set `{{token}}` setelah login
4. Test semua endpoints

---

## 🔐 Environment Variables

| Variable                 | Default               | Kegunaan                                          |
| ------------------------ | --------------------- | ------------------------------------------------- |
| `CONNECTIONSTRING`       | -                     | PostgreSQL connection string                      |
| `JWT_SECRET`             | -                     | Secret key untuk signing JWT token (min 32 chars) |
| `JWT_ISSUER`             | -                     | Token issuer identifier                           |
| `JWT_AUDIENCE`           | -                     | Token audience identifier                         |
| `JWT_EXPIRES_HOURS`      | 24                    | Token expiration time (jam)                       |
| `ASPNETCORE_ENVIRONMENT` | Development           | Environment (Development/Production)              |
| `PORT`                   | 5000                  | Server port                                       |
| `ALLOW_CORS_ORIGIN`      | http://localhost:3000 | Frontend URL untuk CORS                           |

---

## 🐛 Troubleshooting

| Masalah                   | Solusi                                                         |
| ------------------------- | -------------------------------------------------------------- |
| Database connection error | Pastikan PostgreSQL running dan connection string benar        |
| JWT token expired         | Token expires dalam 24 jam, user harus login ulang             |
| 401 Unauthorized          | Pastikan header `Authorization: Bearer {token}` ada di request |
| CORS error                | Configure `ALLOW_CORS_ORIGIN` environment variable             |
| Port already in use       | Ubah PORT atau kill proses yang menggunakan port tersebut      |

---

## 📚 Resources

- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [JWT Authentication](https://tools.ietf.org/html/rfc7519)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Serilog Documentation](https://serilog.net/)

---

## 📄 License

Project ini dilisensikan di bawah MIT License.

---

## 👥 Tim Pengembang

- **Rakha Tech** - Lead Backend Developer

---

## 📞 Kontak & Support

- Email: mrakha.tech@gmail.com
- GitHub Issues: https://github.com/rakha-tech/taskmanager-api/issues

---

**Last Updated**: November 19, 2025

Untuk pertanyaan atau kontribusi, silakan buka issue atau pull request di repository ini.
