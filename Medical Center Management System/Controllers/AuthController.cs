using Medical_Center_Management_System.Models;
using Medical_Center_Management_System.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Medical_Center_Management_System.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AppDbContext _context;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // =========================================
        // LOGIN GET
        // =========================================

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectByRole();

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // =========================================
        // LOGIN POST
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM vm, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var result = await _signInManager.PasswordSignInAsync(
                vm.Email,
                vm.Password,
                vm.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectByRole();
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("",
                    "Account locked due to too many failed attempts. Try again in 10 minutes.");
                return View(vm);
            }

            ModelState.AddModelError("", "Invalid email or password.");
            return View(vm);
        }

        // =========================================
        // REGISTER GET
        // =========================================

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectByRole();

            return View();
        }

        // =========================================
        // REGISTER POST
        // Creates ApplicationUser + Patient record
        // in one transaction, assigns Patient role.
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            // Check phone uniqueness against existing patients
            bool phoneExists = _context.Patients
                .Any(p => p.PhoneNumber == vm.PhoneNumber);

            if (phoneExists)
            {
                ModelState.AddModelError("PhoneNumber",
                    "This phone number is already registered.");
                return View(vm);
            }

            // Create Patient domain record first
            var patient = new Patient
            {
                FullName = vm.FullName,
                PhoneNumber = vm.PhoneNumber,
                DateOfBirth = vm.DateOfBirth,
                Gender = vm.Gender
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            // Create identity user linked to that patient
            var user = new ApplicationUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                FullName = vm.FullName,
                PhoneNumber = vm.PhoneNumber,
                PatientId = patient.PatientId,
                EmailConfirmed = true   // skip email confirmation for now
            };

            var result = await _userManager.CreateAsync(user, vm.Password);

            if (!result.Succeeded)
            {
                // Roll back patient record if user creation fails
                _context.Patients.Remove(patient);
                await _context.SaveChangesAsync();

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(vm);
            }

            await _userManager.AddToRoleAsync(user, "Patient");
            await _signInManager.SignInAsync(user, isPersistent: false);

            TempData["Success"] = "Welcome to Medix! Your account has been created.";
            return RedirectToAction("Index", "PatientPortal");
        }

        // =========================================
        // LOGOUT
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        // =========================================
        // ACCESS DENIED
        // =========================================

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // =========================================
        // HELPER: redirect to role-specific home
        // =========================================

        private IActionResult RedirectByRole()
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction("Index", "Admin");

            if (User.IsInRole("Doctor"))
                return RedirectToAction("Index", "DoctorPortal");

            return RedirectToAction("Index", "PatientPortal");
        }
    }
}
