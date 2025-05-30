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

        #region User Management

        // GET: /Admin/ManageUsers
        public async Task<IActionResult> ManageUsers()
            => View(await _userService.GetManageUsersViewModelAsync());

        // GET: /Admin/CreateUser
        public IActionResult CreateUser() => View();

        // POST: /Admin/CreateUser
        [HttpPost]
        public async Task<IActionResult> CreateUser(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var (success, error) = await _userService.CreateUserFromViewModelAsync(model);
            if (!success)
            {
                ModelState.AddModelError("", error);
                return View(model);
            }
            return RedirectToAction("ManageUsers");
        }

        // GET: /Admin/EditUser?id=...
        public async Task<IActionResult> EditUser(string id)
            => View(await _userService.GetEditUserViewModelAsync(id));

        // POST: /Admin/EditUser
        [HttpPost]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var (success, error) = await _userService.UpdateUserFromViewModelAsync(model);
            if (!success)
            {
                ModelState.AddModelError("", error);
                return View(model);
            }
            return RedirectToAction("ManageUsers");
        }

        // GET: /Admin/DeleteUser?id=...
        public async Task<IActionResult> DeleteUser(string id)
            => View(await _userService.GetUserByIdAsync(id));

        // POST: /Admin/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(User model)
        {
            await _userService.DeleteUserAsync(model.Id);
            return RedirectToAction("ManageUsers");
        }

        #endregion

        #region Course Management

        // GET: /Admin/ManageCourses
        public async Task<IActionResult> ManageCourses(string startingPoint = null, string destination = null, string description = null, string status = null, string shipper = null, string driver = null)
        {
            var (ongoing, completed, users, filters) = await _courseService.GetManageCoursesViewModelAsync(startingPoint, destination, description, status, shipper, driver);
            ViewBag.Users = users;
            ViewBag.FilterStartingPoint = startingPoint;
            ViewBag.FilterDestination = destination;
            ViewBag.FilterDescription = description;
            ViewBag.FilterStatus = status;
            ViewBag.FilterShipper = shipper;
            ViewBag.FilterDriver = driver;
            return View((ongoing, completed));
        }

        // GET: /Admin/CreateCourse
        public async Task<IActionResult> CreateCourse()
        {
            ViewBag.Users = (await _userService.GetAllUsersAsync()).ToList();
            return View();
        }

        // POST: /Admin/CreateCourse
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(Course model, IFormFile document)
        {
            ViewBag.Users = (await _userService.GetAllUsersAsync()).ToList();
            var (success, error) = await _courseService.CreateCourseFromViewModelAsync(model, document);
            if (!success)
            {
                ModelState.AddModelError("", error);
                return View(model);
            }
            TempData["SuccessMessage"] = "Course created successfully.";
            return RedirectToAction("ManageCourses");
        }

        // GET: /Admin/EditCourse?id=...
        public async Task<IActionResult> EditCourse(string id)
        {
            ViewBag.Users = (await _userService.GetAllUsersAsync()).ToList();
            return View(await _courseService.GetCourseByIdAsync(id));
        }

        // POST: /Admin/EditCourse
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(Course model, IFormFile document)
        {
            ViewBag.Users = (await _userService.GetAllUsersAsync()).ToList();
            bool swapPdf = Request.Form["SwapPdf"] == "on";
            var (success, error) = await _courseService.UpdateCourseFromViewModelAsync(model, document, swapPdf);
            if (!success)
            {
                ModelState.AddModelError("", error);
                return View(model);
            }
            TempData["SuccessMessage"] = "Course updated successfully.";
            return RedirectToAction("ManageCourses");
        }

        // GET: /Admin/DeleteCourse?id=...
        public async Task<IActionResult> DeleteCourse(string id)
            => View(await _courseService.GetCourseByIdAsync(id));

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
            var (documentBytes, fileName) = await _courseService.GetCourseDocumentWithNameAsync(id);
            if (documentBytes == null) return NotFound();
            return File(documentBytes, "application/pdf", fileName);
        }

        #endregion
    }
}
