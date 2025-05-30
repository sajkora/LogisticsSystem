using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LogisticsSystem.Services;
using LogisticsSystem.Models;
using System.Threading.Tasks;
using LogisticsSystem.Services.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace LogisticsSystem.Controllers
{
    [Authorize(Roles = "Shipper")]
    public class ShipperController : Controller
    {
        private readonly ICourseService _courseService;
        private readonly IUserService _userService;
        private readonly ICourseEventService _courseEventService;
        private readonly ILogger<ShipperController> _logger;

        public ShipperController(ICourseService courseService, IUserService userService, ILogger<ShipperController> logger, ICourseEventService courseEventService)
        {
            _courseService = courseService;
            _userService = userService;
            _courseEventService = courseEventService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string searchString, string sortOrder, int? page)
        {
            var courses = await _courseService.GetAllCoursesAsync();
            return View(courses);
        }

        public async Task<IActionResult> Details(int id)
        {
            var course = await _courseService.GetCourseByIdAsync(id.ToString());
            if (course == null)
            {
                return NotFound();
            }
            return View(course);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var course = await _courseService.GetCourseByIdAsync(id.ToString());
            if (course == null)
            {
                return NotFound();
            }
            return View(course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Course course)
        {
            if (id.ToString() != course.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await _courseService.UpdateCourseAsync(course);
                TempData["SuccessMessage"] = "Course updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(course);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var course = await _courseService.GetCourseByIdAsync(id.ToString());
            if (course == null)
            {
                return NotFound();
            }
            return View(course);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _courseService.DeleteCourseAsync(id.ToString());
            TempData["SuccessMessage"] = "Course deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Events(int id)
        {
            var events = await _courseEventService.GetEventsByCourseIdAsync(id.ToString());
            return View(events);
        }

        [HttpGet]
        public async Task<IActionResult> AssignCourse()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var model = await _courseService.GetAssignCourseViewModelAsync(userId);
            if (model.Drivers == null || !model.Drivers.Any())
            {
                TempData["ErrorMessage"] = "No drivers available for assignment.";
                return RedirectToAction(nameof(MonitorCourses));
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignCourse(AssignCourseViewModel model, IFormFile document)
        {
            model.ShipperId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var (success, error, resultModel) = await _courseService.AssignCourseFromViewModelAsync(model, document);
            if (!success)
            {
                if (!string.IsNullOrEmpty(error))
                {
                    ModelState.AddModelError("", error);
                }
                return View(resultModel);
            }
            TempData["SuccessMessage"] = "Course assigned successfully.";
            return RedirectToAction(nameof(AssignCourse));
        }

        public async Task<IActionResult> MonitorCourses(string startingPoint = null, string destination = null, string description = null, string status = null, string shipper = null, string driver = null)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var (ongoing, completed, users, filters) = await _courseService.GetShipperCoursesViewModelAsync(userId, startingPoint, destination, description, status, shipper, driver);
            ViewBag.Users = users;
            ViewBag.FilterStartingPoint = startingPoint;
            ViewBag.FilterDestination = destination;
            ViewBag.FilterDescription = description;
            ViewBag.FilterStatus = status;
            ViewBag.FilterShipper = shipper;
            ViewBag.FilterDriver = driver;
            ViewBag.UserId = userId;
            return View((ongoing, completed));
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(string id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var (bytes, fileName) = await _courseService.GetShipperCourseDocumentAsync(id, userId);
            if (bytes == null)
                return NotFound();
            return File(bytes, "application/pdf", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> EditCourse(string id)
        {
            var course = await _courseService.GetCourseByIdAsync(id);
            if (course == null)
                return NotFound();
            ViewBag.Users = (await _userService.GetAllUsersAsync()).ToList();
            return View(course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(string id, Course model, IFormFile document, bool swapPdf)
        {
            if (id != model.Id)
                return NotFound();
            var (success, error) = await _courseService.UpdateCourseFromViewModelAsync(model, document, swapPdf);
            if (!success)
            {
                ModelState.AddModelError("", error);
                ViewBag.Users = (await _userService.GetAllUsersAsync()).ToList();
                return View(model);
            }
            TempData["SuccessMessage"] = "Course updated successfully.";
            return RedirectToAction(nameof(MonitorCourses));
        }

        [HttpGet]
        public async Task<IActionResult> DeleteCourse(string id)
        {
            var course = await _courseService.GetCourseByIdAsync(id);
            if (course == null)
                return NotFound();
            ViewBag.Users = (await _userService.GetAllUsersAsync()).ToList();
            return View(course);
        }

        [HttpPost, ActionName("DeleteCourse")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourseConfirmed(string id)
        {
            await _courseService.DeleteCourseAsync(id);
            TempData["SuccessMessage"] = "Course deleted successfully.";
            return RedirectToAction(nameof(MonitorCourses));
        }

        [HttpGet]
        public async Task<IActionResult> ViewEvents(string courseId)
        {
            var events = await _courseEventService.GetEventsByCourseIdAsync(courseId);
            var course = await _courseService.GetCourseByIdAsync(courseId);
            ViewBag.Course = course;
            ViewBag.Users = (await _userService.GetAllUsersAsync()).ToList();
            return View(events);
        }
    }
}
