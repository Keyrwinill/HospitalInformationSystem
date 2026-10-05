using HospitalInformationSystem.Data;
using HospitalInformationSystem.Models.Constants;
using HospitalInformationSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HospitalInformationSystem.Services;

public class VisitService : IVisitService
{
	private readonly HospitalDbContext _context;
	private readonly IAuditService _auditService;

	public VisitService(HospitalDbContext context, IAuditService auditService)
	{
		_context = context;
		_auditService = auditService;
	}

	public async Task<List<Appointment>> GetAppointmentsForDoctorAsync(
		Guid currentUserId)
	{
		return await _context.Appointments
			.AsNoTracking()
			.Include(x => x.Patient)
			.Include(x => x.Visit)
			.Where(x =>
				x.Doctor.UserId == currentUserId &&
				x.Doctor.IsActive &&
				x.Doctor.User.IsActive)
			.OrderBy(x => x.Status == AppointmentStatus.Scheduled ? 0 : 1)
			.ThenByDescending(x => x.AppointmentDateTime)
			.ToListAsync();
	}

	public async Task<Visit?> CreateFromAppointmentAsync(
		int appointmentId,
		Guid currentUserId)
	{
		var doctor = await _context.Doctors
			.FirstOrDefaultAsync(x =>
				x.UserId == currentUserId &&
				x.IsActive &&
				x.User.IsActive &&
				x.Department.IsActive);

		if (doctor == null)
		{
			return null;
		}

		var appointment = await _context.Appointments
			.FirstOrDefaultAsync(x =>
				x.Id == appointmentId &&
				x.DoctorId == doctor.Id &&
				x.Patient.IsActive);

		if (appointment == null)
		{
			return null;
		}

		if (appointment.Status != AppointmentStatus.Scheduled)
		{
			return null;
		}

		if (appointment.AppointmentDateTime > DateTime.Now)
		{
			return null;
		}

		var visitExists = await _context.Visits
			.AnyAsync(x => x.AppointmentId == appointment.Id);

		if (visitExists)
		{
			return null;
		}

		await using var transaction =
			await _context.Database.BeginTransactionAsync();

		var visit = new Visit
		{
			PatientId = appointment.PatientId,
			DoctorId = doctor.Id,
			AppointmentId = appointment.Id,
			VisitDateTime = DateTime.Now
		};

		_context.Visits.Add(visit);

		// First save creates the Visit and generates visit.Id.
		await _context.SaveChangesAsync();

		await _auditService.LogAsync(
			currentUserId,
			"StartVisit",
			"Visit",
			visit.Id.ToString());

		// Save the staged AuditLog.
		await _context.SaveChangesAsync();

		await transaction.CommitAsync();

		return visit;
	}

	public async Task<Visit?> GetVisitForDoctorAsync(
		int visitId,
		Guid currentUserId)
	{
		return await _context.Visits
			.AsNoTracking()
			.Include(x => x.Patient)
			.Include(x => x.Doctor)
				.ThenInclude(x => x.User)
			.Include(x => x.Appointment)
			.Include(x => x.Diagnoses)
			.Include(x => x.Prescription)
				.ThenInclude(x => x.Items)
					.ThenInclude(x => x.Medication)
			.FirstOrDefaultAsync(x =>
				x.Id == visitId &&
				x.Doctor.UserId == currentUserId &&
				x.Doctor.IsActive &&
				x.Doctor.User.IsActive);
	}

	public async Task<VisitOperationResult> UpdateVisitAsync(
		int visitId,
		Guid currentUserId,
		string? chiefComplaint,
		string? notes)
	{
		var visit = await GetEditableVisitAsync(
			visitId,
			currentUserId);

		if (visit == null)
		{
			return VisitOperationResult.NotFound;
		}

		visit.ChiefComplaint = chiefComplaint;
		visit.Notes = notes;

		await _auditService.LogAsync(
			currentUserId,
			"UpdateVisit",
			"Visit",
			visit.Id.ToString());

		await _context.SaveChangesAsync();

		return VisitOperationResult.Success;
	}

