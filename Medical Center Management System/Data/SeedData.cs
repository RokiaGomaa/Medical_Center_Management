using Medical_Center_Management_System.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Medical_Center_Management_System.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.MigrateAsync();

            // ============================================================
            // 1. ROLES
            // ============================================================
            string[] roles = { "Admin", "Doctor", "Patient" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // ============================================================
            // 2. SPECIALTIES
            // ============================================================
            if (!context.Specialties.Any())
            {
                context.Specialties.AddRange(
                    new Specialty
                    {
                        Name = "Cardiology",
                        Description = "Diagnosis and treatment of heart diseases."
                    },
                    new Specialty
                    {
                        Name = "Dermatology",
                        Description = "Treatment of skin, hair, and nails."
                    },
                    new Specialty
                    {
                        Name = "Neurology",
                        Description = "Deals with brain and nervous system disorders."
                    },
                    new Specialty
                    {
                        Name = "Pediatrics",
                        Description = "Medical care for children."
                    },
                    new Specialty
                    {
                        Name = "Orthopedics",
                        Description = "Treatment of bones and joints."
                    }
                );

                await context.SaveChangesAsync();
            }

            // ============================================================
            // 3. CLINICS
            // ============================================================
            if (!context.Clinics.Any())
            {
                var cardio = context.Specialties.First(s => s.Name == "Cardiology");
                var derma = context.Specialties.First(s => s.Name == "Dermatology");
                var neuro = context.Specialties.First(s => s.Name == "Neurology");
                var pedia = context.Specialties.First(s => s.Name == "Pediatrics");
                var ortho = context.Specialties.First(s => s.Name == "Orthopedics");

                context.Clinics.AddRange(
                    new Clinic
                    {
                        Name = "Cardiology Clinic",
                        Address = "Nasr City, Cairo",
                        PhoneNumber = "01011112222",
                        Price = 300,
                        MaxPatients = 20,
                        StartTime = new TimeSpan(9, 0, 0),
                        EndTime = new TimeSpan(15, 0, 0),
                        IsOpen = true,
                        SpecialtyId = cardio.SpecialtyId
                    },
                    new Clinic
                    {
                        Name = "Dermatology Clinic",
                        Address = "Heliopolis, Cairo",
                        PhoneNumber = "01022223333",
                        Price = 250,
                        MaxPatients = 25,
                        StartTime = new TimeSpan(10, 0, 0),
                        EndTime = new TimeSpan(16, 0, 0),
                        IsOpen = true,
                        SpecialtyId = derma.SpecialtyId
                    },
                    new Clinic
                    {
                        Name = "Neurology Clinic",
                        Address = "6th October, Giza",
                        PhoneNumber = "01033334444",
                        Price = 400,
                        MaxPatients = 15,
                        StartTime = new TimeSpan(11, 0, 0),
                        EndTime = new TimeSpan(17, 0, 0),
                        IsOpen = true,
                        SpecialtyId = neuro.SpecialtyId
                    },
                    new Clinic
                    {
                        Name = "Pediatrics Clinic",
                        Address = "Maadi, Cairo",
                        PhoneNumber = "01044445555",
                        Price = 200,
                        MaxPatients = 30,
                        StartTime = new TimeSpan(9, 30, 0),
                        EndTime = new TimeSpan(14, 30, 0),
                        IsOpen = true,
                        SpecialtyId = pedia.SpecialtyId
                    },
                    new Clinic
                    {
                        Name = "Orthopedics Clinic",
                        Address = "Dokki, Giza",
                        PhoneNumber = "01055556666",
                        Price = 350,
                        MaxPatients = 18,
                        StartTime = new TimeSpan(10, 30, 0),
                        EndTime = new TimeSpan(16, 30, 0),
                        IsOpen = true,
                        SpecialtyId = ortho.SpecialtyId
                    }
                );

                await context.SaveChangesAsync();
            }

            // ============================================================
            // 4. DOCTORS
            // ============================================================
            var doctors = new[]
            {
                new
                {
                    FullName = "Dr. Ahmed Hassan",
                    Email = "doctor1@med.com",
                    Password = "Doctor@123",
                    Phone = "01070000001",
                    YearsOfExp = 10,
                    Specialty = "Cardiology",
                    Clinic = "Cardiology Clinic"
                },
                new
                {
                    FullName = "Dr. Sara Ali",
                    Email = "doctor2@med.com",
                    Password = "Doctor@123",
                    Phone = "01070000002",
                    YearsOfExp = 7,
                    Specialty = "Dermatology",
                    Clinic = "Dermatology Clinic"
                },
                new
                {
                    FullName = "Dr. Omar Khaled",
                    Email = "doctor3@med.com",
                    Password = "Doctor@123",
                    Phone = "01070000003",
                    YearsOfExp = 12,
                    Specialty = "Neurology",
                    Clinic = "Neurology Clinic"
                },
                new
                {
                    FullName = "Dr. Mona Adel",
                    Email = "doctor4@med.com",
                    Password = "Doctor@123",
                    Phone = "01070000004",
                    YearsOfExp = 5,
                    Specialty = "Pediatrics",
                    Clinic = "Pediatrics Clinic"
                },
                new
                {
                    FullName = "Dr. Karim Nabil",
                    Email = "doctor5@med.com",
                    Password = "Doctor@123",
                    Phone = "01070000005",
                    YearsOfExp = 15,
                    Specialty = "Orthopedics",
                    Clinic = "Orthopedics Clinic"
                },
                new
                {
                    FullName = "Dr. Nour Samy",
                    Email = "doctor6@med.com",
                    Password = "Doctor@123",
                    Phone = "01070000006",
                    YearsOfExp = 6,
                    Specialty = "Cardiology",
                    Clinic = "Cardiology Clinic"
                },
                new
                {
                    FullName = "Dr. Hala Tarek",
                    Email = "doctor7@med.com",
                    Password = "Doctor@123",
                    Phone = "01070000007",
                    YearsOfExp = 8,
                    Specialty = "Dermatology",
                    Clinic = "Dermatology Clinic"
                },
                new
                {
                    FullName = "Dr. Youssef Mahmoud",
                    Email = "doctor8@med.com",
                    Password = "Doctor@123",
                    Phone = "01070000008",
                    YearsOfExp = 11,
                    Specialty = "Neurology",
                    Clinic = "Neurology Clinic"
                },
                new
                {
                    FullName = "Dr. Salma Hassan",
                    Email = "doctor9@med.com",
                    Password = "Doctor@123",
                    Phone = "01070000009",
                    YearsOfExp = 9,
                    Specialty = "Pediatrics",
                    Clinic = "Pediatrics Clinic"
                },
                new
                {
                    FullName = "Dr. Mostafa Ali",
                    Email = "doctor10@med.com",
                    Password = "Doctor@123",
                    Phone = "01070000010",
                    YearsOfExp = 14,
                    Specialty = "Orthopedics",
                    Clinic = "Orthopedics Clinic"
                }
            };

            foreach (var d in doctors)
            {
                if (await userManager.FindByEmailAsync(d.Email) is not null)
                    continue;

                var specialty = context.Specialties.First(s => s.Name == d.Specialty);
                var clinic = context.Clinics.First(c => c.Name == d.Clinic);

                var doctor = new Doctor
                {
                    FullName = d.FullName,
                    PhoneNumber = d.Phone,
                    YearsOfExperience = d.YearsOfExp,
                    SpecialtyId = specialty.SpecialtyId,
                    ClinicId = clinic.ClinicId
                };

                context.Doctors.Add(doctor);
                await context.SaveChangesAsync();

                var user = new ApplicationUser
                {
                    FullName = d.FullName,
                    UserName = d.Email,
                    Email = d.Email,
                    PhoneNumber = d.Phone,
                    DoctorId = doctor.DoctorId,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, d.Password);

                if (result.Succeeded)
                    await userManager.AddToRoleAsync(user, "Doctor");
            }

            // ============================================================
            // 5. PATIENTS
            // ============================================================
            var patients = new[]
            {
                new { FullName = "Ahmed Ali", Email = "patient1@mail.com", Phone = "01180000001", DOB = new DateTime(1995,5,1), Gender = "Male" },
                new { FullName = "Sara Mohamed", Email = "patient2@mail.com", Phone = "01180000002", DOB = new DateTime(1998,3,10), Gender = "Female" },
                new { FullName = "Omar Hassan", Email = "patient3@mail.com", Phone = "01180000003", DOB = new DateTime(1992,11,7), Gender = "Male" },
                new { FullName = "Mona Ali", Email = "patient4@mail.com", Phone = "01180000004", DOB = new DateTime(2000,1,20), Gender = "Female" },
                new { FullName = "Khaled Ibrahim", Email = "patient5@mail.com", Phone = "01180000005", DOB = new DateTime(1987,6,12), Gender = "Male" },
                new { FullName = "Nour Adel", Email = "patient6@mail.com", Phone = "01180000006", DOB = new DateTime(1999,8,9), Gender = "Female" },
                new { FullName = "Youssef Tarek", Email = "patient7@mail.com", Phone = "01180000007", DOB = new DateTime(1993,4,15), Gender = "Male" },
                new { FullName = "Fatma Hassan", Email = "patient8@mail.com", Phone = "01180000008", DOB = new DateTime(1997,12,30), Gender = "Female" },
                new { FullName = "Amr Nabil", Email = "patient9@mail.com", Phone = "01180000009", DOB = new DateTime(1985,9,18), Gender = "Male" },
                new { FullName = "Salma Samy", Email = "patient10@mail.com", Phone = "01180000010", DOB = new DateTime(2001,2,25), Gender = "Female" }
            };

            foreach (var p in patients)
            {
                if (await userManager.FindByEmailAsync(p.Email) is not null)
                    continue;

                var patient = new Patient
                {
                    FullName = p.FullName,
                    PhoneNumber = p.Phone,
                    DateOfBirth = p.DOB,
                    Gender = p.Gender
                };

                context.Patients.Add(patient);
                await context.SaveChangesAsync();

                context.MedicalRecords.Add(new MedicalRecord
                {
                    PatientId = patient.PatientId,
                    BloodType = "O+",
                    HasDiabetes = false,
                    HasHypertension = false,
                    HasHeartDisease = false,
                    HasAllergies = false,
                    Height = 170,
                    Weight = 70
                });

                await context.SaveChangesAsync();

                var user = new ApplicationUser
                {
                    FullName = p.FullName,
                    UserName = p.Email,
                    Email = p.Email,
                    PhoneNumber = p.Phone,
                    PatientId = patient.PatientId,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, "Patient@123");

                if (result.Succeeded)
                    await userManager.AddToRoleAsync(user, "Patient");
            }

            // ============================================================
            // 6. ADMIN
            // ============================================================
            const string adminEmail = "admin@medix.com";
            const string adminPassword = "Admin@12345";

            if (await userManager.FindByEmailAsync(adminEmail) is null)
            {
                var admin = new ApplicationUser
                {
                    FullName = "System Admin",
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(admin, adminPassword);

                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }

            // ============================================================
            // 7. APPOINTMENTS
            // ============================================================
            if (!context.Appointments.Any())
            {
                var doctorsList = context.Doctors.Take(10).ToList();
                var patientsList = context.Patients.Take(10).ToList();
                var clinicsList = context.Clinics.Take(5).ToList();

                context.Appointments.AddRange(
                    new Appointment
                    {
                        AppointmentDate = DateTime.Now.AddDays(-5),
                        Status = "Completed",
                        DoctorId = doctorsList[0].DoctorId,
                        PatientId = patientsList[0].PatientId,
                        ClinicId = clinicsList[0].ClinicId
                    },
                    new Appointment
                    {
                        AppointmentDate = DateTime.Now.AddDays(-2),
                        Status = "Completed",
                        DoctorId = doctorsList[1].DoctorId,
                        PatientId = patientsList[1].PatientId,
                        ClinicId = clinicsList[1].ClinicId
                    },
                    new Appointment
                    {
                        AppointmentDate = DateTime.Now.AddDays(1),
                        Status = "Pending",
                        DoctorId = doctorsList[2].DoctorId,
                        PatientId = patientsList[2].PatientId,
                        ClinicId = clinicsList[2].ClinicId
                    },
                    new Appointment
                    {
                        AppointmentDate = DateTime.Now.AddDays(3),
                        Status = "Cancelled",
                        DoctorId = doctorsList[3].DoctorId,
                        PatientId = patientsList[3].PatientId,
                        ClinicId = clinicsList[3].ClinicId
                    },
                    new Appointment
                    {
                        AppointmentDate = DateTime.Now.AddDays(5),
                        Status = "Pending",
                        DoctorId = doctorsList[4].DoctorId,
                        PatientId = patientsList[4].PatientId,
                        ClinicId = clinicsList[4].ClinicId
                    }
                );

                await context.SaveChangesAsync();
            }

            // ============================================================
            // 8. HISTORIES
            // ============================================================
            if (!context.Histories.Any())
            {
                var completedAppointments = context.Appointments
                    .Where(a => a.Status == "Completed")
                    .ToList();

                foreach (var appointment in completedAppointments)
                {
                    context.Histories.Add(new History
                    {
                        AppointmentId = appointment.AppointmentId,
                        IsFirstVisit = true,
                        Diagnosis = "General checkup diagnosis.",
                        Treatment = "Medication prescribed.",
                        DidTreatmentWork = true,
                        DoctorNotes = "Patient condition improved.",
                        FollowUpDate = DateTime.Now.AddMonths(1),
                        Status = "Stable"
                    });
                }

                await context.SaveChangesAsync();
            }
        }
    }
}