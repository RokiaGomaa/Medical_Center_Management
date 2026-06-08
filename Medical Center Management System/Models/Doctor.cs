using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Medical_Center_Management_System.Models
{
    public class Doctor
    {
        public int DoctorId { get; set; }


        // ==============================
        // Doctor Name
        // ==============================

        [Required(ErrorMessage = "Doctor name is required")]
        [StringLength(100)]
        public string FullName { get; set; }


        // ==============================
        // Phone Number
        // ==============================

        [Required(ErrorMessage = "Phone number is required")]

        [StringLength(11)]

        [RegularExpression(
            @"^01[0-2,5]{1}[0-9]{8}$",
            ErrorMessage = "Invalid phone number"
        )]

        public string PhoneNumber { get; set; }


        // ==============================
        // Experience
        // ==============================

        public int YearsOfExperience { get; set; }


        // ==============================
        // Relation with Appointment
        // One Doctor -> Many Appointments
        // ==============================

        public ICollection<Appointment>? Appointments { get; set; }


        // ==============================
        // Relation with Specialty
        // ==============================

        public int SpecialtyId { get; set; }

        public Specialty? Specialty { get; set; }


        // ==============================
        // Relation with Clinic
        // ==============================

        public int ClinicId { get; set; }

        public Clinic? Clinic { get; set; }
    }
}