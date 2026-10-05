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
public class DepartmentsController : Controller
{
	private readonly HospitalDbContext _context;
	private readonly IAuditService _auditService;
	private readonly IDepartmentService _departmentService;

	public DepartmentsController(HospitalDbContext context, IAuditService auditService, IDepartmentService departmentService)
	{
		_context = context;
		_auditService = auditService;
		_departmentService = departmentService;
	}

	public async Task<IActionResult> Index()
	{
		var departments = await _context.Departments
			.AsNoTracking()
			.OrderBy(x => x.Name)
			.ToListAsync();

		return View(departments);
	}

	[HttpGet]
	public IActionResult Create()
	{
		return View();
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(
		CreateDepartmentViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var nameExists = await _context.Departments
			.AnyAsync(x => x.Name == model.Name);

		if (nameExists)
		{
			ModelState.AddModelError(
				nameof(model.Name),
				"Department name already exists.");

			return View(model);
		}

		await using var transaction =
			await _context.Database.BeginTransactionAsync();

		var department = new Department
		{
			Name = model.Name,
			Description = model.Description,
			IsActive = true
		};

		_context.Departments.Add(department);

		// Insert Department and generate department.Id.
		await _context.SaveChangesAsync();

		var currentUserId = Guid.Parse(
			User.FindFirstValue(ClaimTypes.NameIdentifier)!);

		await _auditService.LogAsync(
			currentUserId,
			"CreateDepartment",
			"Department",
			department.Id.ToString());

		// Save the staged AuditLog.
		await _context.SaveChangesAsync();

		await transaction.CommitAsync();

		return RedirectToAction(nameof(Index));
	}

	[HttpGet]
	public async Task<IActionResult> Details(int id)
	{
		var department = await _context.Departments
			.AsNoTracking()
			.Include(x => x.Doctors)
				.ThenInclude(x => x.User)
			.FirstOrDefaultAsync(x => x.Id == id);

		if (department == null)
		{
			return NotFound();
		}

		return View(department);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Deactivate(int id)
	{
		var currentUserId = Guid.Parse(
			User.FindFirstValue(ClaimTypes.NameIdentifier)!);

		var result = await _departmentService.DeactivateAsync(
			id,
			currentUserId);

		if (result == DepartmentOperationResult.NotFound)
		{
			return NotFound();
		}

		if (result == DepartmentOperationResult.Inactive)
		{
			return BadRequest();
		}

		if (result == DepartmentOperationResult.HasActiveDoctors)
		{
			TempData["ErrorMessage"] =
				"The department cannot be deactivated while it has active doctors.";

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

		var result = await _departmentService.ActivateAsync(
			id,
			currentUserId);

		if (result == DepartmentOperationResult.NotFound)
		{
			return NotFound();
		}

		if (result == DepartmentOperationResult.AlreadyActive)
		{
			return BadRequest();
		}

		return RedirectToAction(nameof(Index));
	}

	[HttpGet]
	public async Task<IActionResult> Edit(int id)
	{
		var department = await _context.Departments
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == id);

		if (department == null)
		{
			return NotFound();
		}

		if (!department.IsActive)
		{
			TempData["ErrorMessage"] =
				"An inactive department cannot be edited. Reactivate it first.";

			return RedirectToAction(nameof(Index));
		}

		var model = new EditDepartmentViewModel
		{
			Id = department.Id,
			Name = department.Name,
			Description = department.Description
		};

		return View(model);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Edit(EditDepartmentViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var currentUserId = Guid.Parse(
			User.FindFirstValue(ClaimTypes.NameIdentifier)!);

		var result = await _departmentService.UpdateAsync(
			model.Id,
			model.Name,
			model.Description,
			currentUserId);

		if (result == DepartmentOperationResult.NotFound)
		{
			return NotFound();
		}

		if (result == DepartmentOperationResult.Inactive)
		{
			TempData["ErrorMessage"] =
				"An inactive department cannot be edited. Reactivate it first.";

			return RedirectToAction(nameof(Index));
		}

		if (result == DepartmentOperationResult.DuplicateName)
		{
			ModelState.AddModelError(
				nameof(model.Name),
				"Department name already exists.");

			return View(model);
		}

		return RedirectToAction(
			nameof(Details),
			new { id = model.Id });
	}
}