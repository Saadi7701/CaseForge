using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CaseForgeAI.Services;

namespace CaseForgeAI.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthenticationService _authService;

        public AccountController(AuthenticationService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterInputModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _authService.RegisterAsync(model.Email, model.Password, model.FullName);
            if (result.Succeeded)
            {
                // Auto login on successful registration
                await _authService.LoginAsync(model.Email, model.Password, rememberMe: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginInputModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _authService.LoginAsync(model.Email, model.Password, model.RememberMe);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid login credentials.");
            return View(model);
        }

        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
            return RedirectToAction("Index", "Home");
        }
    }

    public class RegisterInputModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.EmailAddress]
        public string Email { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        public string FullName { get; set; } = string.Empty;
    }

    public class LoginInputModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.EmailAddress]
        public string Email { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;
    }
}
