using HospitalInformationSystem.Data;
using HospitalInformationSystem.Models.Constants;
using HospitalInformationSystem.Models.Entities;
using HospitalInformationSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalInformationSystem.Controllers;

[Authorize(Roles = UserRoles.Admin)]
public class MedicationsController : Controller
{
	private readonly HospitalDbContext _context;

	public MedicationsController(HospitalDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<IActionResult> Index()
	{
		var medications = await _context.Medications
			.AsNoTracking()
			.OrderBy(x => x.Name)
			.ToListAsync();

		return View(medications);
	}

	[HttpGet]
	public IActionResult Create()
	{
		return View();
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(
	CreateMedicationViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var codeExists = await _context.Medications
			.AnyAsync(x => x.Code == model.Code);

		if (codeExists)
		{
			ModelState.AddModelError(
				nameof(model.Code),
				"Medication code already exists.");

			return View(model);
		}

		var medication = new Medication
		{
			Code = model.Code,
			Name = model.Name,
			Unit = model.Unit
		};

		_context.Medications.Add(medication);
		await _context.SaveChangesAsync();

		return RedirectToAction(nameof(Index));
	}
}