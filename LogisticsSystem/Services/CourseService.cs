using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using LogisticsSystem.Models;
using LogisticsSystem.Services.Contracts;
using Microsoft.AspNetCore.Http;

namespace LogisticsSystem.Services
{
    public class CourseService : ICourseService
    {
        private readonly IMongoCollection<Course> _courses;
        private readonly IFileService _fileService;

        public CourseService(IConfiguration configuration, IFileService fileService)
        {
            var client = new MongoClient(configuration["MongoDB:ConnectionString"]);
            var database = client.GetDatabase(configuration["MongoDB:DatabaseName"]);
            _courses = database.GetCollection<Course>("Courses");
            _fileService = fileService;
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
    }
}
