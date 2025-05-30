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
            ViewBag.UserName = await _chatService.GetDisplayName(userId);
            ViewBag.UserId = userId;
            ViewBag.AllUsers = (await _userService.GetAllUsersAsync()).Where(u => u.Id != userId).ToList();
            ViewBag.RecentUsers = await _chatService.GetRecentChatUsers(userId);
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