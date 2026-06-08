using System.ComponentModel.DataAnnotations;

namespace Medical_Center_Management_System.Models
{
    public class Specialty
    {
        public int SpecialtyId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Max 100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        // Relation with Specialty
        // One Specialty -> Many Doctors

        public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();


        // Relation with Specialty
        // One Specialty -> Many Clinics

        public ICollection<Clinic> Clinics { get; set; } = new List<Clinic>();
    }
}