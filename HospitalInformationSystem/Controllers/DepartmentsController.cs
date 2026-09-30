using HospitalInformationSystem.Data;
using HospitalInformationSystem.Models.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalInformationSystem.Models.Entities;
using HospitalInformationSystem.Models.ViewModels;

namespace HospitalInformationSystem.Controllers;

[Authorize(Roles = UserRoles.Admin)]
public class DepartmentsController : Controller
{
	private readonly HospitalDbContext _context;

	public DepartmentsController(HospitalDbContext context)
	{
		_context = context;
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

		var department = new Department
		{
			Name = model.Name,
			Description = model.Description,
			IsActive = true
		};

		_context.Departments.Add(department);

		await _context.SaveChangesAsync();

		return RedirectToAction(nameof(Index));
	}
}