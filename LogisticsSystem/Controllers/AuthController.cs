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
        private readonly IUserService _userService;

        public AuthController(IConfiguration configuration, IUserService userService)
        {
            _configuration = configuration;
            _userService = userService;
        }

        // GET: /Auth/Login
        [HttpGet]
        public IActionResult Login()
        {
            // Zwracamy widok logowania, przekazując pusty model
            return View(new LoginViewModel());
        }

        // POST: /Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // Model binder wiąże dane z form-url-encoded
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userService.GetUserByEmailAsync(model.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Niepoprawne dane logowania.");
                return View(model);
            }

            // Tworzymy claimy dla tokena
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role)

            };

            // Tworzymy token JWT
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // Ustawiamy ciasteczko "AuthToken"
            Response.Cookies.Append("AuthToken", tokenString, new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // ustaw na false, jeśli testujesz na HTTP (dla HTTPS ustaw true)
                Expires = DateTimeOffset.UtcNow.AddMinutes(30),
                Path = "/"
            });

            // Przekierowujemy użytkownika do strony głównej
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