	public async Task<Diagnosis?> AddDiagnosisAsync(
		int visitId,
		Guid currentUserId,
		string diagnosisCode,
		string description)
	{
		if (string.IsNullOrWhiteSpace(diagnosisCode) ||
			string.IsNullOrWhiteSpace(description))
		{
			return null;
		}

		var visit = await GetEditableVisitAsync(
			visitId,
			currentUserId);

		if (visit == null)
		{
			return null;
		}

		await using var transaction =
			await _context.Database.BeginTransactionAsync();

		var diagnosis = new Diagnosis
		{
			VisitId = visit.Id,
			DiagnosisCode = diagnosisCode,
			Description = description
		};

		_context.Diagnoses.Add(diagnosis);

		// First save generates diagnosis.Id.
		await _context.SaveChangesAsync();

		await _auditService.LogAsync(
			currentUserId,
			"AddDiagnosis",
			"Diagnosis",
			diagnosis.Id.ToString());

		// Save the staged AuditLog.
		await _context.SaveChangesAsync();

		await transaction.CommitAsync();

		return diagnosis;
	}

	public async Task<PrescriptionItem?> AddPrescriptionItemAsync(
		int visitId,
		Guid currentUserId,
		int medicationId,
		string dosage,
		string frequency,
		int days)
	{

		if (string.IsNullOrWhiteSpace(dosage) ||
			string.IsNullOrWhiteSpace(frequency) ||
			days <= 0)
		{
			return null;
		}

		var visit = await GetEditableVisitAsync(
			visitId,
			currentUserId);

		if (visit == null)
		{
			return null;
		}

		var medication = await _context.Medications
			.FirstOrDefaultAsync(x =>
				x.Id == medicationId &&
				x.IsActive);

		if (medication == null)
		{
			return null;
		}

		var prescription = await _context.Prescriptions
			.FirstOrDefaultAsync(x => x.VisitId == visit.Id);

		if (prescription == null)
		{
			prescription = new Prescription
			{
				VisitId = visit.Id
			};

			_context.Prescriptions.Add(prescription);
		}

		await using var transaction =
			await _context.Database.BeginTransactionAsync();

		var prescriptionItem = new PrescriptionItem
		{
			Prescription = prescription,
			Medication = medication,
			Dosage = dosage,
			Frequency = frequency,
			Days = days
		};

		_context.PrescriptionItems.Add(prescriptionItem);

		// Saves the new Prescription if necessary,
		// saves PrescriptionItem, and generates prescriptionItem.Id.
		await _context.SaveChangesAsync();

		await _auditService.LogAsync(
			currentUserId,
			"AddPrescriptionItem",
			"PrescriptionItem",
			prescriptionItem.Id.ToString());

		// Save the staged AuditLog.
		await _context.SaveChangesAsync();

		await transaction.CommitAsync();

		return prescriptionItem;
	}

	public async Task<List<Medication>> GetActiveMedicationsAsync()
	{
		return await _context.Medications
			.AsNoTracking()
			.Where(x => x.IsActive)
			.OrderBy(x => x.Name)
			.ToListAsync();
	}

	public async Task<VisitOperationResult> CompleteVisitAsync(
		int visitId,
		Guid currentUserId)
	{
		var visit = await GetEditableVisitAsync(
			visitId,
			currentUserId,
			includeAppointment: true);

		if (visit == null)
		{
			return VisitOperationResult.NotFound;
		}

		var hasDiagnosis = await _context.Diagnoses
			.AnyAsync(x => x.VisitId == visit.Id);

		if (!hasDiagnosis)
		{
			return VisitOperationResult.ValidationError;
		}

		visit.Appointment!.Status = AppointmentStatus.Completed;

		await _auditService.LogAsync(
			currentUserId,
			"CompleteVisit",
			"Visit",
			visit.Id.ToString());

		await _context.SaveChangesAsync();

		return VisitOperationResult.Success;
	}

	public async Task<Diagnosis?> GetDiagnosisForEditAsync(
		int diagnosisId,
		Guid currentUserId)
	{
		return await GetEditableDiagnosisAsync(
			diagnosisId,
			currentUserId,
			asNoTracking: true);
	}

	public async Task<int?> UpdateDiagnosisAsync(
		int diagnosisId,
		Guid currentUserId,
		string diagnosisCode,
		string description)
	{
		if (string.IsNullOrWhiteSpace(diagnosisCode) ||
			string.IsNullOrWhiteSpace(description))
		{
			return null;
		}

		var diagnosis = await GetEditableDiagnosisAsync(
			diagnosisId,
			currentUserId);

		if (diagnosis == null)
		{
			return null;
		}

		diagnosis.DiagnosisCode = diagnosisCode;
		diagnosis.Description = description;

		await _auditService.LogAsync(
			currentUserId,
			"UpdateDiagnosis",
			"Diagnosis",
			diagnosis.Id.ToString());

		await _context.SaveChangesAsync();

		return diagnosis.VisitId;
	}

