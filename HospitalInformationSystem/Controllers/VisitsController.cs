using HospitalInformationSystem.Models.Constants;
using HospitalInformationSystem.Models.Entities;
using HospitalInformationSystem.Models.ViewModels;
using HospitalInformationSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

		if (visit.Appointment == null ||
			visit.Appointment.Status != AppointmentStatus.Scheduled)
		{
			TempData["ErrorMessage"] =
				"This visit can no longer be edited.";

			return RedirectToAction(
				nameof(Details),
				new { id = visit.Id });
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

		var result = await _visitService.UpdateVisitAsync(
			model.Id,
			currentUserId,
			model.ChiefComplaint,
			model.Notes);

		if (result == VisitOperationResult.NotFound)
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

		if (visit.Appointment == null ||
			visit.Appointment.Status != AppointmentStatus.Scheduled)
		{
			TempData["ErrorMessage"] =
				"A diagnosis cannot be added to this visit because the visit is no longer active.";

			return RedirectToAction(
				nameof(Details),
				new { id = visit.Id });
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

		if (visit.Appointment == null ||
			visit.Appointment.Status != AppointmentStatus.Scheduled)
		{
			TempData["ErrorMessage"] =
				"A prescription item cannot be added to this visit because the visit is no longer active.";

			return RedirectToAction(
				nameof(Details),
				new { id = visit.Id });
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

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> AddPrescriptionItem(
	AddPrescriptionItemViewModel model)
	{
		if (!ModelState.IsValid)
		{
			ViewBag.Medications =
				await _visitService.GetActiveMedicationsAsync();

			return View(model);
		}

		var currentUserId = GetCurrentUserId();

		var prescriptionItem =
			await _visitService.AddPrescriptionItemAsync(
				model.VisitId,
				currentUserId,
				model.MedicationId!.Value,
				model.Dosage,
				model.Frequency,
				model.Days!.Value);

		if (prescriptionItem == null)
		{
			return NotFound();
		}

		return RedirectToAction(
			nameof(Details),
			new { id = model.VisitId });
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Complete(int id)
	{
		var currentUserId = GetCurrentUserId();

		var result = await _visitService.CompleteVisitAsync(
			id,
			currentUserId);

		if (result == VisitOperationResult.NotFound)
		{
			return NotFound();
		}

		if (result == VisitOperationResult.ValidationError)
		{
			TempData["ErrorMessage"] =
				"The visit cannot be completed. At least one diagnosis is required.";

			return RedirectToAction(
				nameof(Details),
				new { id });
		}

		return RedirectToAction(
			nameof(Details),
			new { id });
	}

	[HttpGet]
	public async Task<IActionResult> EditDiagnosis(int id)
	{
		var currentUserId = GetCurrentUserId();

		var diagnosis = await _visitService
			.GetDiagnosisForEditAsync(id, currentUserId);

		if (diagnosis == null)
		{
			return NotFound();
		}

		var model = new EditDiagnosisViewModel
		{
			Id = diagnosis.Id,
			VisitId = diagnosis.VisitId,
			DiagnosisCode = diagnosis.DiagnosisCode,
			Description = diagnosis.Description
		};

		return View(model);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> EditDiagnosis(
	EditDiagnosisViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var currentUserId = GetCurrentUserId();

		var visitId = await _visitService.UpdateDiagnosisAsync(
			model.Id,
			currentUserId,
			model.DiagnosisCode,
			model.Description);

		if (!visitId.HasValue)
		{
			return BadRequest();
		}

		return RedirectToAction(
			nameof(Details),
			new { id = visitId.Value });
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> DeleteDiagnosis(int id)
	{
		var currentUserId = GetCurrentUserId();

		var visitId = await _visitService.DeleteDiagnosisAsync(
			id,
			currentUserId);

		if (!visitId.HasValue)
		{
			return BadRequest();
		}

		return RedirectToAction(
			nameof(Details),
			new { id = visitId.Value });
	}

	[HttpGet]
	public async Task<IActionResult> EditPrescriptionItem(int id)
	{
		var currentUserId = GetCurrentUserId();

		var item = await _visitService
			.GetPrescriptionItemForEditAsync(id, currentUserId);

		if (item == null)
		{
			return NotFound();
		}

		var model = new EditPrescriptionItemViewModel
		{
			Id = item.Id,
			Dosage = item.Dosage,
			Frequency = item.Frequency,
			Days = item.Days
		};

		return View(model);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> EditPrescriptionItem(
	EditPrescriptionItemViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var currentUserId = GetCurrentUserId();

		var visitId = await _visitService.UpdatePrescriptionItemAsync(
			model.Id,
			currentUserId,
			model.Dosage,
			model.Frequency,
			model.Days);

		if (!visitId.HasValue)
		{
			return BadRequest();
		}

		return RedirectToAction(
			nameof(Details),
			new { id = visitId.Value });
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> DeletePrescriptionItem(int id)
	{
		var currentUserId = GetCurrentUserId();

		var visitId = await _visitService.DeletePrescriptionItemAsync(
			id,
			currentUserId);

		if (!visitId.HasValue)
		{
			return BadRequest();
		}

		return RedirectToAction(
			nameof(Details),
			new { id = visitId.Value });
	}
}