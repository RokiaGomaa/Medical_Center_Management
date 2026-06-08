using Medical_Center_Management_System.Models;
using Medical_Center_Management_System.ViewModels;
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
    [Authorize(Roles = "Admin")]
    public class DoctorsController : Controller
    {
        private readonly AppDbContext _context;

        public DoctorsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            int? specialtyId,
            int? clinicId,
            string search,
            string sortBy,
            int pageNumber = 1)
        {
            var doctorsQuery = _context.Doctors
                .Include(d => d.Clinic)
                .Include(d => d.Specialty)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                doctorsQuery = doctorsQuery.Where(d =>
                    EF.Functions.Like(d.FullName.ToLower(), $"%{search.ToLower()}%")
                    ||
                    d.PhoneNumber.Contains(search)
                );
            }

            ViewBag.Search = search;

            doctorsQuery = sortBy switch
            {
                "exp_asc" => doctorsQuery.OrderBy(d => d.YearsOfExperience),
                "exp_desc" => doctorsQuery.OrderByDescending(d => d.YearsOfExperience),
                "spec_asc" => doctorsQuery.OrderBy(d => d.Specialty.Name),
                "spec_desc" => doctorsQuery.OrderByDescending(d => d.Specialty.Name),
                _ => doctorsQuery.OrderBy(d => d.FullName)
            };

            ViewBag.SortBy = sortBy;

            if (specialtyId != null)
            {
                doctorsQuery = doctorsQuery.Where(d => d.SpecialtyId == specialtyId);
            }

            if (clinicId != null)
            {
                doctorsQuery = doctorsQuery.Where(d => d.ClinicId == clinicId);
            }

            ViewData["Specialties"] =
                new SelectList(_context.Specialties, "SpecialtyId", "Name");

            ViewData["Clinics"] =
                new SelectList(_context.Clinics, "ClinicId", "Name");

            int pageSize = 5;

            return View(await PaginatedList<Doctor>.CreateAsync(
                doctorsQuery.AsNoTracking(),
                pageNumber,
                pageSize
            ));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var doctor = await _context.Doctors
                .Include(d => d.Clinic)
                .Include(d => d.Specialty)
                .Include(d => d.Appointments)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DoctorId == id);

            if (doctor == null)
                return NotFound();

            return View(doctor);
        }

        public IActionResult Create()
        {
            ViewData["SpecialtyId"] =
                new SelectList(_context.Specialties, "SpecialtyId", "Name");

            ViewData["ClinicId"] =
                new SelectList(_context.Clinics, "ClinicId", "Name");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetClinicBySpecialty(int specialtyId)
        {
            var clinic = await _context.Clinics
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.SpecialtyId == specialtyId);

            if (clinic == null)
            {
                return Json(new
                {
                    success = false,
                    message = "No clinic found for this specialty"
                });
            }

            return Json(new
            {
                success = true,
                clinicId = clinic.ClinicId,
                clinicName = clinic.Name
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("DoctorId,FullName,PhoneNumber,YearsOfExperience,SpecialtyId,ClinicId")]
            Doctor doctor)
        {
            ModelState.Remove("Clinic");
            ModelState.Remove("Specialty");

            if (await _context.Doctors.AnyAsync(d => d.PhoneNumber == doctor.PhoneNumber))
            {
                ModelState.AddModelError("PhoneNumber", "Phone number already exists");
            }

            var clinic = await _context.Clinics
                .FirstOrDefaultAsync(c => c.SpecialtyId == doctor.SpecialtyId);

            if (clinic == null)
            {
                ModelState.AddModelError("SpecialtyId", "No clinic found for this specialty");
            }
            else
            {
                doctor.ClinicId = clinic.ClinicId;
            }

            if (ModelState.IsValid)
            {
                _context.Add(doctor);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Doctor created successfully";

                return RedirectToAction(nameof(Index));
            }

            ViewData["SpecialtyId"] =
                new SelectList(_context.Specialties, "SpecialtyId", "Name", doctor.SpecialtyId);

            ViewData["ClinicId"] =
                new SelectList(_context.Clinics, "ClinicId", "Name", doctor.ClinicId);

            return View(doctor);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var doctor = await _context.Doctors.FindAsync(id);

            if (doctor == null)
                return NotFound();

            ViewData["SpecialtyId"] =
                new SelectList(_context.Specialties, "SpecialtyId", "Name", doctor.SpecialtyId);

            ViewData["ClinicId"] =
                new SelectList(_context.Clinics, "ClinicId", "Name", doctor.ClinicId);

            return View(doctor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Doctor doctor)
        {
            if (id != doctor.DoctorId)
                return NotFound();

            ModelState.Remove("Clinic");
            ModelState.Remove("Specialty");

            if (await _context.Doctors.AnyAsync(d =>
                d.PhoneNumber == doctor.PhoneNumber &&
                d.DoctorId != doctor.DoctorId))
            {
                ModelState.AddModelError("PhoneNumber", "Phone number already exists");
            }

            var clinic = await _context.Clinics
                .FirstOrDefaultAsync(c => c.SpecialtyId == doctor.SpecialtyId);

            if (clinic == null)
            {
                ModelState.AddModelError("SpecialtyId", "No clinic found for this specialty");
            }
            else
            {
                doctor.ClinicId = clinic.ClinicId;
            }

            if (ModelState.IsValid)
            {
                _context.Update(doctor);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Doctor updated successfully";

                return RedirectToAction(nameof(Index));
            }

            ViewData["SpecialtyId"] =
                new SelectList(_context.Specialties, "SpecialtyId", "Name", doctor.SpecialtyId);

            ViewData["ClinicId"] =
                new SelectList(_context.Clinics, "ClinicId", "Name", doctor.ClinicId);

            return View(doctor);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var doctor = await _context.Doctors
                .Include(d => d.Clinic)
                .Include(d => d.Specialty)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DoctorId == id);

            if (doctor == null)
                return NotFound();

            return View(doctor);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.DoctorId == id);

            if (doctor == null)
                return NotFound();

            var appointments = await _context.Appointments
                .Where(a => a.DoctorId == id)
                .ToListAsync();

            var appointmentIds = appointments
                .Select(a => a.AppointmentId)
                .ToList();

            var histories = await _context.Histories
               .Where(h => h.AppointmentId.HasValue && appointmentIds.Contains(h.AppointmentId.Value))
                .ToListAsync();

            if (histories.Any())
            {
                _context.Histories.RemoveRange(histories);
            }

            if (appointments.Any())
            {
                _context.Appointments.RemoveRange(appointments);
            }

            _context.Doctors.Remove(doctor);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Doctor and related appointments deleted successfully";

            return RedirectToAction(nameof(Index));
        }
    }
}