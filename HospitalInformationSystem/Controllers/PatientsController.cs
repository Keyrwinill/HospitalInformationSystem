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

[Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Receptionist}")]
public class PatientsController : Controller
{
	private readonly HospitalDbContext _context;
	private readonly IAuditService _auditService;
	private readonly IPatientService _patientService;

	public PatientsController(HospitalDbContext context, IAuditService auditService, IPatientService patientService)
	{
		_context = context;
		_auditService = auditService;
		_patientService = patientService;
	}

	[HttpGet]
	public async Task<IActionResult> Index(
		string? search,
		bool? isActive)
	{
		var query = _context.Patients
			.AsNoTracking()
			.AsQueryable();

		if (!string.IsNullOrWhiteSpace(search))
		{
			search = search.Trim();

			query = query.Where(x =>
				x.MedicalRecordNumber.Contains(search) ||
				x.FirstName.Contains(search) ||
				x.LastName.Contains(search) ||
				(x.FirstName + " " + x.LastName).Contains(search));
		}

		if (isActive.HasValue)
		{
			query = query.Where(x => x.IsActive == isActive.Value);
		}

		var patients = await query
			.OrderBy(x => x.MedicalRecordNumber)
			.ToListAsync();

		ViewBag.Search = search;
		ViewBag.IsActive = isActive;

		return View(patients);
	}

	[HttpGet]
	public IActionResult Create()
	{
		return View();
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(
	CreatePatientViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var medicalRecordNumberExists = await _context.Patients
			.AnyAsync(x =>
				x.MedicalRecordNumber == model.MedicalRecordNumber);

		if (medicalRecordNumberExists)
		{
			ModelState.AddModelError(
				nameof(model.MedicalRecordNumber),
				"Medical record number already exists.");

			return View(model);
		}

		await using var transaction =
			await _context.Database.BeginTransactionAsync();

		var patient = new Patient
		{
			MedicalRecordNumber = model.MedicalRecordNumber,
			FirstName = model.FirstName,
			LastName = model.LastName,
			Birthday = model.Birthday!.Value,
			Gender = model.Gender,
			Phone = model.Phone,
			Address = model.Address
		};

		_context.Patients.Add(patient);

		// Insert Patient and obtain the generated patient.Id.
		await _context.SaveChangesAsync();

		var currentUserId = Guid.Parse(
			User.FindFirstValue(ClaimTypes.NameIdentifier)!);

		await _auditService.LogAsync(
			currentUserId,
			"CreatePatient",
			"Patient",
			patient.Id.ToString());

		// Insert AuditLog.
		await _context.SaveChangesAsync();

		await transaction.CommitAsync();

		return RedirectToAction(nameof(Index));
	}

	[HttpGet]
	public async Task<IActionResult> Details(int id)
	{
		var patient = await _context.Patients
			.AsNoTracking()
			.Include(x => x.Appointments)
				.ThenInclude(x => x.Doctor)
					.ThenInclude(x => x.User)
			.Include(x => x.Visits)
				.ThenInclude(x => x.Doctor)
					.ThenInclude(x => x.User)
			.FirstOrDefaultAsync(x => x.Id == id);

		if (patient == null)
		{
			return NotFound();
		}

		return View(patient);
	}

	[HttpGet]
	public async Task<IActionResult> Edit(int id)
	{
		var patient = await _context.Patients
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == id);

		if (patient == null)
		{
			return NotFound();
		}

		if (!patient.IsActive)
		{
			TempData["ErrorMessage"] =
				"An inactive patient cannot be edited. Reactivate the patient first.";

			return RedirectToAction(nameof(Index));
		}

		var model = new EditPatientViewModel
		{
			Id = patient.Id,
			MedicalRecordNumber = patient.MedicalRecordNumber,
			FirstName = patient.FirstName,
			LastName = patient.LastName,
			Birthday = patient.Birthday,
			Gender = patient.Gender,
			Phone = patient.Phone,
			Address = patient.Address
		};

		return View(model);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Edit(EditPatientViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var currentUserId = Guid.Parse(
			User.FindFirstValue(ClaimTypes.NameIdentifier)!);

		var result = await _patientService.UpdateAsync(
			model.Id,
			model.MedicalRecordNumber,
			model.FirstName,
			model.LastName,
			model.Birthday,
			model.Gender,
			model.Phone,
			model.Address,
			currentUserId);

		if (result == PatientOperationResult.NotFound)
		{
			return NotFound();
		}

		if (result == PatientOperationResult.Inactive)
		{
			TempData["ErrorMessage"] =
				"Inactive patients cannot be edited.";

			return RedirectToAction(nameof(Index));
		}

		if (result == PatientOperationResult.DuplicateMedicalRecordNumber)
		{
			ModelState.AddModelError(
				nameof(model.MedicalRecordNumber),
				"Medical record number already exists.");

			return View(model);
		}

		return RedirectToAction(
			nameof(Details),
			new { id = model.Id });
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Deactivate(int id)
	{
		var currentUserId = Guid.Parse(
			User.FindFirstValue(ClaimTypes.NameIdentifier)!);

		var result = await _patientService.DeactivateAsync(
			id,
			currentUserId);

		if (result == PatientOperationResult.NotFound)
		{
			return NotFound();
		}

		if (result == PatientOperationResult.Inactive)
		{
			return BadRequest();
		}

		if (result == PatientOperationResult.HasFutureScheduledAppointment)
		{
			TempData["ErrorMessage"] =
				"The patient cannot be deactivated while they have a future scheduled appointment.";

			return RedirectToAction(nameof(Index));
		}

		if (result == PatientOperationResult.HasActiveVisit)
		{
			TempData["ErrorMessage"] =
				"The patient cannot be deactivated while they have an active visit.";

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

		var result = await _patientService.ActivateAsync(
			id,
			currentUserId);

		if (result == PatientOperationResult.NotFound)
		{
			return NotFound();
		}

		if (result == PatientOperationResult.AlreadyActive)
		{
			return BadRequest();
		}

		return RedirectToAction(nameof(Index));
	}
}