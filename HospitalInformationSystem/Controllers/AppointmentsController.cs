using HospitalInformationSystem.Data;
using HospitalInformationSystem.Models.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using HospitalInformationSystem.Models.Entities;
using HospitalInformationSystem.Models.ViewModels;

namespace HospitalInformationSystem.Controllers;

[Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Receptionist}")]
public class AppointmentsController : Controller
{
	private readonly HospitalDbContext _context;

	public AppointmentsController(HospitalDbContext context)
	{
		_context = context;
	}

	public async Task<IActionResult> Index()
	{
		var appointments = await _context.Appointments
			.AsNoTracking()
			.Include(x => x.Patient)
			.Include(x => x.Doctor)
				.ThenInclude(x => x.User)
			.Include(x => x.Doctor)
				.ThenInclude(x => x.Department)
			.OrderBy(x => x.AppointmentDateTime)
			.ToListAsync();

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

		if (model.AppointmentDateTime <= DateTime.Now)
		{
			ModelState.AddModelError(
				nameof(model.AppointmentDateTime),
				"Appointment date and time must be in the future.");

			await LoadCreateOptionsAsync();
			return View(model);
		}

		var patientExists = await _context.Patients
			.AnyAsync(x => x.Id == model.PatientId);

		if (!patientExists)
		{
			ModelState.AddModelError(
				nameof(model.PatientId),
				"Please select a valid patient.");

			await LoadCreateOptionsAsync();
			return View(model);
		}

		var doctorExists = await _context.Doctors
			.AnyAsync(x =>
				x.Id == model.DoctorId &&
				x.IsActive &&
				x.User.IsActive);

		if (!doctorExists)
		{
			ModelState.AddModelError(
				nameof(model.DoctorId),
				"Please select a valid doctor.");

			await LoadCreateOptionsAsync();
			return View(model);
		}

		var appointmentConflict = await _context.Appointments
			.AnyAsync(x =>
				x.DoctorId == model.DoctorId &&
				x.AppointmentDateTime == model.AppointmentDateTime &&
				x.Status != AppointmentStatus.Cancelled);

		if (appointmentConflict)
		{
			ModelState.AddModelError(
				nameof(model.AppointmentDateTime),
				"The doctor already has an appointment at this time.");

			await LoadCreateOptionsAsync();
			return View(model);
		}

		var appointment = new Appointment
		{
			PatientId = model.PatientId!.Value,
			DoctorId = model.DoctorId!.Value,
			AppointmentDateTime = model.AppointmentDateTime!.Value,
			Status = AppointmentStatus.Scheduled,
			Reason = model.Reason
		};

		_context.Appointments.Add(appointment);

		await _context.SaveChangesAsync();

		return RedirectToAction(nameof(Index));
	}

	private async Task LoadCreateOptionsAsync()
	{
		var patients = await _context.Patients
			.AsNoTracking()
			.OrderBy(x => x.MedicalRecordNumber)
			.ToListAsync();

		var doctors = await _context.Doctors
			.AsNoTracking()
			.Where(x => x.IsActive && x.User.IsActive)
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

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Cancel(int id)
	{
		var appointment = await _context.Appointments
			.FirstOrDefaultAsync(x => x.Id == id);

		if (appointment == null)
		{
			return NotFound();
		}

		if (appointment.Status != AppointmentStatus.Scheduled)
		{
			return BadRequest();
		}

		appointment.Status = AppointmentStatus.Cancelled;

		await _context.SaveChangesAsync();

		return RedirectToAction(nameof(Index));
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> MarkNoShow(int id)
	{
		var appointment = await _context.Appointments
			.FirstOrDefaultAsync(x => x.Id == id);

		if (appointment == null)
		{
			return NotFound();
		}

		if (appointment.Status != AppointmentStatus.Scheduled)
		{
			return BadRequest();
		}

		if (appointment.AppointmentDateTime > DateTime.Now)
		{
			return BadRequest();
		}

		appointment.Status = AppointmentStatus.NoShow;

		await _context.SaveChangesAsync();

		return RedirectToAction(nameof(Index));
	}
}