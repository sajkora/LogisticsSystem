using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using LogisticsSystem.Models;
using LogisticsSystem.Services.Contracts;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace LogisticsSystem.Services
{
    public class CourseService : ICourseService
    {
        private readonly IMongoCollection<Course> _courses;
        private readonly IFileService _fileService;
        private readonly IUserService _userService;

        public CourseService(IConfiguration configuration, IFileService fileService, IUserService userService)
        {
            var client = new MongoClient(configuration["MongoDB:ConnectionString"]);
            var database = client.GetDatabase(configuration["MongoDB:DatabaseName"]);
            _courses = database.GetCollection<Course>("Courses");
            _fileService = fileService;
            _userService = userService;
        }

        public async Task<IEnumerable<Course>> GetAllCoursesAsync()
        {
            return await _courses.Find(_ => true).ToListAsync();
        }

        public async Task<Course> GetCourseByIdAsync(string id)
        {
            return await _courses.Find(c => c.Id == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Course>> GetDriverCoursesAsync(string driverId)
        {
            return await _courses.Find(c => c.DriverId == driverId).ToListAsync();
        }

        public async Task<Course> CreateCourseAsync(Course course)
        {
            await _courses.InsertOneAsync(course);
            return course;
        }

        public async Task<Course> AssignCourseAsync(Course course, IFormFile document = null)
        {
            try
            {
                if (document != null)
                {
                    var filePath = await _fileService.SaveFileAsync(document, "CourseDocuments");
                    course.DocumentFileName = document.FileName;
                    course.DocumentPath = filePath;
                    course.DocumentUploadDate = DateTime.UtcNow;
                }

                await _courses.InsertOneAsync(course);
                return course;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error assigning course: {ex.Message}", ex);
            }
        }

        public async Task UpdateCourseAsync(Course course)
        {
            await _courses.ReplaceOneAsync(c => c.Id == course.Id, course);
        }

        public async Task<byte[]> GetCourseDocumentAsync(string courseId)
        {
            var course = await GetCourseByIdAsync(courseId);
            if (course == null || string.IsNullOrEmpty(course.DocumentPath))
                return null;

            return await _fileService.GetFileAsync(course.DocumentPath);
        }

        public async Task DeleteCourseAsync(string courseId)
        {
            var course = await GetCourseByIdAsync(courseId);
            if (course != null && !string.IsNullOrEmpty(course.DocumentPath))
            {
                _fileService.DeleteFile(course.DocumentPath);
            }
            await _courses.DeleteOneAsync(c => c.Id == courseId);
        }

        public async Task<(IEnumerable<Course> Ongoing, IEnumerable<Course> Completed, List<User> Users, object Filters)> GetManageCoursesViewModelAsync(string startingPoint, string destination, string description, string status, string shipper, string driver)
        {
            var courses = await GetAllCoursesAsync();
            if (!string.IsNullOrEmpty(startingPoint))
                courses = courses.Where(c => c.StartingPoint != null && c.StartingPoint.Contains(startingPoint, System.StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(destination))
                courses = courses.Where(c => c.Destination != null && c.Destination.Contains(destination, System.StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(description))
                courses = courses.Where(c => c.Description != null && c.Description.Contains(description, System.StringComparison.OrdinalIgnoreCase));
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
            var users = (await _userService.GetAllUsersAsync()).ToList();
            var ongoing = courses.Where(c => !c.IsCompleted);
            var completed = courses.Where(c => c.IsCompleted);
            return (ongoing, completed, users, null);
        }

        public async Task<(bool Success, string ErrorMessage)> CreateCourseFromViewModelAsync(Course model, IFormFile document)
        {
            try
            {
                if (document != null && document.Length > 0)
                {
                    var filePath = await _fileService.SaveFileAsync(document, "CourseDocuments");
                    model.DocumentFileName = document.FileName;
                    model.DocumentPath = filePath;
                    model.DocumentUploadDate = System.DateTime.UtcNow;
                }
                await CreateCourseAsync(model);
                return (true, null);
            }
            catch
            {
                return (false, "An error occurred while creating the course. Please try again.");
            }
        }

        public async Task<(bool Success, string ErrorMessage)> UpdateCourseFromViewModelAsync(Course model, IFormFile document, bool swapPdf)
        {
            try
            {
                if (swapPdf && document != null && document.Length > 0)
                {
                    var filePath = await _fileService.SaveFileAsync(document, "CourseDocuments");
                    model.DocumentFileName = document.FileName;
                    model.DocumentPath = filePath;
                    model.DocumentUploadDate = System.DateTime.UtcNow;
                }
                else
                {
                    var existingCourse = await GetCourseByIdAsync(model.Id);
                    if (existingCourse != null)
                    {
                        model.DocumentFileName = existingCourse.DocumentFileName;
                        model.DocumentPath = existingCourse.DocumentPath;
                        model.DocumentUploadDate = existingCourse.DocumentUploadDate;
                    }
                }
                await UpdateCourseAsync(model);
                return (true, null);
            }
            catch
            {
                return (false, "An error occurred while updating the course.");
            }
        }

        public async Task<(byte[] DocumentBytes, string FileName)> GetCourseDocumentWithNameAsync(string id)
        {
            var course = await GetCourseByIdAsync(id);
            if (course == null || string.IsNullOrEmpty(course.DocumentPath))
                return (null, null);
            var bytes = await _fileService.GetFileAsync(course.DocumentPath);
            return (bytes, course.DocumentFileName);
        }

        public async Task<(IEnumerable<Course> Ongoing, IEnumerable<Course> Completed)> GetDriverDashboardAsync(string driverId)
        {
            var courses = await GetDriverCoursesAsync(driverId);
            return (courses.Where(c => !c.IsCompleted), courses.Where(c => c.IsCompleted));
        }

        public async Task<(byte[] DocumentBytes, string FileName)> GetDriverCourseDocumentAsync(string courseId, string driverId)
        {
            var course = await GetCourseByIdAsync(courseId);
            if (course == null || course.DriverId != driverId)
                return (null, null);
            var bytes = await GetCourseDocumentAsync(courseId);
            return (bytes, course.DocumentFileName);
        }

        public async Task<AssignCourseViewModel> GetAssignCourseViewModelAsync(string shipperId)
        {
            var drivers = await _userService.GetAllUsersAsync();
            var model = new AssignCourseViewModel
            {
                Drivers = drivers.Where(u => u.Role == "Driver").ToList(),
                ShipperId = shipperId
            };
            return model;
        }

        public async Task<(bool Success, string ErrorMessage, AssignCourseViewModel Model)> AssignCourseFromViewModelAsync(AssignCourseViewModel model, IFormFile document)
        {
            if (string.IsNullOrEmpty(model.ShipperId))
                return (false, "ShipperId is required.", model);

            var drivers = await _userService.GetAllUsersAsync();
            model.Drivers = drivers.Where(u => u.Role == "Driver").ToList();

            if (!model.Drivers.Any())
                return (false, "No drivers available for assignment.", model);

            if (!string.IsNullOrEmpty(model.ShipperId))
            {
                var user = await _userService.GetUserByIdAsync(model.ShipperId);
                if (user != null)
                {
                    // Optionally, you can return user info for ViewBag if needed
                }
            }

            if (string.IsNullOrEmpty(model.DriverId) || string.IsNullOrEmpty(model.StartingPoint) || string.IsNullOrEmpty(model.Destination))
                return (false, "All required fields must be filled.", model);

            var course = new Course
            {
                DriverId = model.DriverId,
                ShipperId = model.ShipperId,
                StartingPoint = model.StartingPoint,
                Destination = model.Destination,
                Description = model.Description,
                IsCompleted = false
            };

            await AssignCourseAsync(course, document);
            return (true, null, model);
        }

        public async Task<(IEnumerable<Course> Ongoing, IEnumerable<Course> Completed, List<User> Users, object Filters)> GetShipperCoursesViewModelAsync(string userId, string startingPoint, string destination, string description, string status, string shipper, string driver)
        {
            var courses = await GetAllCoursesAsync();
            if (!string.IsNullOrEmpty(startingPoint))
                courses = courses.Where(c => c.StartingPoint != null && c.StartingPoint.Contains(startingPoint, System.StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(destination))
                courses = courses.Where(c => c.Destination != null && c.Destination.Contains(destination, System.StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(description))
                courses = courses.Where(c => c.Description != null && c.Description.Contains(description, System.StringComparison.OrdinalIgnoreCase));
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
            var users = (await _userService.GetAllUsersAsync()).ToList();
            var ongoing = courses.Where(c => !c.IsCompleted);
            var completed = courses.Where(c => c.IsCompleted);
            return (ongoing, completed, users, null);
        }

        public async Task<(byte[] DocumentBytes, string FileName)> GetShipperCourseDocumentAsync(string courseId, string userId)
        {
            var course = await GetCourseByIdAsync(courseId);
            if (course == null || course.ShipperId != userId || string.IsNullOrEmpty(course.DocumentPath))
                return (null, null);
            var bytes = await GetCourseDocumentAsync(courseId);
            return (bytes, course.DocumentFileName);
        }
    }
}
