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

[Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Receptionist}")]
public class AppointmentsController : Controller
{
	private readonly HospitalDbContext _context;
	private readonly IAppointmentService _appointmentService;

	public AppointmentsController(HospitalDbContext context, IAppointmentService appointmentService)
	{
		_context = context;
		_appointmentService = appointmentService;
	}

	public async Task<IActionResult> Index(
		AppointmentStatus? status,
		string? search,
		int? doctorId,
		int? departmentId,
		DateOnly? date)
	{
		var query = _context.Appointments
			.AsNoTracking()
			.Include(x => x.Patient)
			.Include(x => x.Doctor)
				.ThenInclude(x => x.User)
			.Include(x => x.Doctor)
				.ThenInclude(x => x.Department)
			.Include(x => x.Visit)
			.AsQueryable();

		if (status.HasValue)
		{
			query = query.Where(x => x.Status == status.Value);
		}

		if (!string.IsNullOrWhiteSpace(search))
		{
			search = search.Trim();

			query = query.Where(x =>
				x.Patient.MedicalRecordNumber.Contains(search) ||
				x.Patient.FirstName.Contains(search) ||
				x.Patient.LastName.Contains(search) ||
				(x.Patient.FirstName + " " + x.Patient.LastName).Contains(search));
		}

		if (doctorId.HasValue)
		{
			query = query.Where(x =>
				x.DoctorId == doctorId.Value);
		}

		if (departmentId.HasValue)
		{
			query = query.Where(x =>
				x.Doctor.DepartmentId == departmentId.Value);
		}

		if (date.HasValue)
		{
			var start = date.Value.ToDateTime(TimeOnly.MinValue);
			var end = start.AddDays(1);

			query = query.Where(x =>
				x.AppointmentDateTime >= start &&
				x.AppointmentDateTime < end);
		}

		var appointments = await query
			.OrderByDescending(x => x.AppointmentDateTime)
			.ToListAsync();

		ViewBag.Status = status;
		ViewBag.Search = search;
		ViewBag.DoctorId = doctorId;
		ViewBag.Doctors = await _context.Doctors
			.AsNoTracking()
			.Include(x => x.User)
			.Where(x =>
				x.IsActive &&
				x.User.IsActive)
			.OrderBy(x => x.User.FirstName)
			.ThenBy(x => x.User.LastName)
			.ToListAsync();

		ViewBag.DepartmentId = departmentId;
		ViewBag.Departments = await _context.Departments
			.AsNoTracking()
			.Where(x => x.IsActive)
			.OrderBy(x => x.Name)
			.ToListAsync();

		ViewBag.Date = date;

		return View(appointments);
	}

