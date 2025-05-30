using LogisticsSystem.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using LogisticsSystem.Services.Contracts;

namespace LogisticsSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUserService _userService;

        public HomeController(ILogger<HomeController> logger, IUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        public async Task<IActionResult> Index()
        {
            var (name, email, id) = await _userService.GetUserDisplayInfoAsync(User);
            ViewBag.UserName = name;
            ViewBag.UserEmail = email;
            ViewBag.UserId = id;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
