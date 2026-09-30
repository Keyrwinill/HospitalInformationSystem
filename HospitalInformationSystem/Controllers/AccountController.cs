using System.Security.Claims;
using HospitalInformationSystem.Models.ViewModels;
using HospitalInformationSystem.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalInformationSystem.Controllers;

public class AccountController : Controller
{
	private readonly AuthService _authService;

	public AccountController(AuthService authService)
	{
		_authService = authService;
	}

	[HttpGet]
	[AllowAnonymous]
	public IActionResult Login()
	{
		return View();
	}

	[HttpPost]
	[AllowAnonymous]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Login(LoginViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var user = await _authService.ValidateUserAsync(
			model.Account,
			model.Password);

		if (user == null)
		{
			ModelState.AddModelError(
				string.Empty,
				"Invalid account or password.");

			return View(model);
		}

		var claims = new List<Claim>
		{
			new(ClaimTypes.NameIdentifier, user.Id.ToString()),
			new(ClaimTypes.Name, user.Account),
			new(ClaimTypes.Role, user.Role)
		};

		var identity = new ClaimsIdentity(
			claims,
			CookieAuthenticationDefaults.AuthenticationScheme);

		var principal = new ClaimsPrincipal(identity);

		var authenticationProperties = new AuthenticationProperties
		{
			IsPersistent = model.RememberMe
		};

		await HttpContext.SignInAsync(
			CookieAuthenticationDefaults.AuthenticationScheme,
			principal,
			authenticationProperties);

		return RedirectToAction("Index", "Home");
	}

	[HttpPost]
	[Authorize]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Logout()
	{
		await HttpContext.SignOutAsync(
			CookieAuthenticationDefaults.AuthenticationScheme);

		return RedirectToAction("Login", "Account");
	}

	[HttpGet]
	[AllowAnonymous]
	public IActionResult AccessDenied()
	{
		return View();
	}
}