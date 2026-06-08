using Medical_Center_Management_System.Models;

public class DashboardViewModel
{
    public int TotalPatients { get; set; }
    public int TotalDoctors { get; set; }
    public int TotalAppointments { get; set; }
    public int TotalClinics { get; set; }
    public int TodayAppointments { get; set; }

    public List<Appointment> RecentAppointments { get; set; }
}