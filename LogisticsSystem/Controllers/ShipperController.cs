using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LogisticsSystem.Services.Contracts;
using LogisticsSystem.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Microsoft.Extensions.Logging;
using LogisticsSystem.Services;

[Authorize(Roles = "Shipper,Admin")]
public class ShipperController : Controller
{
    private readonly ICourseService _courseService;
    private readonly IUserService _userService;
    private readonly ILogger<ShipperController> _logger;
    private readonly ICourseEventService _courseEventService;

    public ShipperController(ICourseService courseService, IUserService userService, ILogger<ShipperController> logger, ICourseEventService courseEventService)
    {
        _courseService = courseService;
        _userService = userService;
        _logger = logger;
        _courseEventService = courseEventService;
    }

    // GET: /Shipper/AssignCourse
    public async Task<IActionResult> AssignCourse()
    {
        try
        {
            _logger.LogInformation("Starting AssignCourse GET action");
            
            var drivers = await _userService.GetAllUsersAsync();
            _logger.LogInformation($"Retrieved {drivers?.Count() ?? 0} users from database");

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userService.GetUserByIdAsync(userId);
                if (user != null)
                {
                    ViewBag.UserName = user.Name;
                    ViewBag.UserEmail = user.Email;
                    ViewBag.UserId = user.Id;
                }
            }

            var model = new AssignCourseViewModel
            {
                Drivers = drivers.Where(u => u.Role == "Driver").ToList(),
                ShipperId = userId
            };

            _logger.LogInformation($"Filtered to {model.Drivers?.Count ?? 0} drivers");

            if (model.Drivers == null || !model.Drivers.Any())
            {
                _logger.LogWarning("No drivers available for assignment");
                TempData["ErrorMessage"] = "No drivers available for assignment.";
                return RedirectToAction(nameof(MonitorCourses));
            }

            _logger.LogInformation("Successfully prepared AssignCourse view model");
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AssignCourse GET action");
            TempData["ErrorMessage"] = "An error occurred while loading the assign course form.";
            return RedirectToAction(nameof(MonitorCourses));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignCourse(AssignCourseViewModel model, IFormFile document)
    {
        // Set ShipperId as the very first thing
        model.ShipperId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        ModelState.Remove(nameof(model.ShipperId));

        // Get the drivers list
        var drivers = await _userService.GetAllUsersAsync();
        model.Drivers = drivers.Where(u => u.Role == "Driver").ToList();
        ModelState.Remove(nameof(model.Drivers));

        // Pass user info to the view
        if (!string.IsNullOrEmpty(model.ShipperId))
        {
            var user = await _userService.GetUserByIdAsync(model.ShipperId);
            if (user != null)
            {
                ViewBag.UserName = user.Name;
                ViewBag.UserEmail = user.Email;
                ViewBag.UserId = user.Id;
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var course = new Course
        {
            DriverId = model.DriverId,
            ShipperId = model.ShipperId,
            StartingPoint = model.StartingPoint,
            Destination = model.Destination,
            Description = model.Description,
            IsCompleted = false
        };

        await _courseService.AssignCourseAsync(course, document);
        TempData["SuccessMessage"] = "Course assigned successfully.";
        
        // Clear the form by creating a new model
        model = new AssignCourseViewModel
        {
            Drivers = drivers.Where(u => u.Role == "Driver").ToList(),
            ShipperId = model.ShipperId
        };
        
        return View(model);
    }

    // Widok monitorowania kursów – lista wszystkich kursów
    public async Task<IActionResult> MonitorCourses(string startingPoint = null, string destination = null, string description = null, string status = null, string shipper = null, string driver = null)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        ViewBag.UserId = userId;
        var courses = await _courseService.GetAllCoursesAsync();
        if (!string.IsNullOrEmpty(startingPoint))
            courses = courses.Where(c => c.StartingPoint != null && c.StartingPoint.Contains(startingPoint, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(destination))
            courses = courses.Where(c => c.Destination != null && c.Destination.Contains(destination, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(description))
            courses = courses.Where(c => c.Description != null && c.Description.Contains(description, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(status))
        {
            if (status == "Completed")
                courses = courses.Where(c => c.IsCompleted);
            else if (status == "In Progress")
                courses = courses.Where(c => !c.IsCompleted);
        }
        if (!string.IsNullOrEmpty(shipper))
            courses = courses.Where(c => c.ShipperId != null && c.ShipperId == shipper);
        if (!string.IsNullOrEmpty(driver))
            courses = courses.Where(c => c.DriverId != null && c.DriverId == driver);

        var users = await _userService.GetAllUsersAsync();
        ViewBag.Users = users.ToList();
        ViewBag.FilterStartingPoint = startingPoint;
        ViewBag.FilterDestination = destination;
        ViewBag.FilterDescription = description;
        ViewBag.FilterStatus = status;
        ViewBag.FilterShipper = shipper;
        ViewBag.FilterDriver = driver;
        var ongoing = courses.Where(c => !c.IsCompleted);
        var completed = courses.Where(c => c.IsCompleted);
        return View((ongoing, completed));
    }

    // GET: /Shipper/EditCourse/{id}
    public async Task<IActionResult> EditCourse(string id)
    {
        var course = await _courseService.GetCourseByIdAsync(id);
        var shipperId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (course == null || course.ShipperId != shipperId)
            return NotFound();
        var users = await _userService.GetAllUsersAsync();
        ViewBag.Users = users.ToList();
        return View(course);
    }

    // POST: /Shipper/EditCourse
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCourse(Course model, IFormFile document)
    {
        var shipperId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (model.ShipperId != shipperId)
        {
            return Forbid();
        }

        // Check if the SwapPdf checkbox was checked
        bool swapPdf = Request.Form["SwapPdf"] == "on";

        if (swapPdf && document != null && document.Length > 0)
        {
            var fileService = HttpContext.RequestServices.GetService(typeof(IFileService)) as IFileService;
            var filePath = await fileService.SaveFileAsync(document, "CourseDocuments");
            model.DocumentFileName = document.FileName;
            model.DocumentPath = filePath;
            model.DocumentUploadDate = DateTime.UtcNow;
            ModelState.Remove("DocumentFileName");
            ModelState.Remove("DocumentPath");
        }
        else
        {
            // No new file uploaded or swap not requested, keep the existing file info
            var existingCourse = await _courseService.GetCourseByIdAsync(model.Id);
            if (existingCourse != null)
            {
                model.DocumentFileName = existingCourse.DocumentFileName;
                model.DocumentPath = existingCourse.DocumentPath;
                model.DocumentUploadDate = existingCourse.DocumentUploadDate;
                ModelState.Remove("DocumentFileName");
                ModelState.Remove("DocumentPath");
            }
        }

        if (!ModelState.IsValid)
        {
            // Add all errors to ModelState as general errors for display
            foreach (var key in ModelState.Keys)
            {
                var errors = ModelState[key].Errors;
                foreach (var error in errors)
                {
                    ModelState.AddModelError("", $"{key}: {error.ErrorMessage}");
                }
            }
            return View(model);
        }

        try
        {
            await _courseService.UpdateCourseAsync(model);
            TempData["SuccessMessage"] = "Course updated successfully.";
            return RedirectToAction("MonitorCourses");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "An error occurred while updating the course.");
            return View(model);
        }
    }

    // GET: /Shipper/DeleteCourse/{id}
    public async Task<IActionResult> DeleteCourse(string id)
    {
        var course = await _courseService.GetCourseByIdAsync(id);
        var shipperId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (course == null || course.ShipperId != shipperId)
            return NotFound();
        return View(course);
    }

    // POST: /Shipper/DeleteCourse
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCourse(Course model)
    {
        var course = await _courseService.GetCourseByIdAsync(model.Id);
        var shipperId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (course == null || course.ShipperId != shipperId)
            return NotFound();
        await _courseService.DeleteCourseAsync(model.Id);
        TempData["SuccessMessage"] = "Course deleted successfully.";
        return RedirectToAction("MonitorCourses");
    }

    [HttpGet]
    public async Task<IActionResult> DownloadDocument(string id)
    {
        var documentBytes = await _courseService.GetCourseDocumentAsync(id);
        if (documentBytes == null)
        {
            return NotFound();
        }

        var course = await _courseService.GetCourseByIdAsync(id);
        return File(documentBytes, "application/pdf", course.DocumentFileName);
    }

    // GET: /Shipper/ViewEvents?courseId=...
    public async Task<IActionResult> ViewEvents(string courseId)
    {
        var course = await _courseService.GetCourseByIdAsync(courseId);
        if (course == null)
            return NotFound();
        var events = await _courseEventService.GetEventsByCourseIdAsync(courseId);
        var users = await _userService.GetAllUsersAsync();
        ViewBag.Course = course;
        ViewBag.Users = users.ToList();
        return View(events);
    }
}
