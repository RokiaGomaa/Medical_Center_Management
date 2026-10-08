using Medical_Center_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Medical_Center_Management_System.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientPortalController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private const int SlotMinutes = 30;

        public PatientPortalController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================
        // Resolve current user's linked Patient
        // =========================================

        private async Task<Patient?> GetCurrentPatientAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.PatientId == null) return null;

            return await _context.Patients
                .Include(p => p.MedicalRecord)
                .Include(p => p.Appointments!)          // ! suppresses CS8620
                    .ThenInclude(a => a.Doctor!)
                        .ThenInclude(d => d.Specialty)
                .Include(p => p.Appointments!)
                    .ThenInclude(a => a.Clinic)
                .Include(p => p.Appointments!)
                    .ThenInclude(a => a.History)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PatientId == user.PatientId);
        }

        // =========================================
        // DASHBOARD
        // =========================================

        public async Task<IActionResult> Index()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Auth");

            return View(patient);
        }

        // =========================================
        // MY PROFILE
        // =========================================

        public async Task<IActionResult> Profile()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return NotFound();

            return View(patient);
        }

        // =========================================
        // AVAILABLE SLOTS
        // =========================================

        public async Task<IActionResult> AvailableSlots(
            DateTime? date,
            int? clinicId,
            string? search)
                {
            var targetDate = date?.Date ?? DateTime.Today;

            ViewBag.SelectedDate = targetDate;
            ViewBag.SelectedClinic = clinicId;

            var tomorrow = targetDate.AddDays(1);

            // Appointments already booked in selected day
            var bookedToday = await _context.Appointments
                .Where(a =>
                    a.AppointmentDate >= targetDate &&
                    a.AppointmentDate < tomorrow &&
                    a.Status != "Cancelled")
                .ToListAsync();

            // Clinics for filter dropdown
            var clinics = await _context.Clinics
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Clinics = clinics;

            // Doctors query
            var doctorsQuery = _context.Doctors
                .Include(d => d.Clinic)
                .Include(d => d.Specialty)
                .AsQueryable();

            // Filter by clinic
            if (clinicId.HasValue)
            {
                doctorsQuery = doctorsQuery
                    .Where(d => d.ClinicId == clinicId.Value);
            }

            var doctors = await doctorsQuery.ToListAsync();

            // Filter by doctor name search
            if (!string.IsNullOrWhiteSpace(search))
            {
                doctors = doctors
                    .Where(d => d.FullName.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            ViewBag.Search = search;

            var result = new List<DoctorSlotsVM>();

            foreach (var doctor in doctors)
            {
                if (doctor.Clinic == null)
                    continue;

                var slots = new List<DateTime>();

                var cursor = targetDate.Add(doctor.Clinic.StartTime);

                var end = targetDate.Add(doctor.Clinic.EndTime);

                while (cursor.AddMinutes(SlotMinutes) <= end)
                {
                    // Skip past slots when viewing today
                    if (targetDate.Date == DateTime.Today &&
                        cursor < DateTime.Now)
                    {
                        cursor = cursor.AddMinutes(SlotMinutes);
                        continue;
                    }

                    bool taken = bookedToday.Any(a =>
                        a.DoctorId == doctor.DoctorId &&
                        cursor < a.AppointmentDate.AddMinutes(SlotMinutes) &&
                        cursor.AddMinutes(SlotMinutes) > a.AppointmentDate);

                    if (!taken)
                    {
                        slots.Add(cursor);
                    }

                    cursor = cursor.AddMinutes(SlotMinutes);
                }

                if (slots.Any())
                {
                    result.Add(new DoctorSlotsVM
                    {
                        Doctor = doctor,
                        AvailableSlots = slots
                    });
                }
            }

            return View(result);
        }

        // =========================================
        // BOOK APPOINTMENT — GET
        // =========================================

        [HttpGet]
        public async Task<IActionResult> BookAppointment(
            int doctorId, int clinicId, DateTime dateTime)
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return NotFound();

            var doctor = await _context.Doctors
                .Include(d => d.Clinic)
                .Include(d => d.Specialty)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DoctorId == doctorId);

            if (doctor == null) return NotFound();

            ViewBag.Patient = patient;
            ViewBag.Doctor = doctor;
            ViewBag.DateTime = dateTime;
            ViewBag.ClinicId = clinicId;

            return View();
        }

        // =========================================
        // BOOK APPOINTMENT — POST
        // FIX CS0111: POST receives a BookAppointmentVM
        // so the two overloads have different signatures.
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("BookAppointment")]
        public async Task<IActionResult> BookAppointmentPost(BookAppointmentVM vm)
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return NotFound();

            var doctor = await _context.Doctors
                .Include(d => d.Clinic)
                .FirstOrDefaultAsync(d => d.DoctorId == vm.DoctorId);

            if (doctor?.Clinic == null)
            {
                TempData["Error"] = "Doctor or clinic not found.";
                return RedirectToAction(nameof(AvailableSlots));
            }

            var newEnd = vm.AppointmentDate.AddMinutes(SlotMinutes);

            // Conflict check
            bool conflict = await _context.Appointments.AnyAsync(a =>
                (a.DoctorId == vm.DoctorId ||
                 a.PatientId == patient.PatientId ||
                 a.ClinicId == vm.ClinicId) &&
                vm.AppointmentDate < a.AppointmentDate.AddMinutes(SlotMinutes) &&
                newEnd > a.AppointmentDate &&
                a.Status != "Cancelled");

            if (conflict)
            {
                TempData["Error"] =
                    "This slot is no longer available. Please choose another.";
                return RedirectToAction(nameof(AvailableSlots));
            }

            // Capacity check
            int todayCount = await _context.Appointments.CountAsync(a =>
                a.ClinicId == vm.ClinicId &&
                a.AppointmentDate.Date == vm.AppointmentDate.Date &&
                a.Status != "Cancelled");

            if (todayCount >= doctor.Clinic.MaxPatients)
            {
                TempData["Error"] = "This clinic is fully booked for the day.";
                return RedirectToAction(nameof(AvailableSlots));
            }

            _context.Appointments.Add(new Appointment
            {
                AppointmentDate = vm.AppointmentDate,
                DoctorId = vm.DoctorId,
                PatientId = patient.PatientId,
                ClinicId = vm.ClinicId,
                Status = "Pending"
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Appointment booked successfully!";
            return RedirectToAction(nameof(MyAppointments));
        }

        // =========================================
        // MY APPOINTMENTS
        // =========================================

        public async Task<IActionResult> MyAppointments()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return NotFound();

            return View(patient.Appointments?
                .OrderByDescending(a => a.AppointmentDate)
                .ToList() ?? new List<Appointment>());
        }
    }

    // =========================================
    // POST form model
    // =========================================

    public class BookAppointmentVM
    {
        public int DoctorId { get; set; }
        public int ClinicId { get; set; }
        public DateTime AppointmentDate { get; set; }
    }

    // =========================================
    // Slot display model
    // =========================================

    public class DoctorSlotsVM
    {
        public Doctor Doctor { get; set; } = null!;
        public List<DateTime> AvailableSlots { get; set; } = new();
    }
}