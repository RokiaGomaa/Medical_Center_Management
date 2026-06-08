using Medical_Center_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        // Index
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

        // Details
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

        // Create (GET) — receives patientId from query string when coming from Patient Details
        public IActionResult Create(int? patientId)
        {
            // If patientId is provided, pre-select it and pass patient info to view
            if (patientId.HasValue)
            {
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
                ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "FullName");
                ViewBag.FromPatient = false;
            }

            return View();
        }

        // Create (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("MedicalRecordId,BloodType,HasDiabetes,HasHypertension,HasHeartDisease,HasAllergies,AllergyDetails,ChronicDiseasesNotes,CurrentMedications,Height,Weight,PatientId")]
            MedicalRecord medicalRecord)
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

                // Always go back to the patient's profile
                return RedirectToAction("Details", "Patients", new { id = medicalRecord.PatientId });
            }

            // Repopulate on error
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
                ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "FullName", medicalRecord.PatientId);
                ViewBag.FromPatient = false;
            }

            return View(medicalRecord);
        }

        // Edit (GET)
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

        // Edit (POST)
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

                // Return to patient profile
                return RedirectToAction("Details", "Patients", new { id = medicalRecord.PatientId });
            }

            var patient = _context.Patients.Find(medicalRecord.PatientId);
            ViewBag.PatientId = medicalRecord.PatientId;
            ViewBag.PatientName = patient?.FullName;
            ViewBag.PatientPhone = patient?.PhoneNumber;

            return View(medicalRecord);
        }

        // Delete (GET)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var medicalRecord = await _context.MedicalRecords
                .Include(m => m.Patient)
                .FirstOrDefaultAsync(m => m.MedicalRecordId == id);

            if (medicalRecord == null) return NotFound();

            return View(medicalRecord);
        }

        // Delete (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var medicalRecord = await _context.MedicalRecords.FindAsync(id);
            int? patientId = medicalRecord?.PatientId;

            if (medicalRecord != null)
                _context.MedicalRecords.Remove(medicalRecord);

            await _context.SaveChangesAsync();
            TempData["Success"] = "Medical record deleted successfully";

            // Return to patient profile if we know the patient
            if (patientId.HasValue)
                return RedirectToAction("Details", "Patients", new { id = patientId.Value });

            return RedirectToAction(nameof(Index));
        }

        private bool MedicalRecordExists(int id) =>
            _context.MedicalRecords.Any(e => e.MedicalRecordId == id);
    }
}