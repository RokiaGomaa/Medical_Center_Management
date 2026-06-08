using Medical_Center_Management_System.Models;
using Medical_Center_Management_System.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Medical_Center_Management_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SpecialtiesController : Controller
    {
        private readonly AppDbContext _context;

        public SpecialtiesController(AppDbContext context)
        {
            _context = context;
        }

        // =========================================
        // INDEX
        // =========================================

        public async Task<IActionResult> Index(string? search, int pageNumber = 1)
        {
            var specialties = _context.Specialties
                .Include(s => s.Doctors)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                specialties = specialties
                    .Where(s => s.Name.Contains(search));
            }

            int pageSize = 5;

            return View(await PaginatedList<Specialty>.CreateAsync(
                specialties.AsNoTracking(),
                pageNumber,
                pageSize
            ));
        }

        // =========================================
        // DETAILS
        // =========================================

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var specialty = await _context.Specialties
                .Include(s => s.Doctors)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.SpecialtyId == id);

            if (specialty == null)
                return NotFound();

            return View(specialty);
        }

        // =========================================
        // CREATE GET
        // =========================================

        public IActionResult Create()
        {
            return View();
        }

        // =========================================
        // CREATE POST
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("SpecialtyId,Name,Description")]
            Specialty specialty)
        {
            if (await _context.Specialties
                .AnyAsync(s =>
                    s.Name.ToLower() ==
                    specialty.Name.ToLower()))
            {
                ModelState.AddModelError(
                    "Name",
                    "Specialty already exists");
            }

            if (ModelState.IsValid)
            {
                _context.Specialties.Add(specialty);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Specialty saved successfully";

                return RedirectToAction(nameof(Index));
            }

            return View(specialty);
        }

        // =========================================
        // EDIT GET
        // =========================================

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var specialty = await _context.Specialties
                .FindAsync(id);

            if (specialty == null)
                return NotFound();

            return View(specialty);
        }

        // =========================================
        // EDIT POST
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("SpecialtyId,Name,Description")]
            Specialty specialty)
        {
            if (id != specialty.SpecialtyId)
                return NotFound();

            if (await _context.Specialties.AnyAsync(s =>
                s.Name == specialty.Name &&
                s.SpecialtyId != specialty.SpecialtyId))
            {
                ModelState.AddModelError(
                    "Name",
                    "Specialty already exists");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(specialty);

                    await _context.SaveChangesAsync();

                    TempData["Success"] =
                        "Specialty updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SpecialtyExists(specialty.SpecialtyId))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(specialty);
        }

        // =========================================
        // DELETE GET
        // =========================================

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var specialty = await _context.Specialties
                .FirstOrDefaultAsync(m => m.SpecialtyId == id);

            if (specialty == null)
                return NotFound();

            return View(specialty);
        }

        // =========================================
        // DELETE POST
        // =========================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var specialty = await _context.Specialties
                .Include(s => s.Doctors)
                .Include(s => s.Clinics)
                .FirstOrDefaultAsync(s => s.SpecialtyId == id);

            if (specialty == null)
                return NotFound();

            // =====================================
            // Get clinics
            // =====================================

            var clinics = await _context.Clinics
                .Where(c => c.SpecialtyId == id)
                .ToListAsync();

            // =====================================
            // Get doctors
            // =====================================

            var doctors = await _context.Doctors
                .Where(d => d.SpecialtyId == id)
                .ToListAsync();

            // =====================================
            // Get appointments
            // =====================================

            var appointments = await _context.Appointments
                .Where(a =>
                    doctors.Select(d => d.DoctorId)
                    .Contains(a.DoctorId))
                .ToListAsync();

            // =====================================
            // Get appointment ids
            // =====================================

            var appointmentIds = appointments
                .Select(a => a.AppointmentId)
                .ToList();

            // =====================================
            // Get histories
            // =====================================

            var histories = await _context.Histories
                .Where(h =>
                    h.AppointmentId.HasValue &&
                    appointmentIds.Contains(h.AppointmentId.Value))
                .ToListAsync();

            // =====================================
            // Delete histories
            // =====================================

            if (histories.Any())
            {
                _context.Histories.RemoveRange(histories);
            }

            // =====================================
            // Delete appointments
            // =====================================

            if (appointments.Any())
            {
                _context.Appointments.RemoveRange(appointments);
            }

            // =====================================
            // Delete doctors
            // =====================================

            if (doctors.Any())
            {
                _context.Doctors.RemoveRange(doctors);
            }

            // =====================================
            // Delete clinics
            // =====================================

            if (clinics.Any())
            {
                _context.Clinics.RemoveRange(clinics);
            }

            // =====================================
            // Delete specialty
            // =====================================

            _context.Specialties.Remove(specialty);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Specialty and all related data deleted successfully";

            return RedirectToAction(nameof(Index));
        }

        // =========================================
        // EXISTS
        // =========================================

        private bool SpecialtyExists(int id)
        {
            return _context.Specialties
                .Any(e => e.SpecialtyId == id);
        }
    }
}