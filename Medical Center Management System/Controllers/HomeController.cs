using Microsoft.AspNetCore.Mvc;

namespace Medical_Center_Management_System.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            //?? ???????? “???? ??? ??????” ??????
            //?? ?? ? ???? ?? ???? ??? Login   ?? ??? ? ?????? ??????
            // If already logged in, redirect to appropriate portal
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Index", "Admin");

                if (User.IsInRole("Doctor"))
                    return RedirectToAction("Index", "DoctorPortal");

                return RedirectToAction("Index", "PatientPortal");
            }

            return View();
        }
        //?? ?? ??? ??? Landing ? ???? ?? ???? ??? Index”
        public IActionResult Landing()
        {
            return View("Index");
        }
    }
}
