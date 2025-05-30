using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using LogisticsSystem.Models;
using LogisticsSystem.Services.Contracts;

namespace LogisticsSystem.Controllers
{
    public class AuthController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IAuthService _authService;

        public AuthController(IConfiguration configuration, IAuthService authService)
        {
            _configuration = configuration;
            _authService = authService;
        }

        // GET: /Auth/Login
        [HttpGet]
        public IActionResult Login()
        {
            // Return the login view with an empty model
            return View(new LoginViewModel());
        }

        // POST: /Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // Model binder binds data from form-url-encoded
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (success, error, user) = await _authService.ValidateUserCredentialsAsync(model.Email, model.Password);
            if (!success)
            {
                ModelState.AddModelError("", error);
                return View(model);
            }

            var tokenString = _authService.GenerateJwtToken(user);

            // Set the "AuthToken" cookie
            Response.Cookies.Append("AuthToken", tokenString, new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // set to false for HTTP testing (set to true for HTTPS)
                Expires = DateTimeOffset.UtcNow.AddMinutes(30),
                Path = "/"
            });

            // Redirect the user to the home page
            return RedirectToAction("Index", "Home");
        }

        // GET: /Auth/Logout
        [HttpGet]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("AuthToken");
            return RedirectToAction("Login", "Auth");
        }
    }
}
