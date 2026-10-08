using Medical_Center_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Medical_Center_Management_System.Controllers
{
    [Authorize(Roles = "Admin,Doctor")]
    public class MedicalRecordsController : Controller
    {
        private readonly AppDbContext _context;

        public MedicalRecordsController(AppDbContext context)
        {
            _context = context;
        }

        // =========================================
        // Helper — redirect back to patient profile
        // based on the current user's role
        // =========================================

        private IActionResult RedirectToPatient(int patientId)
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction("Details", "Patients", new { id = patientId });
            else
                return RedirectToAction("PatientProfile", "DoctorPortal", new { patientId });
        }

        // =========================================
        // INDEX
        // =========================================

        public async Task<IActionResult> Index(string? search)
        {
            var medicalRecords = _context.MedicalRecords
                .Include(m => m.Patient)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                medicalRecords = medicalRecords.Where(m =>
                    m.Patient != null &&
                    (EF.Functions.Like(m.Patient.FullName.ToLower(), $"%{search.ToLower()}%") ||
                     EF.Functions.Like(m.Patient.PhoneNumber, $"%{search}%")));
            }

            ViewData["CurrentSearch"] = search;
            return View(await medicalRecords.ToListAsync());
        }

        // =========================================
        // DETAILS
        // =========================================

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var medicalRecord = await _context.MedicalRecords
                .AsNoTracking()
                .Include(m => m.Patient)
                .FirstOrDefaultAsync(m => m.MedicalRecordId == id);

            if (medicalRecord == null) return NotFound();

            return View(medicalRecord);
        }

        // =========================================
        // CREATE — GET
        // =========================================

        public IActionResult Create(int? patientId, string? returnTo)
        {
            // Full patient list for the standalone (no patientId) case
            var patients = _context.Patients.ToList();
            ViewData["PatientList"] = new SelectList(patients, "PatientId", "FullName", patientId);

            ViewBag.ReturnTo = returnTo;

            if (patientId.HasValue)
            {
                var patient = _context.Patients.FirstOrDefault(p => p.PatientId == patientId.Value);
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
                ViewBag.FromPatient = false;
            }

            return View();
        }

        // =========================================
        // CREATE — POST
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("MedicalRecordId,BloodType,HasDiabetes,HasHypertension,HasHeartDisease,HasAllergies,AllergyDetails,ChronicDiseasesNotes,CurrentMedications,Height,Weight,PatientId")]
            MedicalRecord medicalRecord,
            string? returnTo)
        {
            if (await _context.MedicalRecords.AnyAsync(m => m.PatientId == medicalRecord.PatientId))
                ModelState.AddModelError("PatientId", "This patient already has a medical record.");

            if (medicalRecord.HasAllergies && string.IsNullOrWhiteSpace(medicalRecord.AllergyDetails))
                ModelState.AddModelError("AllergyDetails", "Please enter allergy details.");

            if (ModelState.IsValid)
            {
                _context.Add(medicalRecord);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Medical record created successfully";

                // returnTo overrides role check — used when a specific flow needs a fixed destination
                if (!string.IsNullOrEmpty(returnTo) && returnTo == "DoctorPortal")
                    return RedirectToAction("PatientProfile", "DoctorPortal",
                        new { patientId = medicalRecord.PatientId });

                return RedirectToPatient(medicalRecord.PatientId);  // ✅ role-aware
            }

            // Repopulate on validation error
            var patient = _context.Patients.Find(medicalRecord.PatientId);
            if (patient != null)
            {
                ViewBag.PatientId = medicalRecord.PatientId;
                ViewBag.PatientName = patient.FullName;
                ViewBag.PatientPhone = patient.PhoneNumber;
                ViewBag.FromPatient = true;
            }
            else
            {
                ViewData["PatientList"] = new SelectList(
                    _context.Patients, "PatientId", "FullName", medicalRecord.PatientId);
                ViewBag.FromPatient = false;
            }

            ViewBag.ReturnTo = returnTo;
            return View(medicalRecord);
        }

        // =========================================
        // EDIT — GET
        // =========================================

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var medicalRecord = await _context.MedicalRecords
                .Include(m => m.Patient)
                .FirstOrDefaultAsync(m => m.MedicalRecordId == id);

            if (medicalRecord == null) return NotFound();

            ViewBag.PatientId = medicalRecord.PatientId;
            ViewBag.PatientName = medicalRecord.Patient?.FullName;
            ViewBag.PatientPhone = medicalRecord.Patient?.PhoneNumber;

            return View(medicalRecord);
        }

        // =========================================
        // EDIT — POST
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("MedicalRecordId,BloodType,HasDiabetes,HasHypertension,HasHeartDisease,HasAllergies,AllergyDetails,ChronicDiseasesNotes,CurrentMedications,Height,Weight,PatientId")]
            MedicalRecord medicalRecord)
        {
            if (id != medicalRecord.MedicalRecordId) return NotFound();

            if (await _context.MedicalRecords.AnyAsync(m =>
                m.PatientId == medicalRecord.PatientId &&
                m.MedicalRecordId != medicalRecord.MedicalRecordId))
                ModelState.AddModelError("PatientId", "This patient already has a medical record.");

            if (medicalRecord.HasAllergies && string.IsNullOrWhiteSpace(medicalRecord.AllergyDetails))
                ModelState.AddModelError("AllergyDetails", "Please enter allergy details.");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(medicalRecord);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Medical record updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MedicalRecordExists(medicalRecord.MedicalRecordId)) return NotFound();
                    else throw;
                }

                return RedirectToPatient(medicalRecord.PatientId);  // ✅ role-aware
            }

            var patient = _context.Patients.Find(medicalRecord.PatientId);
            ViewBag.PatientId = medicalRecord.PatientId;
            ViewBag.PatientName = patient?.FullName;
            ViewBag.PatientPhone = patient?.PhoneNumber;

            return View(medicalRecord);
        }

        // =========================================
        // DELETE — GET
        // =========================================

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var medicalRecord = await _context.MedicalRecords
                .Include(m => m.Patient)
                .FirstOrDefaultAsync(m => m.MedicalRecordId == id);

            if (medicalRecord == null) return NotFound();

            return View(medicalRecord);
        }

        // =========================================
        // DELETE — POST
        // =========================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var medicalRecord = await _context.MedicalRecords.FindAsync(id);
            int patientId = medicalRecord?.PatientId ?? 0;

            if (medicalRecord != null)
                _context.MedicalRecords.Remove(medicalRecord);

            await _context.SaveChangesAsync();
            TempData["Success"] = "Medical record deleted successfully";

            if (patientId != 0)
                return RedirectToPatient(patientId);  // ✅ role-aware

            return RedirectToAction(nameof(Index));
        }

        // =========================================
        // HELPERS
        // =========================================

        private bool MedicalRecordExists(int id) =>
            _context.MedicalRecords.Any(e => e.MedicalRecordId == id);
    }
}