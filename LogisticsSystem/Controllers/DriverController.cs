using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LogisticsSystem.Services.Contracts;
using LogisticsSystem.Models;
using System.Threading.Tasks;
using System;
using System.Linq;

[Authorize(Roles = "Driver")]
public class DriverController : Controller
{
    private readonly ICourseService _courseService;
    private readonly ICourseEventService _courseEventService;
    private readonly IUserService _userService;

    public DriverController(ICourseService courseService, ICourseEventService courseEventService, IUserService userService)
    {
        _courseService = courseService;
        _courseEventService = courseEventService;
        _userService = userService;
    }

    // Widok dashboardu kierowcy – pobiera kursy przypisane do aktualnie zalogowanego kierowcy
    public async Task<IActionResult> MyCourses()
    {
        var driverId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var courses = await _courseService.GetDriverCoursesAsync(driverId);
        var ongoing = courses.Where(c => !c.IsCompleted);
        var completed = courses.Where(c => c.IsCompleted);
        return View((ongoing, completed));
    }

    // GET: /Driver/ReportEvent?courseId=...
    public async Task<IActionResult> ReportEvent(string courseId)
    {
        var driverId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var course = await _courseService.GetCourseByIdAsync(courseId);
        if (course == null || course.DriverId != driverId)
            return NotFound();

        var model = new CourseEvent
        {
            CourseId = courseId,
            DriverId = driverId
        };
        return View(model);
    }

    // POST: /Driver/ReportEvent
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportEvent(CourseEvent model)
    {
        var driverId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (model.DriverId != driverId)
            return Forbid();
        model.Timestamp = DateTime.UtcNow;
        if (!ModelState.IsValid)
            return View(model);
        await _courseEventService.AddEventAsync(model);
        TempData["SuccessMessage"] = "Event reported successfully.";
        return RedirectToAction("MyCourses");
    }

    [HttpGet]
    public async Task<IActionResult> DownloadDocument(string id)
    {
        var driverId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var course = await _courseService.GetCourseByIdAsync(id);
        
        if (course == null || course.DriverId != driverId)
            return NotFound();

        var documentBytes = await _courseService.GetCourseDocumentAsync(id);
        if (documentBytes == null)
            return NotFound();

        return File(documentBytes, "application/pdf", course.DocumentFileName);
    }
}
