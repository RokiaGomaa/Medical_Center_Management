using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Medical_Center_Management_System.Models
{
    // Changed base class from DbContext to IdentityDbContext<ApplicationUser>
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        protected AppDbContext() { }

        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Clinic> Clinics { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Specialty> Specialties { get; set; }
        public DbSet<MedicalRecord> MedicalRecords { get; set; }
        public DbSet<History> Histories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Must call base — this sets up all Identity tables
            base.OnModelCreating(modelBuilder);

            // =========================================
            // ApplicationUser -> Patient (optional link)
            // =========================================
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Patient)
                .WithOne()
                .HasForeignKey<ApplicationUser>(u => u.PatientId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // =========================================
            // ApplicationUser -> Doctor (optional link)
            // =========================================
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Doctor)
                .WithOne()
                .HasForeignKey<ApplicationUser>(u => u.DoctorId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // =========================================
            // UNIQUE Phone Number For Doctor
            // =========================================
            modelBuilder.Entity<Doctor>()
                .HasIndex(d => d.PhoneNumber)
                .IsUnique();

            // =========================================
            // Patient -> MedicalRecord (one-to-one)
            // =========================================
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.MedicalRecord)
                .WithOne(m => m.Patient)
                .HasForeignKey<MedicalRecord>(m => m.PatientId)
                .OnDelete(DeleteBehavior.NoAction);

            // =========================================
            // Appointment -> History (one-to-one, optional)
            // =========================================
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.History)
                .WithOne(h => h.Appointment)
                .HasForeignKey<History>(h => h.AppointmentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            // =========================================
            // Appointment -> Doctor
            // =========================================
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.NoAction);

            // =========================================
            // Appointment -> Patient
            // =========================================
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.NoAction);

            // =========================================
            // Appointment -> Clinic
            // =========================================
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Clinic)
                .WithMany(c => c.Appointments)
                .HasForeignKey(a => a.ClinicId)
                .OnDelete(DeleteBehavior.NoAction);

            // =========================================
            // Doctor -> Clinic
            // =========================================
            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.Clinic)
                .WithMany(c => c.Doctors)
                .HasForeignKey(d => d.ClinicId)
                .OnDelete(DeleteBehavior.NoAction);

            // =========================================
            // Doctor -> Specialty
            // =========================================
            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.Specialty)
                .WithMany(s => s.Doctors)
                .HasForeignKey(d => d.SpecialtyId)
                .OnDelete(DeleteBehavior.NoAction);

            // =========================================
            // Clinic -> Specialty
            // =========================================
            modelBuilder.Entity<Clinic>()
                .HasOne(c => c.Specialty)
                .WithMany(s => s.Clinics)
                .HasForeignKey(c => c.SpecialtyId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
