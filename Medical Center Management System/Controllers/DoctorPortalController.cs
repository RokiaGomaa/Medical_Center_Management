using Medical_Center_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;

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
            if (doctor == null) return NotFound();

            var appointments = doctor.Appointments?
                .AsQueryable()
                ?? Enumerable.Empty<Appointment>().AsQueryable();

            if (!string.IsNullOrEmpty(status))
                appointments = appointments.Where(a => a.Status == status);

            if (date.HasValue)
                appointments = appointments.Where(a => a.AppointmentDate.Date == date.Value.Date);

            if (todayOnly)
                appointments = appointments.Where(a => a.AppointmentDate.Date == DateTime.Today);

            appointments = appointments.OrderBy(a => a.AppointmentDate);

            ViewBag.TodayCount = doctor.Appointments?.Count(a => a.AppointmentDate.Date == DateTime.Today) ?? 0;
            ViewBag.PendingCount = doctor.Appointments?.Count(a => a.Status == "Pending") ?? 0;
            ViewBag.CompletedCount = doctor.Appointments?.Count(a => a.Status == "Completed") ?? 0;
            ViewBag.FollowUpCount = doctor.Appointments?.Count(a => a.Status == "Needs Follow Up") ?? 0;
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
        public async Task<IActionResult> UpdateStatus(int appointmentId, string status)
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null) return NotFound();

            var validStatuses = new[] { "Pending", "Confirmed", "Completed", "Cancelled", "Needs Follow Up" };
            if (!validStatuses.Contains(status))
            {
                TempData["Error"] = "Invalid status.";
                return RedirectToAction(nameof(MyAppointments));
            }

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
        // VIEW PATIENT PROFILE
        // Only if the patient had at least one
        // appointment with this doctor.
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
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Doctor)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PatientId == patientId);

            if (patient == null) return NotFound();

            return View(patient);
        }

        // =========================================
        // MEDICAL RECORD — GET
        // =========================================

        [HttpGet]
        public async Task<IActionResult> MedicalRecord(int patientId)
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null) return NotFound();

            // Only doctors who treated this patient can edit their record
            bool hasRelation = doctor.Appointments?
                .Any(a => a.PatientId == patientId) ?? false;

            if (!hasRelation) return Forbid();

            var patient = await _context.Patients
                .Include(p => p.MedicalRecord)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PatientId == patientId);

            if (patient == null) return NotFound();

            ViewBag.PatientId = patientId;
            ViewBag.PatientName = patient.FullName;
            ViewBag.PatientPhone = patient.PhoneNumber;
            ViewBag.FromPatient = true;

            var record = patient.MedicalRecord ?? new MedicalRecord
            {
                PatientId = patientId
            };

            return View(record);
        }

        // =========================================
        // MEDICAL RECORD — POST
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MedicalRecord(MedicalRecord record)
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null) return NotFound();

            bool hasRelation = doctor.Appointments?
                .Any(a => a.PatientId == record.PatientId) ?? false;

            if (!hasRelation) return Forbid();

            ModelState.Remove("Patient");

            if (record.HasAllergies && string.IsNullOrWhiteSpace(record.AllergyDetails))
                ModelState.AddModelError("AllergyDetails", "Please enter allergy details.");

            if (!ModelState.IsValid)
            {
                var patient = await _context.Patients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PatientId == record.PatientId);

                ViewBag.PatientId = record.PatientId;
                ViewBag.PatientName = patient?.FullName;
                ViewBag.PatientPhone = patient?.PhoneNumber;
                ViewBag.FromPatient = true;
                return View(record);
            }

            bool exists = await _context.MedicalRecords
                .AnyAsync(m => m.MedicalRecordId == record.MedicalRecordId);

            if (exists)
                _context.MedicalRecords.Update(record);
            else
                _context.MedicalRecords.Add(record);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Medical record saved successfully.";
            return RedirectToAction(nameof(PatientProfile), new { patientId = record.PatientId });
        }

        // =========================================
        // WRITE / EDIT HISTORY — GET
        // =========================================

        [HttpGet]
        public async Task<IActionResult> WriteHistory(int appointmentId)
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Clinic)
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

        // =========================================
        // WRITE / EDIT HISTORY — POST
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WriteHistory(History history)
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null) return NotFound();

            bool owns = await _context.Appointments.AnyAsync(a =>
                a.AppointmentId == history.AppointmentId &&
                a.DoctorId == doctor.DoctorId);

            if (!owns) return Forbid();

            ModelState.Remove("Appointment");

            if (!ModelState.IsValid)
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Clinic)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.AppointmentId == history.AppointmentId);

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
        // PATIENT PDF REPORT
        // =========================================

        public async Task<IActionResult> Report(int patientId)
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null) return NotFound();

            bool hasRelation = doctor.Appointments?
                .Any(a => a.PatientId == patientId) ?? false;

            if (!hasRelation) return Forbid();

            var patient = await _context.Patients
                .Include(p => p.MedicalRecord)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Doctor)
                        .ThenInclude(d => d.Specialty)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Clinic)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.History)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PatientId == patientId);

            if (patient == null) return NotFound();

            return new ViewAsPdf("~/Views/Patients/report.cshtml", patient)
            {
                FileName = $"Patient_Report_{patient.FullName.Replace(" ", "_")}_{DateTime.Today:yyyyMMdd}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageMargins = new Rotativa.AspNetCore.Options.Margins(15, 15, 15, 15),
                CustomSwitches = "--print-media-type --disable-smart-shrinking"
            };
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