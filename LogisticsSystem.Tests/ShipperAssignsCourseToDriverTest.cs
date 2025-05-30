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
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace LogisticsSystem.Tests
{
    public class ShipperAssignsCourseToDriverTest
    {
        [Fact]
        public async Task ShipperAssignsCourseToDriver_Success()
        {
            var shipperId = "shipper123";
            var driverId = "driver123";
            var shipper = new User { Id = shipperId, Role = "Shipper", Name = "Shipper1", Email = "shipper1@example.com" };
            var driver = new User { Id = driverId, Role = "Driver", Name = "Driver1", Email = "driver1@example.com" };

            var mockUserService = new Mock<IUserService>();
            var mockCourseService = new Mock<ICourseService>();
            var mockCourseEventService = new Mock<ICourseEventService>();

            mockUserService.Setup(x => x.GetUserByIdAsync(shipperId)).ReturnsAsync(shipper);
            mockUserService.Setup(x => x.GetUserByIdAsync(driverId)).ReturnsAsync(driver);
            mockUserService.Setup(x => x.GetAllUsersAsync()).ReturnsAsync(new List<User> { shipper, driver });

            var controller = new ShipperController(
                mockCourseService.Object,
                mockUserService.Object,
                null,
                mockCourseEventService.Object
            );

            var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, shipperId) };
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

            var model = new AssignCourseViewModel
            {
                DriverId = driverId,
                ShipperId = shipperId,
                StartingPoint = "New York",
                Destination = "Los Angeles",
                Description = "Test delivery",
                Drivers = new List<User> { driver },
                Document = null
            };

            controller.ModelState.Clear();

            var result = await controller.AssignCourse(model, null);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(controller.TempData.ContainsKey("SuccessMessage"));
            Assert.Equal("Course assigned successfully.", controller.TempData["SuccessMessage"]);
            mockCourseService.Verify(x => x.AssignCourseAsync(It.IsAny<Course>(), null), Times.Once);
        }
    }
} 