using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Medical_Center_Management_System.Models
{
    public class History
    {
        public int HistoryId { get; set; }

        

        public bool IsFirstVisit { get; set; }

        [StringLength(300, ErrorMessage = "Diagnosis cannot exceed 300 characters.")]
        public string? Diagnosis { get; set; }

        [StringLength(300, ErrorMessage = "Treatment cannot exceed 300 characters.")]
        public string? Treatment { get; set; }

        public bool DidTreatmentWork { get; set; }

        [StringLength(1000, ErrorMessage = "Doctor notes cannot exceed 1000 characters.")]
        public string? DoctorNotes { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FollowUpDate { get; set; }

        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters.")]
        public string? Status { get; set; }

        
        public int? AppointmentId { get; set; }
        public virtual Appointment? Appointment { get; set; }













    }
}
