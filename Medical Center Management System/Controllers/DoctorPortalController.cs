using Medical_Center_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Medical_Center_Management_System.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorPortalController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DoctorPortalController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================
        // Resolve current user's linked Doctor
        // =========================================

        private async Task<Doctor?> GetCurrentDoctorAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.DoctorId == null) return null;

            return await _context.Doctors
                .Include(d => d.Specialty)
                .Include(d => d.Clinic)
                .Include(d => d.Appointments)
                    .ThenInclude(a => a.Patient)
                .Include(d => d.Appointments)
                    .ThenInclude(a => a.History)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DoctorId == user.DoctorId);
        }

        // =========================================
        // DASHBOARD
        // =========================================

        public async Task<IActionResult> Index()
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null) return RedirectToAction("Login", "Auth");

            var today = DateTime.Today;

            ViewBag.TodayCount = doctor.Appointments?
                .Count(a => a.AppointmentDate.Date == today) ?? 0;

            ViewBag.PendingCount = doctor.Appointments?
                .Count(a => a.Status == "Pending") ?? 0;

            ViewBag.FollowUpCount = doctor.Appointments?
                .Count(a => a.Status == "Needs Follow Up") ?? 0;

            return View(doctor);
        }

        // =========================================
        // MY APPOINTMENTS
        // =========================================

        public async Task<IActionResult> MyAppointments(
        string? status,
        DateTime? date,
        bool todayOnly = false)
        {
            var doctor = await GetCurrentDoctorAsync();

            if (doctor == null)
                return NotFound();

            var appointments = doctor.Appointments?
                .AsQueryable()
                ?? Enumerable.Empty<Appointment>().AsQueryable();

            // Status filter
            if (!string.IsNullOrEmpty(status))
            {
                appointments = appointments
                    .Where(a => a.Status == status);
            }

            // Specific date filter
            if (date.HasValue)
            {
                appointments = appointments
                    .Where(a => a.AppointmentDate.Date == date.Value.Date);
            }

            // Today's appointments only
            if (todayOnly)
            {
                appointments = appointments
                    .Where(a => a.AppointmentDate.Date == DateTime.Today);
            }

            // Order by nearest appointment
            appointments = appointments
                .OrderBy(a => a.AppointmentDate);

            // Statistics
            ViewBag.TodayCount = doctor.Appointments?
                .Count(a => a.AppointmentDate.Date == DateTime.Today) ?? 0;

            ViewBag.PendingCount = doctor.Appointments?
                .Count(a => a.Status == "Pending") ?? 0;

            ViewBag.CompletedCount = doctor.Appointments?
                .Count(a => a.Status == "Completed") ?? 0;

            ViewBag.FollowUpCount = doctor.Appointments?
                .Count(a => a.Status == "Needs Follow Up") ?? 0;

            ViewBag.StatusFilter = status;
            ViewBag.DateFilter = date;
            ViewBag.TodayOnly = todayOnly;

            return View(appointments.ToList());
        }

        // =========================================
        // UPDATE APPOINTMENT STATUS
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int appointmentId, string status)
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null) return NotFound();

            var validStatuses = new[]
            {
                "Pending", "Confirmed", "Completed",
                "Cancelled", "Needs Follow Up"
            };

            if (!validStatuses.Contains(status))
            {
                TempData["Error"] = "Invalid status.";
                return RedirectToAction(nameof(MyAppointments));
            }

            // Only allow updating own appointments
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a =>
                    a.AppointmentId == appointmentId &&
                    a.DoctorId == doctor.DoctorId);

            if (appointment == null) return NotFound();

            appointment.Status = status;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Appointment status updated to \"{status}\".";
            return RedirectToAction(nameof(MyAppointments));
        }

        // =========================================
        // VIEW PATIENT (read full profile)
        // Only accessible if the patient has had
        // at least one appointment with this doctor.
        // =========================================

        public async Task<IActionResult> PatientProfile(int patientId)
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null) return NotFound();

            bool hasRelation = doctor.Appointments?
                .Any(a => a.PatientId == patientId) ?? false;

            if (!hasRelation) return Forbid();

            var patient = await _context.Patients
                .Include(p => p.MedicalRecord)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.History)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Clinic)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PatientId == patientId);

            if (patient == null) return NotFound();

            return View(patient);
        }

        // =========================================
        // WRITE / EDIT MEDICAL RECORD
        // =========================================

        //[HttpGet]
        //public async Task<IActionResult> MedicalRecord(int patientId)
        //{
        //    var doctor = await GetCurrentDoctorAsync();
        //    if (doctor == null) return NotFound();

        //    bool hasRelation = doctor.Appointments?
        //        .Any(a => a.PatientId == patientId) ?? false;
        //    if (!hasRelation) return Forbid();

        //    var patient = await _context.Patients
        //        .Include(p => p.MedicalRecord)
        //        .AsNoTracking()
        //        .FirstOrDefaultAsync(p => p.PatientId == patientId);

        //    if (patient == null) return NotFound();

        //    ViewBag.PatientId = patientId;
        //    ViewBag.PatientName = patient.FullName;

        //    // Return existing record or a blank one
        //    var record = patient.MedicalRecord ?? new MedicalRecord
        //    {
        //        PatientId = patientId
        //    };

        //    return View(record);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> MedicalRecord(MedicalRecord record)
        //{
        //    var doctor = await GetCurrentDoctorAsync();
        //    if (doctor == null) return NotFound();

        //    bool hasRelation = doctor.Appointments?
        //        .Any(a => a.PatientId == record.PatientId) ?? false;
        //    if (!hasRelation) return Forbid();

        //    ModelState.Remove("Patient");

        //    if (record.HasAllergies &&
        //        string.IsNullOrWhiteSpace(record.AllergyDetails))
        //    {
        //        ModelState.AddModelError("AllergyDetails",
        //            "Please enter allergy details.");
        //    }

        //    if (!ModelState.IsValid)
        //    {
        //        var patient = await _context.Patients
        //            .AsNoTracking()
        //            .FirstOrDefaultAsync(p => p.PatientId == record.PatientId);

        //        ViewBag.PatientId = record.PatientId;
        //        ViewBag.PatientName = patient?.FullName;
        //        return View(record);
        //    }

        //    bool exists = await _context.MedicalRecords
        //        .AnyAsync(m => m.MedicalRecordId == record.MedicalRecordId);

        //    if (exists)
        //        _context.MedicalRecords.Update(record);
        //    else
        //        _context.MedicalRecords.Add(record);

        //    await _context.SaveChangesAsync();

        //    TempData["Success"] = "Medical record saved successfully.";
        //    return RedirectToAction(nameof(PatientProfile),
        //        new { patientId = record.PatientId });
        //}

        // =========================================
        // WRITE HISTORY FOR AN APPOINTMENT
        // =========================================

        [HttpGet]
        public async Task<IActionResult> WriteHistory(int appointmentId)
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.History)
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.AppointmentId == appointmentId &&
                    a.DoctorId == doctor.DoctorId);

            if (appointment == null) return NotFound();

            ViewBag.Appointment = appointment;

            var history = appointment.History ?? new History
            {
                AppointmentId = appointmentId
            };

            return View(history);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WriteHistory(History history)
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null) return NotFound();

            // Verify the appointment belongs to this doctor
            bool owns = await _context.Appointments.AnyAsync(a =>
                a.AppointmentId == history.AppointmentId &&
                a.DoctorId == doctor.DoctorId);

            if (!owns) return Forbid();

            ModelState.Remove("Appointment");

            if (!ModelState.IsValid)
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a =>
                        a.AppointmentId == history.AppointmentId);

                ViewBag.Appointment = appointment;
                return View(history);
            }

            bool exists = await _context.Histories
                .AnyAsync(h => h.HistoryId == history.HistoryId);

            if (exists)
                _context.Histories.Update(history);
            else
                _context.Histories.Add(history);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Clinical history saved.";
            return RedirectToAction(nameof(MyAppointments));
        }

        // =========================================
        // MY PROFILE
        // =========================================

        public async Task<IActionResult> Profile()
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null) return NotFound();

            return View(doctor);
        }
    }
}
