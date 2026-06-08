using Medical_Center_Management_System.Models;
using Medical_Center_Management_System.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Medical_Center_Management_System.Controllers
{
    [Authorize(Roles = "Admin,Doctor")]
    public class HistoriesController : Controller
    {
        private readonly AppDbContext _context;

        public HistoriesController(AppDbContext context)
        {
            _context = context;
        }

        // Index
        public async Task<IActionResult> Index(string search, int pageNumber = 1)
        {
            var histories = _context.Histories
                .Include(h => h.Appointment)
                    .ThenInclude(a => a.Patient)
                .Include(h => h.Appointment)
                    .ThenInclude(a => a.Doctor)
                .Include(h => h.Appointment)
                    .ThenInclude(a => a.Clinic)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                histories = histories.Where(h =>
                    (h.Diagnosis != null && h.Diagnosis.Contains(search)) ||
                    (h.Treatment != null && h.Treatment.Contains(search)) ||
                    (h.Status != null && h.Status.Contains(search))||
                    h.Appointment.Patient.FullName.Contains(search) ||
                    h.Appointment.Doctor.FullName.Contains(search) ||
                    h.Appointment.Clinic.Name.Contains(search));
            }

            int pageSize = 5;

            return View(await PaginatedList<History>.CreateAsync(
                histories,
                pageNumber,
                pageSize
            ));
        }

        // Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var history = await _context.Histories
                .Include(h => h.Appointment)
                    .ThenInclude(a => a.Patient)
                .Include(h => h.Appointment)
                    .ThenInclude(a => a.Doctor)
                        .ThenInclude(d => d.Specialty)
                .Include(h => h.Appointment)
                    .ThenInclude(a => a.Clinic)
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.HistoryId == id);

            if (history == null) return NotFound();

            return View(history);
        }

        // Create (GET) — receives patientId from query string when coming from Patient Details
        public IActionResult Create(int? patientId)
        {
            if (patientId.HasValue)
            {
                // Load only appointments for this patient that don't have a history yet
                LoadAppointmentsDropDown(patientId: patientId.Value);

                var patient = _context.Patients.Find(patientId.Value);
                if (patient != null)
                {
                    ViewBag.PatientId = patientId.Value;
                    ViewBag.PatientName = patient.FullName;
                    ViewBag.PatientPhone = patient.PhoneNumber;
                    ViewBag.FromPatient = true;
                }
            }
            else
            {
                LoadAppointmentsDropDown();
                ViewBag.FromPatient = false;
            }

            return View();
        }

        // Create (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("HistoryId,IsFirstVisit,Diagnosis,Treatment,DidTreatmentWork,DoctorNotes,FollowUpDate,Status,AppointmentId")]
            History history)
        {
            ModelState.Remove("Appointment");

            bool appointmentAlreadyHasHistory = await _context.Histories
                .AnyAsync(h => h.AppointmentId == history.AppointmentId);

            if (appointmentAlreadyHasHistory)
                ModelState.AddModelError("AppointmentId", "This appointment already has a history.");

            if (ModelState.IsValid)
            {
                _context.Histories.Add(history);
                await _context.SaveChangesAsync();
                TempData["Success"] = "History created successfully";

                // Go back to the patient's profile
                var appointment = await _context.Appointments.FindAsync(history.AppointmentId);
                if (appointment != null)
                    return RedirectToAction("Details", "Patients", new { id = appointment.PatientId });

                return RedirectToAction(nameof(Index));
            }

            // Repopulate on error
            var patientIdFromAppt = history.AppointmentId.HasValue
                ? _context.Appointments.Find(history.AppointmentId.Value)?.PatientId
                : null;

            if (patientIdFromAppt.HasValue)
            {
                LoadAppointmentsDropDown(history.AppointmentId, patientIdFromAppt.Value);
                var patient = _context.Patients.Find(patientIdFromAppt.Value);
                ViewBag.PatientId = patientIdFromAppt.Value;
                ViewBag.PatientName = patient?.FullName;
                ViewBag.PatientPhone = patient?.PhoneNumber;
                ViewBag.FromPatient = true;
            }
            else
            {
                LoadAppointmentsDropDown(history.AppointmentId);
                ViewBag.FromPatient = false;
            }

            return View(history);
        }

        // Edit (GET)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var history = await _context.Histories
                .Include(h => h.Appointment)
                    .ThenInclude(a => a.Patient)
                .FirstOrDefaultAsync(h => h.HistoryId == id);

            if (history == null) return NotFound();

            LoadAppointmentsDropDown(history.AppointmentId);

            ViewBag.PatientId = history.Appointment?.PatientId;
            ViewBag.PatientName = history.Appointment?.Patient?.FullName;
            ViewBag.PatientPhone = history.Appointment?.Patient?.PhoneNumber;

            return View(history);
        }

        // Edit (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("HistoryId,IsFirstVisit,Diagnosis,Treatment,DidTreatmentWork,DoctorNotes,FollowUpDate,Status,AppointmentId")]
            History history)
        {
            if (id != history.HistoryId) return NotFound();

            ModelState.Remove("Appointment");

            bool appointmentAlreadyHasHistory = await _context.Histories
                .AnyAsync(h => h.AppointmentId == history.AppointmentId && h.HistoryId != history.HistoryId);

            if (appointmentAlreadyHasHistory)
                ModelState.AddModelError("AppointmentId", "This appointment already has a history.");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Histories.Update(history);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "History updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HistoryExists(history.HistoryId)) return NotFound();
                    else throw;
                }

                // Go back to the patient's profile
                var appointment = await _context.Appointments.FindAsync(history.AppointmentId);
                if (appointment != null)
                    return RedirectToAction("Details", "Patients", new { id = appointment.PatientId });

                return RedirectToAction(nameof(Index));
            }

            LoadAppointmentsDropDown(history.AppointmentId);

            var appt = history.AppointmentId.HasValue
                ? await _context.Appointments.Include(a => a.Patient).FirstOrDefaultAsync(a => a.AppointmentId == history.AppointmentId)
                : null;

            ViewBag.PatientId = appt?.PatientId;
            ViewBag.PatientName = appt?.Patient?.FullName;
            ViewBag.PatientPhone = appt?.Patient?.PhoneNumber;

            return View(history);
        }

        // Delete (GET)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var history = await _context.Histories
                .Include(h => h.Appointment)
                    .ThenInclude(a => a.Patient)
                .Include(h => h.Appointment)
                    .ThenInclude(a => a.Doctor)
                .Include(h => h.Appointment)
                    .ThenInclude(a => a.Clinic)
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.HistoryId == id);

            if (history == null) return NotFound();

            return View(history);
        }

        // Delete (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var history = await _context.Histories
                .Include(h => h.Appointment)
                .FirstOrDefaultAsync(h => h.HistoryId == id);

            int? patientId = history?.Appointment?.PatientId;

            if (history != null)
            {
                _context.Histories.Remove(history);
                await _context.SaveChangesAsync();
                TempData["Success"] = "History deleted successfully";
            }

            // Go back to patient profile
            if (patientId.HasValue)
                return RedirectToAction("Details", "Patients", new { id = patientId.Value });

            return RedirectToAction(nameof(Index));
        }

        // Helpers
        private void LoadAppointmentsDropDown(int? selectedAppointmentId = null, int? patientId = null)
        {
            // Get appointments that don't have a history yet (or the currently selected one)
            var appointmentsQuery = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Clinic)
                .AsNoTracking()
                .Where(a => !_context.Histories.Any(h =>
                    h.AppointmentId == a.AppointmentId &&
                    h.AppointmentId != selectedAppointmentId));

            // Filter by patient if coming from patient profile
            if (patientId.HasValue)
                appointmentsQuery = appointmentsQuery.Where(a => a.PatientId == patientId.Value);

            var appointments = appointmentsQuery
                .Select(a => new
                {
                    a.AppointmentId,
                    DisplayText =
                        a.Patient.FullName + " — " +
                        a.Doctor.FullName + " — " +
                        a.Clinic.Name + " — " +
                        a.AppointmentDate.ToString("yyyy-MM-dd HH:mm")
                })
                .ToList();

            ViewData["AppointmentId"] = new SelectList(appointments, "AppointmentId", "DisplayText", selectedAppointmentId);
        }

        private bool HistoryExists(int id) =>
            _context.Histories.Any(e => e.HistoryId == id);
    }
}