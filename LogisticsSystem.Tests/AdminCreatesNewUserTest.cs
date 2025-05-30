using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using LogisticsSystem.Controllers;
using LogisticsSystem.Models;
using LogisticsSystem.Services;
using LogisticsSystem.Services.Contracts;
using Microsoft.AspNetCore.Http;
using Moq;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace LogisticsSystem.Tests
{
    public class AdminCreatesNewUserTest
    {
        [Fact]
        public async Task AdminCreatesNewUser_Success()
        {
            var adminId = "admin123";
            var admin = new User { Id = adminId, Role = "Admin" };

            var mockUserService = new Mock<IUserService>();
            var mockCourseService = new Mock<ICourseService>();
            var mockFileService = new Mock<IFileService>();

            mockUserService.Setup(x => x.GetUserByIdAsync(adminId)).ReturnsAsync(admin);
            mockUserService.Setup(x => x.CreateUserFromViewModelAsync(It.IsAny<RegisterViewModel>())).ReturnsAsync((true, null));

            var controller = new AdminController(
                mockUserService.Object,
                mockCourseService.Object,
                mockFileService.Object
            );

            var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, adminId) };
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

            var model = new RegisterViewModel
            {
                Name = "Test User",
                Email = "test@example.com",
                Password = "Test1234!",
                ConfirmPassword = "Test1234!",
                Role = "Driver"
            };

            var result = await controller.CreateUser(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ManageUsers", redirectResult.ActionName);
            mockUserService.Verify(x => x.CreateUserFromViewModelAsync(It.IsAny<RegisterViewModel>()), Times.Once);
        }
    }
} 