using HospitalInformationSystem.Data;
using HospitalInformationSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HospitalInformationSystem.Services;

public class VisitService : IVisitService
{
	private readonly HospitalDbContext _context;

	public VisitService(HospitalDbContext context)
	{
		_context = context;
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
			.OrderBy(x => x.AppointmentDateTime)
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
				x.User.IsActive);

		if (doctor == null)
		{
			return null;
		}

		var appointment = await _context.Appointments
			.FirstOrDefaultAsync(x =>
				x.Id == appointmentId &&
				x.DoctorId == doctor.Id);

		if (appointment == null)
		{
			return null;
		}

		if (appointment.Status != AppointmentStatus.Scheduled)
		{
			return null;
		}

		var visitExists = await _context.Visits
			.AnyAsync(x => x.AppointmentId == appointment.Id);

		if (visitExists)
		{
			return null;
		}

		var visit = new Visit
		{
			PatientId = appointment.PatientId,
			DoctorId = doctor.Id,
			AppointmentId = appointment.Id,
			VisitDateTime = DateTime.Now
		};

		_context.Visits.Add(visit);

		await _context.SaveChangesAsync();

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
			.FirstOrDefaultAsync(x =>
				x.Id == visitId &&
				x.Doctor.UserId == currentUserId &&
				x.Doctor.IsActive &&
				x.Doctor.User.IsActive);
	}

	public async Task<bool> UpdateVisitAsync(
		int visitId,
		Guid currentUserId,
		string? chiefComplaint,
		string? notes)
	{
		var visit = await _context.Visits
			.FirstOrDefaultAsync(x =>
				x.Id == visitId &&
				x.Doctor.UserId == currentUserId &&
				x.Doctor.IsActive &&
				x.Doctor.User.IsActive);

		if (visit == null)
		{
			return false;
		}

		visit.ChiefComplaint = chiefComplaint;
		visit.Notes = notes;

		await _context.SaveChangesAsync();

		return true;
	}

	public async Task<Diagnosis?> AddDiagnosisAsync(
		int visitId,
		Guid currentUserId,
		string diagnosisCode,
		string description)
	{
		var visit = await _context.Visits
			.FirstOrDefaultAsync(x =>
				x.Id == visitId &&
				x.Doctor.UserId == currentUserId &&
				x.Doctor.IsActive &&
				x.Doctor.User.IsActive);

		if (visit == null)
		{
			return null;
		}

		var diagnosis = new Diagnosis
		{
			VisitId = visit.Id,
			DiagnosisCode = diagnosisCode,
			Description = description
		};

		_context.Diagnoses.Add(diagnosis);

		await _context.SaveChangesAsync();

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
		var visit = await _context.Visits
			.FirstOrDefaultAsync(x =>
				x.Id == visitId &&
				x.Doctor.UserId == currentUserId &&
				x.Doctor.IsActive &&
				x.Doctor.User.IsActive);

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

		var prescriptionItem = new PrescriptionItem
		{
			Prescription = prescription,
			Medication = medication,
			Dosage = dosage,
			Frequency = frequency,
			Days = days
		};

		_context.PrescriptionItems.Add(prescriptionItem);

		await _context.SaveChangesAsync();

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
}