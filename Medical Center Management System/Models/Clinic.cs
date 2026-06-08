using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Medical_Center_Management_System.Models
{
    public class Clinic
    {
        public int ClinicId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Address { get; set; }

        public string PhoneNumber { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        public int MaxPatients { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        // time span علشان دي اوقات مش تواريخ

        public bool IsOpen { get; set; } = true;


        // ==============================
        // Relation With Specialty
        // One Specialty -> Many Clinics
        // ==============================

        public int SpecialtyId { get; set; }

        public Specialty? Specialty { get; set; }


        // ==============================
        // Relation with Appointment
        // One Clinic -> Many Appointments
        // ==============================

        public ICollection<Appointment> Appointments { get; set; }
            = new List<Appointment>();


        // ==============================
        // Relation with Doctors
        // One Clinic -> Many Doctors
        // ==============================

        public ICollection<Doctor> Doctors { get; set; }
            = new List<Doctor>();
    }
}