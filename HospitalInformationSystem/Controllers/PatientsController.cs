using HospitalInformationSystem.Data;
using HospitalInformationSystem.Models.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalInformationSystem.Models.Entities;
using HospitalInformationSystem.Models.ViewModels;

namespace HospitalInformationSystem.Controllers;

[Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Receptionist}")]
public class PatientsController : Controller
{
	private readonly HospitalDbContext _context;

	public PatientsController(HospitalDbContext context)
	{
		_context = context;
	}

	public async Task<IActionResult> Index()
	{
		var patients = await _context.Patients
			.AsNoTracking()
			.OrderBy(x => x.MedicalRecordNumber)
			.ToListAsync();

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

		await _context.SaveChangesAsync();

		return RedirectToAction(nameof(Index));
	}
}