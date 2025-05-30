using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using LogisticsSystem.Controllers;
using LogisticsSystem.Models;
using LogisticsSystem.Services.Contracts;
using Microsoft.AspNetCore.Http;
using Moq;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace LogisticsSystem.Tests
{
    public class DriverReportsEventTest
    {
        [Fact]
        public async Task DriverReportsEvent_Success()
        {
            var driverId = "driver123";
            var courseId = "course123";
            var driver = new User { Id = driverId, Role = "Driver", Name = "Driver1", Email = "driver1@example.com" };
            var course = new Course { Id = courseId, DriverId = driverId, StartingPoint = "A", Destination = "B", Description = "desc", ShipperId = "shipper123", IsCompleted = false };

            var mockUserService = new Mock<IUserService>();
            var mockCourseService = new Mock<ICourseService>();
            var mockCourseEventService = new Mock<ICourseEventService>();

            mockUserService.Setup(x => x.GetUserByIdAsync(driverId)).ReturnsAsync(driver);
            mockCourseService.Setup(x => x.GetCourseByIdAsync(courseId)).ReturnsAsync(course);

            var controller = new DriverController(
                mockCourseService.Object,
                mockCourseEventService.Object,
                mockUserService.Object
            );

            var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, driverId) };
            var identity = new System.Security.Claims.ClaimsIdentity(claims);
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
            controller.TempData = new TempDataDictionary(
                controller.ControllerContext.HttpContext,
                Mock.Of<ITempDataProvider>()
            );

            var model = new CourseEvent
            {
                CourseId = courseId,
                DriverId = driverId,
                Description = "Test event",
                Timestamp = DateTime.UtcNow
            };

            mockCourseEventService.Setup(x => x.ReportEventAsync(It.IsAny<CourseEvent>(), driverId)).ReturnsAsync((true, null));
            var result = await controller.ReportEvent(model);
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyCourses", redirectResult.ActionName);
            mockCourseEventService.Verify(x => x.ReportEventAsync(It.IsAny<CourseEvent>(), driverId), Times.Once);
        }
    }
} 