	[HttpGet]
	public async Task<IActionResult> Create()
	{
		await LoadCreateOptionsAsync();

		return View();
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(
	CreateAppointmentViewModel model)
	{
		if (!ModelState.IsValid)
		{
			await LoadCreateOptionsAsync();
			return View(model);
		}

		var currentUserId = Guid.Parse(
			User.FindFirstValue(ClaimTypes.NameIdentifier)!);

		var result = await _appointmentService.CreateAsync(
			model.PatientId!.Value,
			model.DoctorId!.Value,
			model.AppointmentDateTime!.Value,
			model.Reason,
			currentUserId);

		if (result == AppointmentOperationResult.PastDateTime)
		{
			ModelState.AddModelError(
				nameof(model.AppointmentDateTime),
				"Appointment date and time must be in the future.");
		}
		else if (result == AppointmentOperationResult.InvalidPatient)
		{
			ModelState.AddModelError(
				nameof(model.PatientId),
				"Please select a valid patient.");
		}
		else if (result == AppointmentOperationResult.InvalidDoctor)
		{
			ModelState.AddModelError(
				nameof(model.DoctorId),
				"Please select a valid doctor.");
		}
		else if (result == AppointmentOperationResult.ScheduleConflict)
		{
			ModelState.AddModelError(
				nameof(model.AppointmentDateTime),
				"The doctor already has an appointment at this time.");
		}

		if (result != AppointmentOperationResult.Success)
		{
			await LoadCreateOptionsAsync();
			return View(model);
		}

		return RedirectToAction(nameof(Index));
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Cancel(int id)
	{
		var currentUserId = Guid.Parse(
			User.FindFirstValue(ClaimTypes.NameIdentifier)!);

		var result = await _appointmentService.CancelAsync(
			id,
			currentUserId);

		if (result == AppointmentOperationResult.NotFound)
		{
			return NotFound();
		}

		if (result == AppointmentOperationResult.InvalidState)
		{
			TempData["ErrorMessage"] =
				"This appointment can no longer be cancelled.";

			return RedirectToAction(nameof(Index));
		}

		if (result == AppointmentOperationResult.PastDateTime)
		{
			TempData["ErrorMessage"] =
				"A past appointment cannot be cancelled.";

			return RedirectToAction(nameof(Index));
		}

		return RedirectToAction(nameof(Index));
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> MarkNoShow(int id)
	{
		var currentUserId = Guid.Parse(
			User.FindFirstValue(ClaimTypes.NameIdentifier)!);

		var result = await _appointmentService.MarkNoShowAsync(
			id,
			currentUserId);

		if (result == AppointmentOperationResult.NotFound)
		{
			return NotFound();
		}

		if (result == AppointmentOperationResult.InvalidState)
		{
			TempData["ErrorMessage"] =
				"This appointment can no longer be marked as no-show.";

			return RedirectToAction(nameof(Index));
		}

		if (result == AppointmentOperationResult.FutureDateTime)
		{
			TempData["ErrorMessage"] =
				"A future appointment cannot be marked as no-show.";

			return RedirectToAction(nameof(Index));
		}

		return RedirectToAction(nameof(Index));
	}

	[HttpGet]
	public async Task<IActionResult> Reschedule(int id)
	{
		var appointment =
			await _appointmentService.GetReschedulableAppointmentAsync(id);

		if (appointment == null)
		{
			TempData["ErrorMessage"] =
				"This appointment cannot be rescheduled.";

			return RedirectToAction(nameof(Index));
		}

		var model = new RescheduleAppointmentViewModel
		{
			Id = appointment.Id,
			AppointmentDateTime = appointment.AppointmentDateTime
		};

		return View(model);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Reschedule(
	RescheduleAppointmentViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var currentUserId = Guid.Parse(
			User.FindFirstValue(ClaimTypes.NameIdentifier)!);

		var result = await _appointmentService.RescheduleAsync(
			model.Id,
			model.AppointmentDateTime,
			currentUserId);

		if (result == AppointmentOperationResult.NotFound)
		{
			return NotFound();
		}

		if (result == AppointmentOperationResult.InvalidState)
		{
			TempData["ErrorMessage"] =
				"This appointment can no longer be rescheduled.";

			return RedirectToAction(nameof(Index));
		}

		if (result == AppointmentOperationResult.PastDateTime)
		{
			ModelState.AddModelError(
				nameof(model.AppointmentDateTime),
				"Appointment date and time must be in the future.");

			return View(model);
		}

		if (result == AppointmentOperationResult.ScheduleConflict)
		{
			ModelState.AddModelError(
				nameof(model.AppointmentDateTime),
				"The doctor already has an appointment at this time.");

			return View(model);
		}

		if (result == AppointmentOperationResult.InvalidPatient)
		{
			TempData["ErrorMessage"] =
				"This appointment cannot be rescheduled because the patient is inactive.";

			return RedirectToAction(nameof(Index));
		}

		if (result == AppointmentOperationResult.InvalidDoctor)
		{
			TempData["ErrorMessage"] =
				"This appointment cannot be rescheduled because the doctor is unavailable.";

			return RedirectToAction(nameof(Index));
		}

		return RedirectToAction(nameof(Index));
	}

	private async Task LoadCreateOptionsAsync()
	{
		var patients = await _context.Patients
			.AsNoTracking()
			.Where(x => x.IsActive)
			.OrderBy(x => x.MedicalRecordNumber)
			.ToListAsync();

		var doctors = await _context.Doctors
			.AsNoTracking()
			.Where(x =>
				x.IsActive &&
				x.User.IsActive &&
				x.Department.IsActive)
			.Include(x => x.User)
			.Include(x => x.Department)
			.OrderBy(x => x.User.LastName)
			.ThenBy(x => x.User.FirstName)
			.ToListAsync();


		ViewBag.Patients = new SelectList(
			patients.Select(x => new
			{
				x.Id,
				DisplayName =
					$"{x.MedicalRecordNumber} - {x.FirstName} {x.LastName}"
			}),
			"Id",
			"DisplayName");

		ViewBag.Doctors = new SelectList(
			doctors.Select(x => new
			{
				x.Id,
				DisplayName =
					$"{x.User.FirstName} {x.User.LastName} - {x.Department.Name}"
			}),
			"Id",
			"DisplayName");
	}
}