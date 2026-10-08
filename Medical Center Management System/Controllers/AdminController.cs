using Medical_Center_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Medical_Center_Management_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var today = DateTime.Today;

            var model = new DashboardViewModel
            {
                TotalPatients = _context.Patients.Count(),
                TotalDoctors = _context.Doctors.Count(),
                TotalAppointments = _context.Appointments.Count(),
                TotalClinics = _context.Clinics.Count(),

                TodayAppointments = _context.Appointments
                    .Where(a => a.AppointmentDate.Date == today)
                    .Count(),

                RecentAppointments = _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .Include(a => a.Clinic)
                    .OrderByDescending(a => a.AppointmentDate)
                    .Take(6)
                    .ToList()
            };

            return View(model);
        }
    }
}
