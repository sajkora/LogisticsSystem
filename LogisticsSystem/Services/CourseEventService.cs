using System.Collections.Generic;
using System.Threading.Tasks;
using LogisticsSystem.Models;
using LogisticsSystem.Services.Contracts;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;

namespace LogisticsSystem.Services
{
    public class CourseEventService : ICourseEventService
    {
        private readonly IMongoCollection<CourseEvent> _courseEvents;

        public CourseEventService(IConfiguration configuration)
        {
            var client = new MongoClient(configuration["MongoDB:ConnectionString"]);
            var dbName = configuration["MongoDB:DatabaseName"];
            var database = client.GetDatabase(dbName);
            _courseEvents = database.GetCollection<CourseEvent>("CourseEvents");
        }

        public async Task AddEventAsync(CourseEvent courseEvent)
        {
            await _courseEvents.InsertOneAsync(courseEvent);
        }

        public async Task<List<CourseEvent>> GetEventsByCourseIdAsync(string courseId)
        {
            return await _courseEvents.Find(e => e.CourseId == courseId).SortByDescending(e => e.Timestamp).ToListAsync();
        }

        public async Task<List<CourseEvent>> GetEventsByDriverIdAsync(string driverId)
        {
            return await _courseEvents.Find(e => e.DriverId == driverId).SortByDescending(e => e.Timestamp).ToListAsync();
        }

        public async Task<(bool Success, string ErrorMessage)> ReportEventAsync(CourseEvent model, string driverId)
        {
            if (model.DriverId != driverId)
            {
                return (false, "Forbidden");
            }
            await AddEventAsync(model);
            return (true, null);
        }
    }
} 