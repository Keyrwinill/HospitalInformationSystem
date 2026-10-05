using HospitalInformationSystem.Data;
using HospitalInformationSystem.Models.Constants;
using HospitalInformationSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HospitalInformationSystem.Services;

public class PatientService : IPatientService
{
	private readonly HospitalDbContext _context;
	private readonly IAuditService _auditService;

	public PatientService(
		HospitalDbContext context,
		IAuditService auditService)
	{
		_context = context;
		_auditService = auditService;
	}

	public async Task<PatientOperationResult> DeactivateAsync(
		int patientId,
		Guid currentUserId)
	{
		var patient = await _context.Patients
			.FirstOrDefaultAsync(x => x.Id == patientId);

		if (patient == null)
		{
			return PatientOperationResult.NotFound;
		}

		if (!patient.IsActive)
		{
			return PatientOperationResult.Inactive;
		}

		var hasFutureScheduledAppointment =
			await _context.Appointments.AnyAsync(x =>
				x.PatientId == patient.Id &&
				x.Status == AppointmentStatus.Scheduled &&
				x.AppointmentDateTime > DateTime.Now);

		if (hasFutureScheduledAppointment)
		{
			return PatientOperationResult.HasFutureScheduledAppointment;
		}

		var hasActiveVisit = await _context.Visits
			.AnyAsync(x =>
				x.PatientId == patient.Id &&
				x.Appointment != null &&
				x.Appointment.Status == AppointmentStatus.Scheduled);

		if (hasActiveVisit)
		{
			return PatientOperationResult.HasActiveVisit;
		}

		patient.IsActive = false;

		await _auditService.LogAsync(
			currentUserId,
			"DeactivatePatient",
			"Patient",
			patient.Id.ToString());

		await _context.SaveChangesAsync();

		return PatientOperationResult.Success;
	}

	public async Task<PatientOperationResult> ActivateAsync(
		int patientId,
		Guid currentUserId)
	{
		var patient = await _context.Patients
			.FirstOrDefaultAsync(x => x.Id == patientId);

		if (patient == null)
		{
			return PatientOperationResult.NotFound;
		}

		if (patient.IsActive)
		{
			return PatientOperationResult.AlreadyActive;
		}

		patient.IsActive = true;

		await _auditService.LogAsync(
			currentUserId,
			"ActivatePatient",
			"Patient",
			patient.Id.ToString());

		await _context.SaveChangesAsync();

		return PatientOperationResult.Success;
	}

	public async Task<PatientOperationResult> UpdateAsync(
		int patientId,
		string medicalRecordNumber,
		string firstName,
		string lastName,
		DateOnly birthday,
		string gender,
		string? phone,
		string? address,
		Guid currentUserId)
	{
		var patient = await _context.Patients
			.FirstOrDefaultAsync(x => x.Id == patientId);

		if (patient == null)
		{
			return PatientOperationResult.NotFound;
		}

		if (!patient.IsActive)
		{
			return PatientOperationResult.Inactive;
		}

		var duplicateMedicalRecordNumber =
			await _context.Patients.AnyAsync(x =>
				x.MedicalRecordNumber == medicalRecordNumber &&
				x.Id != patientId);

		if (duplicateMedicalRecordNumber)
		{
			return PatientOperationResult.DuplicateMedicalRecordNumber;
		}

		patient.MedicalRecordNumber = medicalRecordNumber;
		patient.FirstName = firstName;
		patient.LastName = lastName;
		patient.Birthday = birthday;
		patient.Gender = gender;
		patient.Phone = phone;
		patient.Address = address;

		await _auditService.LogAsync(
			currentUserId,
			"UpdatePatient",
			"Patient",
			patient.Id.ToString());

		await _context.SaveChangesAsync();

		return PatientOperationResult.Success;
	}
}