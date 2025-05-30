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
        var (ongoing, completed) = await _courseService.GetDriverDashboardAsync(driverId);
        return View((ongoing, completed));
    }

    // GET: /Driver/ReportEvent?courseId=...
    public async Task<IActionResult> ReportEvent(string courseId)
    {
        var driverId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var course = await _courseService.GetCourseByIdAsync(courseId);
        if (course == null || course.DriverId != driverId)
            return NotFound();
        var model = new CourseEvent { CourseId = courseId, DriverId = driverId };
        return View(model);
    }

    // POST: /Driver/ReportEvent
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportEvent(CourseEvent model)
    {
        var driverId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var (success, error) = await _courseEventService.ReportEventAsync(model, driverId);
        if (!success)
        {
            if (error == "Forbidden") return Forbid();
            ModelState.AddModelError("", error);
            return View(model);
        }
        TempData["SuccessMessage"] = "Event reported successfully.";
        return RedirectToAction("MyCourses");
    }

    [HttpGet]
    public async Task<IActionResult> DownloadDocument(string id)
    {
        var driverId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var (documentBytes, fileName) = await _courseService.GetDriverCourseDocumentAsync(id, driverId);
        if (documentBytes == null) return NotFound();
        return File(documentBytes, "application/pdf", fileName);
    }
}
