using HospitalInformationSystem.Data;
using HospitalInformationSystem.Models.Constants;
using HospitalInformationSystem.Models.Entities;
using HospitalInformationSystem.Models.ViewModels;
using HospitalInformationSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HospitalInformationSystem.Controllers;

[Authorize(Roles = UserRoles.Admin)]
public class DoctorsController : Controller
{
	private readonly HospitalDbContext _context;
	private readonly PasswordService _passwordService;
	private readonly IAuditService _auditService;	
	private readonly IDoctorService _doctorService;

	public DoctorsController(
		HospitalDbContext context,
		PasswordService passwordService,
		IAuditService auditService,
		IDoctorService doctorService)
	{
		_context = context;
		_passwordService = passwordService;
		_auditService = auditService;
		_doctorService = doctorService;
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

		await using var transaction =
			await _context.Database.BeginTransactionAsync();

		var doctor = new Doctor
		{
			User = user,
			DepartmentId = model.DepartmentId!.Value,
			LicenseNumber = model.LicenseNumber,
			IsActive = true
		};

		_context.Doctors.Add(doctor);

		// Creates both User and Doctor.
		// SQL Server generates doctor.Id.
		await _context.SaveChangesAsync();

		var currentUserId = Guid.Parse(
			User.FindFirstValue(ClaimTypes.NameIdentifier)!);

		await _auditService.LogAsync(
			currentUserId,
			"CreateDoctor",
			"Doctor",
			doctor.Id.ToString());

		// Save the staged AuditLog.
		await _context.SaveChangesAsync();

		await transaction.CommitAsync();

		return RedirectToAction(nameof(Index));
	}

	[HttpGet]
	public async Task<IActionResult> Details(int id)
	{
		var doctor = await _context.Doctors
			.AsNoTracking()
			.Include(x => x.User)
			.Include(x => x.Department)
			.FirstOrDefaultAsync(x => x.Id == id);

		if (doctor == null)
		{
			return NotFound();
		}

		return View(doctor);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Deactivate(int id)
	{
		var currentUserId = Guid.Parse(
			User.FindFirstValue(ClaimTypes.NameIdentifier)!);

		var result = await _doctorService.DeactivateAsync(
			id,
			currentUserId);

		if (result == DoctorOperationResult.NotFound)
		{
			return NotFound();
		}

		if (result == DoctorOperationResult.Inactive)
		{
			return BadRequest();
		}

		if (result == DoctorOperationResult.HasFutureScheduledAppointment)
		{
			TempData["ErrorMessage"] =
				"The doctor cannot be deactivated while they have a future scheduled appointment.";

			return RedirectToAction(nameof(Index));
		}

		if (result == DoctorOperationResult.HasActiveVisit)
		{
			TempData["ErrorMessage"] =
				"The doctor cannot be deactivated while they have an active visit.";

			return RedirectToAction(nameof(Index));
		}

		return RedirectToAction(nameof(Index));
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Activate(int id)
	{
		var currentUserId = Guid.Parse(
			User.FindFirstValue(ClaimTypes.NameIdentifier)!);

		var result = await _doctorService.ActivateAsync(
			id,
			currentUserId);

		if (result == DoctorOperationResult.NotFound)
		{
			return NotFound();
		}

		if (result == DoctorOperationResult.AlreadyActive)
		{
			return BadRequest();
		}

		if (result == DoctorOperationResult.InactiveDepartment)
		{
			TempData["ErrorMessage"] =
				"The doctor cannot be activated while their department is inactive.";

			return RedirectToAction(nameof(Index));
		}

		return RedirectToAction(nameof(Index));
	}

	[HttpGet]
	public async Task<IActionResult> Edit(int id)
	{
		var doctor = await _context.Doctors
			.AsNoTracking()
			.Include(x => x.User)
			.FirstOrDefaultAsync(x => x.Id == id);

		if (doctor == null)
		{
			return NotFound();
		}

		if (!doctor.IsActive || !doctor.User.IsActive)
		{
			TempData["ErrorMessage"] =
				"An inactive doctor cannot be edited. Reactivate the doctor first.";

			return RedirectToAction(nameof(Index));
		}

		var model = new EditDoctorViewModel
		{
			Id = doctor.Id,
			FirstName = doctor.User.FirstName,
			LastName = doctor.User.LastName,
			Email = doctor.User.Email,
			LicenseNumber = doctor.LicenseNumber,
			DepartmentId = doctor.DepartmentId
		};

		await LoadDepartmentsAsync();

		return View(model);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Edit(EditDoctorViewModel model)
	{
		if (!ModelState.IsValid)
		{
			await LoadDepartmentsAsync();
			return View(model);
		}

		var currentUserId = Guid.Parse(
			User.FindFirstValue(ClaimTypes.NameIdentifier)!);

		var result = await _doctorService.UpdateAsync(
			model.Id,
			model.FirstName,
			model.LastName,
			model.Email,
			model.LicenseNumber,
			model.DepartmentId!.Value,
			currentUserId);

		if (result == DoctorOperationResult.NotFound)
		{
			return NotFound();
		}

		if (result == DoctorOperationResult.Inactive)
		{
			TempData["ErrorMessage"] =
				"An inactive doctor cannot be edited. Reactivate the doctor first.";

			return RedirectToAction(nameof(Index));
		}

		if (result == DoctorOperationResult.InactiveDepartment)
		{
			ModelState.AddModelError(
				nameof(model.DepartmentId),
				"Please select an active department.");

			await LoadDepartmentsAsync();
			return View(model);
		}

		if (result == DoctorOperationResult.DuplicateEmail)
		{
			ModelState.AddModelError(
				nameof(model.Email),
				"Email already exists.");

			await LoadDepartmentsAsync();
			return View(model);
		}

		if (result == DoctorOperationResult.DuplicateLicenseNumber)
		{
			ModelState.AddModelError(
				nameof(model.LicenseNumber),
				"License number already exists.");

			await LoadDepartmentsAsync();
			return View(model);
		}

		return RedirectToAction(
			nameof(Details),
			new { id = model.Id });
	}
}