# 🏥 Medix — Medical Center Management System

A web application for managing a multi-clinic medical center, built with **ASP.NET Core MVC (.NET 8)**, **Entity Framework Core** and **SQL Server**. It gives each type of user — Admin, Doctor and Patient — their own portal for handling clinics, appointments, medical records and visit history.

---

## ✨ Features

### 👤 Authentication & Authorization

* Registration and login built on **ASP.NET Core Identity**
* Three roles: **Admin**, **Doctor**, **Patient**
* Role-based redirect after login and role-restricted controllers
* Password policy (min. 8 chars + at least one digit), account lockout after 5 failed attempts (10 minutes)
* Cookie authentication with 8-hour sliding expiration
* Self-registration creates a **Patient** account and its linked patient record in one step

### 🛠️ Admin

* Dashboard with totals (patients, doctors, clinics, appointments), today's appointments and the latest bookings
* Full CRUD for **Specialties**, **Clinics**, **Doctors**, **Patients**, **Appointments**, **Medical Records** and **Histories**
* Search, filtering, sorting and pagination on the list pages
* **User management**: list all users with their roles, promote a user to Doctor by linking them to a doctor record, delete users
* View available appointment slots per clinic/doctor for any date
* Export a patient report as **PDF**

### 🩺 Doctor Portal

* Personal dashboard and profile
* List of own appointments, with the ability to update an appointment's status
* Patient profile view
* Create / update the patient's **medical record**
* Write the visit **history** (diagnosis, treatment, notes, follow-up date) for an appointment
* Generate a patient **PDF report**

### 🧑‍⚕️ Patient Portal

* Personal dashboard and profile
* Browse **available time slots** and book an appointment
* View own appointments

### 📅 Booking Logic

* Appointments are split into **30-minute slots** within the clinic's working hours
* Overlapping slots for the same doctor are prevented; cancelled appointments free their slot
* Each clinic has a **maximum number of patients** that is enforced when booking
* Appointment statuses: `Pending`, `Completed`, `Cancelled`

---

## 🧱 Tech Stack

| Layer      | Technology                                                                         |
| ---------- | ---------------------------------------------------------------------------------- |
| Framework  | ASP.NET Core MVC, .NET 8 (C#)                                                      |
| ORM        | Entity Framework Core 8 (Code First + Migrations)                                  |
| Database   | Microsoft SQL Server                                                               |
| Auth       | ASP.NET Core Identity                                                              |
| PDF export | [Rotativa.AspNetCore](https://github.com/webgio/Rotativa.AspNetCore) (wkhtmltopdf) |
| Front-end  | Razor Views, Bootstrap, jQuery, jQuery Validation                                  |

---

## 🗂️ Data Model

```text
Specialty 1 ──── * Clinic 1 ──── * Doctor * ──── 1 Specialty
                      │               │
                      └── * Appointment * ──┘
                              │   *
                              │   └── 1 Patient 1 ──── 1 MedicalRecord
                              └── 0..1 History

ApplicationUser (Identity) ── 0..1 Patient
                           └─ 0..1 Doctor
```

| Entity            | Description                                                                              |
| ----------------- | ---------------------------------------------------------------------------------------- |
| `ApplicationUser` | Identity user with `FullName`, optionally linked to a `Patient` **or** a `Doctor`        |
| `Specialty`       | Medical specialty (name, description)                                                    |
| `Clinic`          | Name, address, phone, price, max patients, opening/closing time, open flag               |
| `Doctor`          | Name, unique phone number, years of experience, specialty, clinic                        |
| `Patient`         | Name, phone, date of birth, gender                                                       |
| `Appointment`     | Date/time, status, doctor, patient, clinic                                               |
| `MedicalRecord`   | Blood type, chronic conditions, allergies, medications, height, weight (one per patient) |
| `History`         | Per-visit diagnosis, treatment, outcome, doctor notes, follow-up date                    |

Phone numbers are validated against Egyptian mobile format (`01[0-2,5]xxxxxxxx`).

---

## 📁 Project Structure

```text
Medical Center Management System/
├── Controllers/        # MVC controllers (Admin, Auth, Doctors, PatientPortal, DoctorPortal, ...)
├── Data/
│   └── SeedData.cs     # Applies migrations, seeds roles, demo data and the admin account
├── Migrations/         # EF Core migrations
├── Models/             # Entities + AppDbContext
├── ViewModels/         # View models (auth, dashboard, pagination, ...)
├── Views/              # Razor views grouped per controller
├── wwwroot/            # Static files, CSS, JS, libs, Rotativa/wkhtmltopdf.exe
├── appsettings.json    # Configuration & connection string
└── Program.cs          # App startup & service configuration
```

---

## 🚀 Getting Started

### Prerequisites

* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* SQL Server (LocalDB, Express, or a full instance)
* Visual Studio 2022 / VS Code / Rider *(optional)*
* Windows is recommended — the bundled `wkhtmltopdf.exe` used for PDF export is a Windows binary

### 1. Clone the repository

```bash
git clone https://github.com/YOUR-USERNAME/YOUR-REPOSITORY.git
cd YOUR-REPOSITORY
```

### 2. Configure the database connection

Edit `appsettings.json` (or better, use [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)) and point `DefaultConnection` to your SQL Server:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=WDT;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

For example, with SQL Server LocalDB:

```text
Server=(localdb)\\MSSQLLocalDB;Database=WDT;Trusted_Connection=True;TrustServerCertificate=True;
```

### 3. Run the app

```bash
cd "Medical Center Management System"
dotnet restore
dotnet run
```

On startup the app **automatically applies the EF Core migrations and seeds** the database, so no manual `dotnet ef database update` is needed.

Default URLs (from `launchSettings.json`):

* HTTP: `http://localhost:5238`
* HTTPS: `https://localhost:7097`

---

## 🔑 Demo Accounts

Demo accounts are created automatically by `SeedData` on first run.

| Role    | Email                                      |
| ------- | ------------------------------------------ |
| Admin   | `admin@medix.com`                          |
| Doctor  | `doctor1@med.com` … `doctor10@med.com`     |
| Patient | `patient1@mail.com` … `patient10@mail.com` |

> ⚠️ **Security:** Demo credentials are configured in `SeedData` for local development only. Do not use development credentials in production.

The seed data also includes 5 specialties (Cardiology, Dermatology, Neurology, Pediatrics, Orthopedics), 5 clinics, 10 doctors, 10 patients, and sample appointments, medical records and histories.

---

## 🗃️ Database Migrations

```bash
# Add a new migration
dotnet ef migrations add MigrationName

# Apply migrations manually (also done automatically on startup)
dotnet ef database update
```

---

## 📄 PDF Reports

Patient reports are generated with Rotativa and `wkhtmltopdf`. The executable is expected at `wwwroot/Rotative/wkhtmltopdf.exe` (configured in `Program.cs`). If you run on Linux/macOS or Docker, install the matching `wkhtmltopdf` binary and update the Rotativa setup accordingly.

---

## 🗺️ Roadmap Ideas

* Email / SMS appointment reminders
* Online payment for clinic fees
* Doctor working-day schedules and vacations
* Email confirmation and password reset
* Unit and integration tests
