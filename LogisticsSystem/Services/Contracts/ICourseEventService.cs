using System.Collections.Generic;
using System.Threading.Tasks;
using LogisticsSystem.Models;

namespace LogisticsSystem.Services.Contracts
{
    public interface ICourseEventService
    {
        Task AddEventAsync(CourseEvent courseEvent);
        Task<List<CourseEvent>> GetEventsByCourseIdAsync(string courseId);
        Task<List<CourseEvent>> GetEventsByDriverIdAsync(string driverId);
    }
} 