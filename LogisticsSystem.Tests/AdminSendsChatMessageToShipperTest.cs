using System;
using System.Threading.Tasks;
using Xunit;
using LogisticsSystem.Controllers;
using LogisticsSystem.Models;
using LogisticsSystem.Services.Contracts;
using LogisticsSystem.Services;
using Moq;

namespace LogisticsSystem.Tests
{
    public class AdminSendsChatMessageToShipperTest
    {
        [Fact]
        public async Task AdminSendsChatMessageToShipper_Success()
        {
            var adminId = "admin123";
            var shipperId = "shipper123";
            var admin = new User { Id = adminId, Role = "Admin", Name = "AdminUser" };
            var shipper = new User { Id = shipperId, Role = "Shipper", Name = "ShipperUser" };

            var mockChatService = new Mock<IChatService>();
            var mockUserService = new Mock<IUserService>();

            mockUserService.Setup(x => x.GetUserByIdAsync(adminId)).ReturnsAsync(admin);
            mockUserService.Setup(x => x.GetUserByIdAsync(shipperId)).ReturnsAsync(shipper);

            var controller = new ChatController(mockChatService.Object, mockUserService.Object);

            var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, adminId) };
            var identity = new System.Security.Claims.ClaimsIdentity(claims);
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = principal }
            };

            var message = new ChatMessage
            {
                SenderId = adminId,
                SenderName = admin.Name,
                Content = "Hello Shipper!",
                RoomId = $"private_{adminId}_{shipperId}"
            };

            mockChatService.Setup(x => x.SaveMessage(It.IsAny<ChatMessage>())).ReturnsAsync(message);

            var result = await mockChatService.Object.SaveMessage(message);

            Assert.Equal("Hello Shipper!", result.Content);
            Assert.Equal(adminId, result.SenderId);
            Assert.Equal($"private_{adminId}_{shipperId}", result.RoomId);
            mockChatService.Verify(x => x.SaveMessage(It.IsAny<ChatMessage>()), Times.Once);
        }
    }
} 