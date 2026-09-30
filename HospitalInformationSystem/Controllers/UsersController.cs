using HospitalInformationSystem.Data;
using HospitalInformationSystem.Models.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalInformationSystem.Models.Entities;
using HospitalInformationSystem.Models.ViewModels;
using HospitalInformationSystem.Services;

namespace HospitalInformationSystem.Controllers;

[Authorize(Roles = UserRoles.Admin)]
public class UsersController : Controller
{
	private readonly HospitalDbContext _context;
	private readonly PasswordService _passwordService;

	public UsersController(
		HospitalDbContext context,
		PasswordService passwordService)
	{
		_context = context;
		_passwordService = passwordService;
	}

	public async Task<IActionResult> Index()
	{
		var users = await _context.Users
			.AsNoTracking()
			.OrderBy(x => x.Account)
			.ToListAsync();

		return View(users);
	}

	[HttpGet]
	public IActionResult Create()
	{
		return View();
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(CreateUserViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var allowedRoles = new[]
		{
			UserRoles.Admin,
			UserRoles.Receptionist
		};

		if (!allowedRoles.Contains(model.Role))
		{
			ModelState.AddModelError(
				nameof(model.Role),
				"Invalid role.");

			return View(model);
		}

		var accountExists = await _context.Users
			.AnyAsync(x => x.Account == model.Account);

		if (accountExists)
		{
			ModelState.AddModelError(
				nameof(model.Account),
				"Account already exists.");

			return View(model);
		}

		var emailExists = await _context.Users
			.AnyAsync(x => x.Email == model.Email);

		if (emailExists)
		{
			ModelState.AddModelError(
				nameof(model.Email),
				"Email already exists.");

			return View(model);
		}

		var user = new User
		{
			Id = Guid.NewGuid(),
			Account = model.Account,
			Email = model.Email,
			FirstName = model.FirstName,
			LastName = model.LastName,
			Role = model.Role,
			IsActive = true
		};

		user.PasswordHash =
			_passwordService.HashPassword(user, model.Password);

		_context.Users.Add(user);

		await _context.SaveChangesAsync();

		return RedirectToAction(nameof(Index));
	}
}