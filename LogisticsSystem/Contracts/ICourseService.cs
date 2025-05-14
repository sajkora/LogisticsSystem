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
    }
} 