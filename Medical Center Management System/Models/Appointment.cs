using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Medical_Center_Management_System.Models
{
    public class Appointment
    {
        public int AppointmentId { get; set; }

        [Required(ErrorMessage = "Date is required")]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [StringLength(50)]
        public string Status { get; set; } 


        //Relation With Doctors

        public int DoctorId { get; set; }
        public virtual Doctor Doctor { get; set; }


        //Relation With Patient
        public int PatientId { get; set; }
        public virtual Patient Patient { get; set; }


        //Relation With Clinic
        public int ClinicId { get; set; }
        public virtual Clinic Clinic { get; set; }

        //Relation with history
        public virtual History? History { get; set; }
    }
}
