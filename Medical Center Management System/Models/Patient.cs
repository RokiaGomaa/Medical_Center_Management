using System.ComponentModel.DataAnnotations;

namespace Medical_Center_Management_System.Models
{
    public class Patient
    {
        public int PatientId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [StringLength(11)]
        [RegularExpression(@"^01[0-2,5]{1}[0-9]{8}$", ErrorMessage = "Invalid phone number")]
        public string PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string Gender { get; set; }



        // Relation with Patient One Patient -> Many Appointments
        public ICollection<Appointment>? Appointments { get; set; }

        //Relation with medical record
        public MedicalRecord? MedicalRecord { get; set; }

    }
}
