using Medical_Center_Management_System.Models;
using Medical_Center_Management_System.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Medical_Center_Management_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;

        public UserManagementController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // =========================================
        // INDEX — list all users with roles
        // =========================================

        public async Task<IActionResult> Index(string? search, int pageNumber = 1)
        {
            var users = await _userManager.Users
                .Include(u => u.Patient)
                .Include(u => u.Doctor)
                .AsNoTracking()
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                users = users
                    .Where(u =>
                        u.FullName.ToLower().Contains(search) ||
                        (u.Email ?? "").ToLower().Contains(search))
                    .ToList();
            }

            // Attach roles to each user for display
            var userRoles = new List<UserWithRolesVM>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles.Add(new UserWithRolesVM
                {
                    User = user,
                    Roles = roles.ToList()
                });
            }

            ViewBag.Search = search;

            int pageSize = 5;

            return View(PaginatedList<UserWithRolesVM>.Create(
                userRoles,
                pageNumber,
                pageSize
            ));
        }

        // =========================================
        // ASSIGN DOCTOR ROLE
        // Links an existing user account to a Doctor
        // record so they can log in as a Doctor.
        // =========================================

        [HttpGet]
        public async Task<IActionResult> AssignDoctor(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Only makes sense for Patient-role users (or no role yet)
            ViewBag.User = user;
            ViewData["DoctorId"] = new SelectList(
                await _context.Doctors
                    .Include(d => d.Specialty)
                    .AsNoTracking()
                    .ToListAsync(),
                "DoctorId",
                "FullName");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDoctor(string userId, int doctorId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var doctor = await _context.Doctors.FindAsync(doctorId);
            if (doctor == null) return NotFound();

            // Remove Patient role if present, add Doctor role
            if (await _userManager.IsInRoleAsync(user, "Patient"))
                await _userManager.RemoveFromRoleAsync(user, "Patient");

            if (!await _userManager.IsInRoleAsync(user, "Doctor"))
                await _userManager.AddToRoleAsync(user, "Doctor");

            // Link doctor record
            user.DoctorId = doctorId;
            user.PatientId = null;    // clear patient link
            await _userManager.UpdateAsync(user);

            TempData["Success"] =
                $"{user.FullName} has been assigned the Doctor role.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================
        // DELETE USER (and their domain record)
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Prevent deleting your own admin account
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == userId)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Index));
            }

            await _userManager.DeleteAsync(user);

            TempData["Success"] = "User account deleted.";
            return RedirectToAction(nameof(Index));
        }
    }

    // =========================================
    // ViewModel for index list
    // =========================================

    public class UserWithRolesVM
    {
        public ApplicationUser User { get; set; } = null!;
        public List<string> Roles { get; set; } = new();
    }
}