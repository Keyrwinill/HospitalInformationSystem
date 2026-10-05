using HospitalInformationSystem.Data;
using HospitalInformationSystem.Models.Constants;
using HospitalInformationSystem.Models.Entities;
using HospitalInformationSystem.Models.ViewModels;
using HospitalInformationSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HospitalInformationSystem.Controllers;

[Authorize(Roles = UserRoles.Admin)]
public class UsersController : Controller
{
	private readonly HospitalDbContext _context;
	private readonly IUserService _userService;

	public UsersController(
		HospitalDbContext context,
		IUserService userService)
	{
		_context = context;
		_userService = userService;
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

		var currentUserId = Guid.Parse(
			User.FindFirstValue(ClaimTypes.NameIdentifier)!);

		var result = await _userService.CreateAsync(
			model.Account,
			model.Email,
			model.FirstName,
			model.LastName,
			model.Role,
			model.Password,
			currentUserId);

		if (result == UserOperationResult.InvalidRole)
		{
			ModelState.AddModelError(
				nameof(model.Role),
				"Invalid role.");

			return View(model);
		}

		if (result == UserOperationResult.DuplicateAccount)
		{
			ModelState.AddModelError(
				nameof(model.Account),
				"Account already exists.");

			return View(model);
		}

		if (result == UserOperationResult.DuplicateEmail)
		{
			ModelState.AddModelError(
				nameof(model.Email),
				"Email already exists.");

			return View(model);
		}

		return RedirectToAction(nameof(Index));
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Deactivate(Guid id)
	{
		var currentUserId = Guid.Parse(
			User.FindFirstValue(ClaimTypes.NameIdentifier)!);

		var result = await _userService.DeactivateAsync(
			id,
			currentUserId);

		if (result == UserOperationResult.NotFound)
		{
			return NotFound();
		}

		if (result == UserOperationResult.Inactive ||
			result == UserOperationResult.DoctorManagedSeparately)
		{
			return BadRequest();
		}

		if (result == UserOperationResult.CannotDeactivateSelf)
		{
			TempData["ErrorMessage"] =
				"You cannot deactivate your own account.";

			return RedirectToAction(nameof(Index));
		}

		return RedirectToAction(nameof(Index));
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Activate(Guid id)
	{
		var currentUserId = Guid.Parse(
			User.FindFirstValue(ClaimTypes.NameIdentifier)!);

		var result = await _userService.ActivateAsync(
			id,
			currentUserId);

		if (result == UserOperationResult.NotFound)
		{
			return NotFound();
		}

		if (result == UserOperationResult.AlreadyActive ||
			result == UserOperationResult.DoctorManagedSeparately)
		{
			return BadRequest();
		}

		return RedirectToAction(nameof(Index));
	}
}