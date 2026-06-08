using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Medical_Center_Management_System.Models
{
    public class MedicalRecord
    {
        public int MedicalRecordId { get; set; }

       

        [Required]
        [StringLength(3, ErrorMessage = "Blood type cannot exceed 3 characters.")]
        public string BloodType { get; set; }

        public bool HasDiabetes { get; set; }

        public bool HasHypertension { get; set; }

        public bool HasHeartDisease { get; set; }

        public bool HasAllergies { get; set; }

        [StringLength(200, ErrorMessage = "Allergy details cannot exceed 200 characters.")]
        public string? AllergyDetails { get; set; }

        [StringLength(500, ErrorMessage = "Chronic diseases notes cannot exceed 500 characters.")]
        public string? ChronicDiseasesNotes { get; set; }

        [StringLength(300, ErrorMessage = "Current medications cannot exceed 300 characters.")]
        public string? CurrentMedications { get; set; }

        [Range(30, 250, ErrorMessage = "Height must be between 30 and 250 cm.")]
        public double? Height { get; set; }

        [Range(1, 300, ErrorMessage = "Weight must be between 1 and 300 kg.")]
        public double? Weight { get; set; }


        public int PatientId { get; set; }

        public Patient? Patient { get; set; }
    }
}
