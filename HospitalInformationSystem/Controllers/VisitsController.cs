using HospitalInformationSystem.Models.Constants;
using HospitalInformationSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HospitalInformationSystem.Models.ViewModels;

namespace HospitalInformationSystem.Controllers;

[Authorize(Roles = UserRoles.Doctor)]
public class VisitsController : Controller
{
	private readonly IVisitService _visitService;

	public VisitsController(IVisitService visitService)
	{
		_visitService = visitService;
	}

	[HttpGet]
	public async Task<IActionResult> Index()
	{
		var currentUserId = GetCurrentUserId();

		var appointments =
			await _visitService.GetAppointmentsForDoctorAsync(currentUserId);

		return View(appointments);
	}

	[HttpGet]
	public async Task<IActionResult> Details(int id)
	{
		var currentUserId = GetCurrentUserId();

		var visit = await _visitService.GetVisitForDoctorAsync(
			id,
			currentUserId);

		if (visit == null)
		{
			return NotFound();
		}

		return View(visit);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Start(int appointmentId)
	{
		var currentUserId = GetCurrentUserId();

		var visit = await _visitService.CreateFromAppointmentAsync(
			appointmentId,
			currentUserId);

		if (visit == null)
		{
			return BadRequest();
		}

		return RedirectToAction(nameof(Details), new { id = visit.Id });
	}

	private Guid GetCurrentUserId()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (!Guid.TryParse(userId, out var currentUserId))
		{
			throw new InvalidOperationException(
				"The authenticated user does not have a valid user ID.");
		}

		return currentUserId;
	}

	[HttpGet]
	public async Task<IActionResult> Edit(int id)
	{
		var currentUserId = GetCurrentUserId();

		var visit = await _visitService.GetVisitForDoctorAsync(
			id,
			currentUserId);

		if (visit == null)
		{
			return NotFound();
		}

		var model = new EditVisitViewModel
		{
			Id = visit.Id,
			ChiefComplaint = visit.ChiefComplaint,
			Notes = visit.Notes
		};

		return View(model);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Edit(EditVisitViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var currentUserId = GetCurrentUserId();

		var updated = await _visitService.UpdateVisitAsync(
			model.Id,
			currentUserId,
			model.ChiefComplaint,
			model.Notes);

		if (!updated)
		{
			return NotFound();
		}

		return RedirectToAction(
			nameof(Details),
			new { id = model.Id });
	}

	[HttpGet]
	public async Task<IActionResult> AddDiagnosis(int visitId)
	{
		var currentUserId = GetCurrentUserId();

		var visit = await _visitService.GetVisitForDoctorAsync(
			visitId,
			currentUserId);

		if (visit == null)
		{
			return NotFound();
		}

		var model = new CreateDiagnosisViewModel
		{
			VisitId = visit.Id
		};

		return View(model);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> AddDiagnosis(
		CreateDiagnosisViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var currentUserId = GetCurrentUserId();

		var diagnosis = await _visitService.AddDiagnosisAsync(
			model.VisitId,
			currentUserId,
			model.DiagnosisCode,
			model.Description);

		if (diagnosis == null)
		{
			return NotFound();
		}

		return RedirectToAction(
			nameof(Details),
			new { id = model.VisitId });
	}

	[HttpGet]
	public async Task<IActionResult> AddPrescriptionItem(int visitId)
	{
		var currentUserId = GetCurrentUserId();

		var visit = await _visitService.GetVisitForDoctorAsync(
			visitId,
			currentUserId);

		if (visit == null)
		{
			return NotFound();
		}

		var medications =
			await _visitService.GetActiveMedicationsAsync();

		ViewBag.Medications = medications;

		var model = new AddPrescriptionItemViewModel
		{
			VisitId = visit.Id
		};

		return View(model);
	}
}