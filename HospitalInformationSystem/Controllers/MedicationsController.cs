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
public class MedicationsController : Controller
{
	private readonly HospitalDbContext _context;
	private readonly IAuditService _auditService;
	private readonly IMedicationService _medicationService;

	public MedicationsController(HospitalDbContext context, IAuditService auditService, IMedicationService medicationService)
	{
		_context = context;
		_auditService = auditService;
		_medicationService = medicationService;
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

		await using var transaction =
			await _context.Database.BeginTransactionAsync();

		var medication = new Medication
		{
			Code = model.Code,
			Name = model.Name,
			Unit = model.Unit
		};

		_context.Medications.Add(medication);

		// Insert Medication and generate medication.Id.
		await _context.SaveChangesAsync();

		var currentUserId = Guid.Parse(
			User.FindFirstValue(ClaimTypes.NameIdentifier)!);

		await _auditService.LogAsync(
			currentUserId,
			"CreateMedication",
			"Medication",
			medication.Id.ToString());

		// Save the staged AuditLog.
		await _context.SaveChangesAsync();

		await transaction.CommitAsync();

		return RedirectToAction(nameof(Index));
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Deactivate(int id)
	{
		var currentUserId = Guid.Parse(
			User.FindFirstValue(ClaimTypes.NameIdentifier)!);

		var result = await _medicationService.DeactivateAsync(
			id,
			currentUserId);

		if (result == MedicationOperationResult.NotFound)
		{
			return NotFound();
		}

		if (result == MedicationOperationResult.Inactive)
		{
			return BadRequest();
		}

		return RedirectToAction(nameof(Index));
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Activate(int id)
	{
		var currentUserId = Guid.Parse(
			User.FindFirstValue(ClaimTypes.NameIdentifier)!);

		var result = await _medicationService.ActivateAsync(
			id,
			currentUserId);

		if (result == MedicationOperationResult.NotFound)
		{
			return NotFound();
		}

		if (result == MedicationOperationResult.AlreadyActive)
		{
			return BadRequest();
		}

		return RedirectToAction(nameof(Index));
	}
}