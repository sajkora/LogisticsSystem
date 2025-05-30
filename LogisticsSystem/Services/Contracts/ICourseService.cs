using System.Collections.Generic;
using System.Threading.Tasks;
using LogisticsSystem.Models;
using Microsoft.AspNetCore.Http;

namespace LogisticsSystem.Services.Contracts
{
    public interface ICourseService
    {
        Task<IEnumerable<Course>> GetAllCoursesAsync();
        Task<Course> GetCourseByIdAsync(string id);
        Task<IEnumerable<Course>> GetDriverCoursesAsync(string driverId);
        Task<Course> CreateCourseAsync(Course course);
        Task<Course> AssignCourseAsync(Course course, IFormFile document = null);
        Task UpdateCourseAsync(Course course);
        Task DeleteCourseAsync(string courseId);
        Task<byte[]> GetCourseDocumentAsync(string courseId);
        Task<(IEnumerable<Course> Ongoing, IEnumerable<Course> Completed, List<User> Users, object Filters)> GetManageCoursesViewModelAsync(string startingPoint, string destination, string description, string status, string shipper, string driver);
        Task<(bool Success, string ErrorMessage)> CreateCourseFromViewModelAsync(Course model, IFormFile document);
        Task<(bool Success, string ErrorMessage)> UpdateCourseFromViewModelAsync(Course model, IFormFile document, bool swapPdf);
        Task<(byte[] DocumentBytes, string FileName)> GetCourseDocumentWithNameAsync(string id);
        Task<(IEnumerable<Course> Ongoing, IEnumerable<Course> Completed)> GetDriverDashboardAsync(string driverId);
        Task<(byte[] DocumentBytes, string FileName)> GetDriverCourseDocumentAsync(string courseId, string driverId);
        Task<AssignCourseViewModel> GetAssignCourseViewModelAsync(string shipperId);
        Task<(bool Success, string ErrorMessage, AssignCourseViewModel Model)> AssignCourseFromViewModelAsync(AssignCourseViewModel model, IFormFile document);
        Task<(IEnumerable<Course> Ongoing, IEnumerable<Course> Completed, List<User> Users, object Filters)> GetShipperCoursesViewModelAsync(string userId, string startingPoint, string destination, string description, string status, string shipper, string driver);
        Task<(byte[] DocumentBytes, string FileName)> GetShipperCourseDocumentAsync(string courseId, string userId);
    }
} 