	public async Task<int?> DeleteDiagnosisAsync(
		int diagnosisId,
		Guid currentUserId)
	{
		var diagnosis = await GetEditableDiagnosisAsync(
			diagnosisId,
			currentUserId);

		if (diagnosis == null)
		{
			return null;
		}

		var visitId = diagnosis.VisitId;

		_context.Diagnoses.Remove(diagnosis);

		await _auditService.LogAsync(
			currentUserId,
			"DeleteDiagnosis",
			"Diagnosis",
			diagnosisId.ToString());

		await _context.SaveChangesAsync();

		return visitId;
	}

	public async Task<PrescriptionItem?> GetPrescriptionItemForEditAsync(
		int prescriptionItemId,
		Guid currentUserId)
	{
		return await GetEditablePrescriptionItemAsync(
			prescriptionItemId,
			currentUserId,
			asNoTracking: true);
	}

	public async Task<int?> UpdatePrescriptionItemAsync(
		int prescriptionItemId,
		Guid currentUserId,
		string dosage,
		string frequency,
		int days)
	{
		if (string.IsNullOrWhiteSpace(dosage) ||
			string.IsNullOrWhiteSpace(frequency) ||
			days <= 0)
		{
			return null;
		}

		var item = await GetEditablePrescriptionItemAsync(
			prescriptionItemId,
			currentUserId,
			includePrescription: true);

		if (item == null)
		{
			return null;
		}

		item.Dosage = dosage;
		item.Frequency = frequency;
		item.Days = days;

		await _auditService.LogAsync(
			currentUserId,
			"UpdatePrescriptionItem",
			"PrescriptionItem",
			item.Id.ToString());

		await _context.SaveChangesAsync();

		return item.Prescription.VisitId;
	}

	public async Task<int?> DeletePrescriptionItemAsync(
		int prescriptionItemId,
		Guid currentUserId)
	{
		var item = await GetEditablePrescriptionItemAsync(
			prescriptionItemId,
			currentUserId,
			includePrescription: true);

		if (item == null)
		{
			return null;
		}

		var visitId = item.Prescription.VisitId;

		_context.PrescriptionItems.Remove(item);

		await _auditService.LogAsync(
			currentUserId,
			"DeletePrescriptionItem",
			"PrescriptionItem",
			prescriptionItemId.ToString());

		await _context.SaveChangesAsync();

		return visitId;
	}

	private async Task<Visit?> GetEditableVisitAsync(
		int visitId,
		Guid currentUserId,
		bool includeAppointment = false)
	{
		var query = _context.Visits
			.Where(x =>
				x.Id == visitId &&
				x.Doctor.UserId == currentUserId &&
				x.Doctor.IsActive &&
				x.Doctor.User.IsActive &&
				x.Appointment != null &&
				x.Appointment.Status == AppointmentStatus.Scheduled);

		if (includeAppointment)
		{
			query = query.Include(x => x.Appointment);
		}

		return await query.FirstOrDefaultAsync();
	}

	private async Task<Diagnosis?> GetEditableDiagnosisAsync(
		int diagnosisId,
		Guid currentUserId,
		bool asNoTracking = false)
	{
		var query = _context.Diagnoses
			.Where(x =>
				x.Id == diagnosisId &&
				x.Visit.Doctor.UserId == currentUserId &&
				x.Visit.Doctor.IsActive &&
				x.Visit.Doctor.User.IsActive &&
				x.Visit.Appointment != null &&
				x.Visit.Appointment.Status == AppointmentStatus.Scheduled);

		if (asNoTracking)
		{
			query = query.AsNoTracking();
		}

		return await query.FirstOrDefaultAsync();
	}

	private async Task<PrescriptionItem?> GetEditablePrescriptionItemAsync(
		int prescriptionItemId,
		Guid currentUserId,
		bool asNoTracking = false,
		bool includePrescription = false)
	{
		var query = _context.PrescriptionItems
			.Where(x =>
				x.Id == prescriptionItemId &&
				x.Prescription.Visit.Doctor.UserId == currentUserId &&
				x.Prescription.Visit.Doctor.IsActive &&
				x.Prescription.Visit.Doctor.User.IsActive &&
				x.Prescription.Visit.Appointment != null &&
				x.Prescription.Visit.Appointment.Status ==
					AppointmentStatus.Scheduled);

		if (includePrescription)
		{
			query = query.Include(x => x.Prescription);
		}

		if (asNoTracking)
		{
			query = query.AsNoTracking();
		}

		return await query.FirstOrDefaultAsync();
	}
}