using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using LogisticsSystem.Models;
using LogisticsSystem.Services;
using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using LogisticsSystem.Services.Contracts;

namespace LogisticsSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IUserService _userService;
        private readonly ICourseService _courseService;
        private readonly IFileService _fileService;

        public AdminController(IUserService userService, ICourseService courseService, IFileService fileService)
        {
            _userService = userService;
            _courseService = courseService;
            _fileService = fileService;
        }

        #region Zarządzanie użytkownikami

        // GET: /Admin/ManageUsers
        public async Task<IActionResult> ManageUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            var adminUsers = users.Where(u => u.Role == "Admin").ToList();
            var shipperUsers = users.Where(u => u.Role == "Shipper").ToList();
            var driverUsers = users.Where(u => u.Role == "Driver").ToList();
            
            var viewModel = new ManageUsersViewModel
            {
                AdminUsers = adminUsers,
                ShipperUsers = shipperUsers,
                DriverUsers = driverUsers
            };
            
            return View(viewModel);
        }

        // GET: /Admin/CreateUser
        public IActionResult CreateUser()
        {
            return View();
        }

        // POST: /Admin/CreateUser
        [HttpPost]
        public async Task<IActionResult> CreateUser(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existingUser = await _userService.GetUserByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "Użytkownik o podanym adresie email już istnieje.");
                return View(model);
            }

            var newUser = new User
            {
                Name = model.Name,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = model.Role
            };

            await _userService.CreateUserAsync(newUser);
            return RedirectToAction("ManageUsers");
        }

        // GET: /Admin/EditUser?id=...
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();

            // Mapowanie z modelu domenowego na DTO
            var model = new EditUserViewModel
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role
                // Password pozostaje pusty, jeśli użytkownik nie chce zmieniać hasła
            };

            return View(model);
        }

        // POST: /Admin/EditUser
        [HttpPost]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userService.GetUserByIdAsync(model.Id);
            if (user == null)
                return NotFound();

            user.Name = model.Name;
            user.Email = model.Email;
            user.Role = model.Role;

            // Aktualizacja hasła tylko, jeśli użytkownik podał nowe hasło
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
            }

            await _userService.UpdateUserAsync(user);
            return RedirectToAction("ManageUsers");
        }

        // GET: /Admin/DeleteUser?id=...
        public async Task<IActionResult> DeleteUser(string id)
        {
            await _userService.DeleteUserAsync(id);
            return RedirectToAction("ManageUsers");
        }

        #endregion

        #region Zarządzanie kursami

        // GET: /Admin/ManageCourses
        public async Task<IActionResult> ManageCourses(string startingPoint = null, string destination = null, string description = null, string status = null, string shipper = null, string driver = null)
        {
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

        // GET: /Admin/CreateCourse
        public async Task<IActionResult> CreateCourse()
        {
            var users = await _userService.GetAllUsersAsync();
            ViewBag.Users = users.ToList();
            return View();
        }

        // POST: /Admin/CreateCourse
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(Course model, IFormFile document)
        {
            var users = await _userService.GetAllUsersAsync();
            ViewBag.Users = users.ToList();

            // Handle file upload first
            if (document != null && document.Length > 0)
            {
                var filePath = await _fileService.SaveFileAsync(document, "CourseDocuments");
                model.DocumentFileName = document.FileName;
                model.DocumentPath = filePath;
                model.DocumentUploadDate = DateTime.UtcNow;
            }

            // Remove validation for document fields if you want them to be optional
            ModelState.Remove("DocumentFileName");
            ModelState.Remove("DocumentPath");

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    ModelState.AddModelError("", error.ErrorMessage);
                }
                return View(model);
            }

            try
            {
                await _courseService.CreateCourseAsync(model);
                TempData["SuccessMessage"] = "Course created successfully.";
                return RedirectToAction("ManageCourses");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while creating the course. Please try again.");
                return View(model);
            }
        }

        // GET: /Admin/EditCourse?id=...
        public async Task<IActionResult> EditCourse(string id)
        {
            var course = await _courseService.GetCourseByIdAsync(id);
            if (course == null)
                return NotFound();

            var users = await _userService.GetAllUsersAsync();
            ViewBag.Users = users.ToList();
            return View(course);
        }

        // POST: /Admin/EditCourse
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(Course model, IFormFile document)
        {
            var users = await _userService.GetAllUsersAsync();
            ViewBag.Users = users.ToList();

            // Always remove document validation for edit
            ModelState.Remove("DocumentFileName");
            ModelState.Remove("DocumentPath");

            // Check if the SwapPdf checkbox was checked
            bool swapPdf = Request.Form["SwapPdf"] == "on";

            if (swapPdf && document != null && document.Length > 0)
            {
                var filePath = await _fileService.SaveFileAsync(document, "CourseDocuments");
                model.DocumentFileName = document.FileName;
                model.DocumentPath = filePath;
                model.DocumentUploadDate = DateTime.UtcNow;
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
                }
            }

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    ModelState.AddModelError("", error.ErrorMessage);
                }
                return View(model);
            }

            try
            {
                await _courseService.UpdateCourseAsync(model);
                TempData["SuccessMessage"] = "Course updated successfully.";
                return RedirectToAction("ManageCourses");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating the course.");
                return View(model);
            }
        }

        // GET: /Admin/DeleteCourse?id=...
        public async Task<IActionResult> DeleteCourse(string id)
        {
            var course = await _courseService.GetCourseByIdAsync(id);
            if (course == null)
                return NotFound();
            return View(course);
        }

        // POST: /Admin/DeleteCourse
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourse(Course model)
        {
            await _courseService.DeleteCourseAsync(model.Id);
            TempData["SuccessMessage"] = "Course deleted successfully.";
            return RedirectToAction("ManageCourses");
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

        #endregion
    }
}
