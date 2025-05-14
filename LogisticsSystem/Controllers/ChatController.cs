using Microsoft.AspNetCore.Mvc;
using LogisticsSystem.Services;
using LogisticsSystem.Models;
using System.Linq;
using System.Threading.Tasks;
using LogisticsSystem.Services.Contracts;

namespace LogisticsSystem.Controllers
{
    public class ChatController : Controller
    {
        private readonly IChatService _chatService;
        private readonly IUserService _userService;

        public ChatController(IChatService chatService, IUserService userService)
        {
            _chatService = chatService;
            _userService = userService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var user = await _userService.GetUserByIdAsync(userId);
            var displayName = user?.Name ?? "Anonymous";

            // Fetch all users for search
            var allUsers = await _userService.GetAllUsersAsync();
            ViewBag.AllUsers = allUsers.Where(u => u.Id != userId).ToList();

            // Fetch recent chat users (those the current user has messaged or been messaged by)
            var messages = (await _chatService.GetUserMessages(userId, 1000))
                .Concat(await _chatService.GetRoomMessages(userId, 1000)) // in case private rooms use userId as roomId
                .ToList();
            var recentUserIds = messages
                .Select(m => m.SenderId == userId ? m.SenderId : m.SenderId)
                .Concat(messages.Select(m => m.SenderId != userId ? m.SenderId : m.SenderId))
                .Concat(messages.Select(m => m.RoomId.Replace($"private_{userId}_", "").Replace($"private_", "").Replace(userId, "").Replace("_", "")))
                .Where(id => !string.IsNullOrEmpty(id) && id != userId)
                .Distinct()
                .ToList();
            var recentUsers = allUsers.Where(u => recentUserIds.Contains(u.Id)).ToList();
            ViewBag.RecentUsers = recentUsers;

            ViewBag.UserName = displayName;
            ViewBag.UserId = userId;
            return View();
        }

        [HttpGet("api/room/{roomId}")]
        public async Task<ActionResult<List<ChatMessage>>> GetRoomMessages(string roomId, [FromQuery] int limit = 50)
        {
            var messages = await _chatService.GetRoomMessages(roomId, limit);
            return Ok(messages);
        }

        [HttpGet("api/user/{userId}")]
        public async Task<ActionResult<List<ChatMessage>>> GetUserMessages(string userId, [FromQuery] int limit = 50)
        {
            var messages = await _chatService.GetUserMessages(userId, limit);
            return Ok(messages);
        }
    }
} 