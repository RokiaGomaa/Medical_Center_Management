using Medical_Center_Management_System.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Medical_Center_Management_System.ViewModels
{
    public class ClincsVM
    {
        public int ClinicId { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        [Required]
        public int MaxPatients { get; set; }
        [Required]
        public TimeSpan StartTime { get; set; }
        [Required]
        public TimeSpan EndTime { get; set; }
        public int SpecialtyId { get; set; }

        public Specialty? Specialty { get; set; }
    }
}
