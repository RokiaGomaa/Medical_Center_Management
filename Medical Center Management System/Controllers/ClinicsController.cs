using Medical_Center_Management_System;
using Medical_Center_Management_System.Models;
using Medical_Center_Management_System.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Medical_Center_Management_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ClinicsController : Controller
    {
        private readonly AppDbContext _context;

        public ClinicsController(AppDbContext context)
        {
            _context = context;
        }

        // =========================================
        // INDEX
        // =========================================

        public async Task<IActionResult> Index(
            string search,
            decimal? minPrice,
            decimal? maxPrice,
            int pageNumber = 1)
        {
            var clinics = _context.Clinics.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                clinics = clinics.Where(c =>
                    c.Name.Contains(search) ||
                    c.Doctors.Any(d => d.FullName.Contains(search))
                );
            }

            if (minPrice != null)
                clinics = clinics.Where(c => c.Price >= minPrice);

            if (maxPrice != null)
                clinics = clinics.Where(c => c.Price <= maxPrice);

            int pageSize = 5;

            return View(await PaginatedList<Clinic>.CreateAsync(
                clinics
                    .AsNoTracking()
                    .Include(c => c.Doctors)
                    .Include(c => c.Appointments)
                    .Include(c => c.Specialty),
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

            var clinic = await _context.Clinics
                .AsNoTracking()
                .Include(c => c.Doctors)
                .Include(c => c.Appointments)
                .Include(c => c.Specialty)
                .FirstOrDefaultAsync(c => c.ClinicId == id);

            if (clinic == null)
                return NotFound();

            return View(clinic);
        }

        // =========================================
        // CREATE GET
        // =========================================

        public IActionResult Create()
        {
            ViewData["SpecialtyId"] =
                new SelectList(
                    _context.Specialties,
                    "SpecialtyId",
                    "Name"
                );

            return View();
        }

        // =========================================
        // CREATE POST
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Clinic clinic)
        {
            ModelState.Remove("PhoneNumber");
            ModelState.Remove("Address");
            ModelState.Remove("Doctors");
            ModelState.Remove("Appointments");
            ModelState.Remove("Specialty");

            if (await _context.Clinics.AnyAsync(c =>
                c.Name == clinic.Name))
            {
                ModelState.AddModelError(
                    "Name",
                    "Clinic name already exists"
                );
            }

            var specialtyExists = await _context.Specialties
                .AnyAsync(s => s.SpecialtyId == clinic.SpecialtyId);

            if (!specialtyExists)
            {
                ModelState.AddModelError(
                    "SpecialtyId",
                    "Please select a valid specialty"
                );
            }

            if (!ModelState.IsValid)
            {
                ViewData["SpecialtyId"] =
                    new SelectList(
                        _context.Specialties,
                        "SpecialtyId",
                        "Name",
                        clinic.SpecialtyId
                    );

                return View(clinic);
            }

            clinic.PhoneNumber = AppSettings.PhoneNumber;
            clinic.Address = AppSettings.Address;

            _context.Add(clinic);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Clinic created successfully";

            return RedirectToAction(nameof(Index));
        }

        // =========================================
        // EDIT GET
        // =========================================

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var clinic = await _context.Clinics.FindAsync(id);

            if (clinic == null)
                return NotFound();

            var vm = new ClincsVM
            {
                ClinicId = clinic.ClinicId,
                Name = clinic.Name,
                Price = clinic.Price,
                MaxPatients = clinic.MaxPatients,
                StartTime = clinic.StartTime,
                EndTime = clinic.EndTime,
                SpecialtyId = clinic.SpecialtyId
            };

            ViewData["SpecialtyId"] =
                new SelectList(
                    _context.Specialties,
                    "SpecialtyId",
                    "Name",
                    vm.SpecialtyId
                );

            return View(vm);
        }

        // =========================================
        // EDIT POST
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ClincsVM vm)
        {
            var clinic = await _context.Clinics.FindAsync(vm.ClinicId);

            if (clinic == null)
                return NotFound();

            if (await _context.Clinics.AnyAsync(c =>
                c.Name == vm.Name &&
                c.ClinicId != vm.ClinicId))
            {
                ModelState.AddModelError(
                    "Name",
                    "Clinic name already exists"
                );
            }

            if (!ModelState.IsValid)
            {
                ViewData["SpecialtyId"] =
                    new SelectList(
                        _context.Specialties,
                        "SpecialtyId",
                        "Name",
                        vm.SpecialtyId
                    );

                return View(vm);
            }

            clinic.Name = vm.Name;
            clinic.Price = vm.Price;
            clinic.MaxPatients = vm.MaxPatients;
            clinic.StartTime = vm.StartTime;
            clinic.EndTime = vm.EndTime;
            clinic.SpecialtyId = vm.SpecialtyId;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Clinic updated successfully";

            return RedirectToAction(nameof(Index));
        }

        // =========================================
        // DELETE GET
        // =========================================

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var clinic = await _context.Clinics
                .Include(c => c.Doctors)
                .Include(c => c.Appointments)
                .Include(c => c.Specialty)
                .FirstOrDefaultAsync(c => c.ClinicId == id);

            if (clinic == null)
                return NotFound();

            return View(clinic);
        }

        // =========================================
        // DELETE POST
        // =========================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var clinic = await _context.Clinics
                .Include(c => c.Doctors)
                .Include(c => c.Appointments)
                .FirstOrDefaultAsync(c => c.ClinicId == id);

            if (clinic == null)
                return NotFound();

            // =====================================
            // SERVER-SIDE GUARD — matches what the
            // Delete view tells the user
            // =====================================
            if (clinic.Doctors.Any() || clinic.Appointments.Any())
            {
                TempData["Error"] =
                    "Cannot delete this clinic because it has linked doctors or appointments. " +
                    "Please reassign or remove them first.";

                return RedirectToAction(nameof(Delete), new { id });
            }

            // Safe to delete — no linked data
            _context.Clinics.Remove(clinic);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Clinic deleted successfully";

            return RedirectToAction(nameof(Index));
        }

        // =========================================
        // EXISTS
        // =========================================

        private bool ClinicExists(int id)
        {
            return _context.Clinics.Any(e => e.ClinicId == id);
        }
    }
}