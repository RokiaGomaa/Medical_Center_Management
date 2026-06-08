using Medical_Center_Management_System.Models;
using Medical_Center_Management_System.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Medical_Center_Management_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PatientsController : Controller
    {
        private readonly AppDbContext _context;

        public PatientsController(AppDbContext context)
        {
            _context = context;
        }

        // Index
        public async Task<IActionResult> Index(string searchPhone, int pageNumber = 1)
        {
            var patients = _context.Patients
                .AsNoTracking()
                .Include(p => p.MedicalRecord)
                .Include(p => p.Appointments)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchPhone))
            {
                patients = patients.Where(p =>
                    p.PhoneNumber.Contains(searchPhone));
            }

            int pageSize = 5;

            return View(await PaginatedList<Patient>.CreateAsync(
                patients,
                pageNumber,
                pageSize
            ));
        }

        // Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var patient = await _context.Patients
                .AsNoTracking()
                .Include(p => p.MedicalRecord)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Doctor)
                        .ThenInclude(d => d.Specialty)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Clinic)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.History)
                .FirstOrDefaultAsync(p => p.PatientId == id);

            if (patient == null)
                return NotFound();

            return View(patient);
        }

        // PDF Report
        public async Task<IActionResult> Report(int? id)
        {
            if (id == null)
                return NotFound();

            var patient = await _context.Patients
                .AsNoTracking()
                .Include(p => p.MedicalRecord)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Doctor)
                        .ThenInclude(d => d.Specialty)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Clinic)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.History)
                .FirstOrDefaultAsync(p => p.PatientId == id);

            if (patient == null)
                return NotFound();

            return new ViewAsPdf("Report", patient)
            {
                FileName =
                    $"Patient_Report_{patient.FullName.Replace(" ", "_")}_{DateTime.Today:yyyyMMdd}.pdf",

                PageSize = Rotativa.AspNetCore.Options.Size.A4,

                PageMargins =
                    new Rotativa.AspNetCore.Options.Margins(15, 15, 15, 15),

                CustomSwitches =
                    "--print-media-type --disable-smart-shrinking"
            };
        }

        // Create GET
        public IActionResult Create()
        {
            LoadGenderList();

            return View();
        }

        // Create POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("PatientId,FullName,PhoneNumber,DateOfBirth,Gender")]
            Patient patient)
        {
            if (await _context.Patients.AnyAsync(p =>
                p.PhoneNumber == patient.PhoneNumber))
            {
                ModelState.AddModelError(
                    "PhoneNumber",
                    "Phone number already exists");
            }

            if (ModelState.IsValid)
            {
                _context.Add(patient);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Patient created successfully. You can now add a Medical Record.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = patient.PatientId });
            }

            LoadGenderList();

            return View(patient);
        }

        // Edit GET
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var patient = await _context.Patients.FindAsync(id);

            if (patient == null)
                return NotFound();

            LoadGenderList(patient.Gender);

            return View(patient);
        }

        // Edit POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("PatientId,FullName,PhoneNumber,DateOfBirth,Gender")]
            Patient patient)
        {
            if (id != patient.PatientId)
                return NotFound();

            if (await _context.Patients.AnyAsync(p =>
                p.PhoneNumber == patient.PhoneNumber &&
                p.PatientId != patient.PatientId))
            {
                ModelState.AddModelError(
                    "PhoneNumber",
                    "Phone number already exists");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(patient);

                    await _context.SaveChangesAsync();

                    TempData["Success"] =
                        "Patient updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(patient.PatientId))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction(
                    nameof(Details),
                    new { id = patient.PatientId });
            }

            LoadGenderList(patient.Gender);

            return View(patient);
        }

        // Delete GET
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var patient = await _context.Patients
                .Include(p => p.MedicalRecord)
                .Include(p => p.Appointments)
                .FirstOrDefaultAsync(p => p.PatientId == id);

            if (patient == null)
                return NotFound();

            return View(patient);
        }

        // Delete POST
        [HttpPost, ActionName("Delete")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteConfirmed(int id)
{
    var patient = await _context.Patients
        .Include(p => p.Appointments)
        .Include(p => p.MedicalRecord)
        .FirstOrDefaultAsync(p => p.PatientId == id);

    if (patient == null)
        return NotFound();

    // =====================================
    // SERVER-SIDE GUARD — cannot be bypassed
    // unlike a disabled HTML button
    // =====================================
    if (patient.Appointments != null && patient.Appointments.Any())
    {
        TempData["Error"] = 
            "Cannot delete this patient because they have linked appointments. " +
            "Please remove all appointments first.";

        return RedirectToAction(nameof(Delete), new { id });
    }

    // Delete medical record if exists
    if (patient.MedicalRecord != null)
    {
        _context.MedicalRecords.Remove(patient.MedicalRecord);
    }

    // Delete patient
    _context.Patients.Remove(patient);

    await _context.SaveChangesAsync();

    TempData["Success"] = "Patient deleted successfully";

    return RedirectToAction(nameof(Index));
}

        private void LoadGenderList(string? selected = null)
        {
            ViewData["GenderList"] =
                new List<SelectListItem>
                {
                    new SelectListItem
                    {
                        Value = "Male",
                        Text = "Male",
                        Selected = selected == "Male"
                    },

                    new SelectListItem
                    {
                        Value = "Female",
                        Text = "Female",
                        Selected = selected == "Female"
                    }
                };
        }

        private bool PatientExists(int id) =>
            _context.Patients.Any(e => e.PatientId == id);
    }
}