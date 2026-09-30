using HospitalInformationSystem.Data;
using HospitalInformationSystem.Models.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalInformationSystem.Models.Entities;
using HospitalInformationSystem.Models.ViewModels;
using HospitalInformationSystem.Services;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HospitalInformationSystem.Controllers;

[Authorize(Roles = UserRoles.Admin)]
public class DoctorsController : Controller
{
	private readonly HospitalDbContext _context;
	private readonly PasswordService _passwordService;

	public DoctorsController(
		HospitalDbContext context,
		PasswordService passwordService)
	{
		_context = context;
		_passwordService = passwordService;
	}

	public async Task<IActionResult> Index()
	{
		var doctors = await _context.Doctors
			.AsNoTracking()
			.Include(x => x.User)
			.Include(x => x.Department)
			.OrderBy(x => x.User.LastName)
			.ThenBy(x => x.User.FirstName)
			.ToListAsync();

		return View(doctors);
	}

	private async Task LoadDepartmentsAsync()
	{
		var departments = await _context.Departments
			.AsNoTracking()
			.Where(x => x.IsActive)
			.OrderBy(x => x.Name)
			.ToListAsync();

		ViewBag.Departments = new SelectList(
			departments,
			"Id",
			"Name");
	}

	[HttpGet]
	public async Task<IActionResult> Create()
	{
		await LoadDepartmentsAsync();

		return View();
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(CreateDoctorViewModel model)
	{
		if (!ModelState.IsValid)
		{
			await LoadDepartmentsAsync();
			return View(model);
		}

		var accountExists = await _context.Users
			.AnyAsync(x => x.Account == model.Account);

		if (accountExists)
		{
			ModelState.AddModelError(
				nameof(model.Account),
				"Account already exists.");

			await LoadDepartmentsAsync();
			return View(model);
		}

		var emailExists = await _context.Users
			.AnyAsync(x => x.Email == model.Email);

		if (emailExists)
		{
			ModelState.AddModelError(
				nameof(model.Email),
				"Email already exists.");

			await LoadDepartmentsAsync();
			return View(model);
		}

		var licenseExists = await _context.Doctors
			.AnyAsync(x => x.LicenseNumber == model.LicenseNumber);

		if (licenseExists)
		{
			ModelState.AddModelError(
				nameof(model.LicenseNumber),
				"License number already exists.");

			await LoadDepartmentsAsync();
			return View(model);
		}

		var departmentExists = await _context.Departments
			.AnyAsync(x =>
				x.Id == model.DepartmentId &&
				x.IsActive);

		if (!departmentExists)
		{
			ModelState.AddModelError(
				nameof(model.DepartmentId),
				"Please select a valid department.");

			await LoadDepartmentsAsync();
			return View(model);
		}

		var user = new User
		{
			Id = Guid.NewGuid(),
			Account = model.Account,
			Email = model.Email,
			FirstName = model.FirstName,
			LastName = model.LastName,
			Role = UserRoles.Doctor,
			IsActive = true
		};

		user.PasswordHash =
			_passwordService.HashPassword(user, model.Password);

		var doctor = new Doctor
		{
			User = user,
			DepartmentId = model.DepartmentId!.Value,
			LicenseNumber = model.LicenseNumber,
			IsActive = true
		};

		_context.Doctors.Add(doctor);

		await _context.SaveChangesAsync();

		return RedirectToAction(nameof(Index));
	}
}