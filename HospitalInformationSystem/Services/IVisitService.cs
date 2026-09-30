using HospitalInformationSystem.Models.Entities;

namespace HospitalInformationSystem.Services;

public interface IVisitService
{
	Task<List<Appointment>> GetAppointmentsForDoctorAsync(
		Guid currentUserId);

	Task<Visit?> CreateFromAppointmentAsync(
		int appointmentId,
		Guid currentUserId);

	Task<Visit?> GetVisitForDoctorAsync(
		int visitId,
		Guid currentUserId);

	Task<bool> UpdateVisitAsync(
		int visitId,
		Guid currentUserId,
		string? chiefComplaint,
		string? notes);

	Task<Diagnosis?> AddDiagnosisAsync(
		int visitId,
		Guid currentUserId,
		string diagnosisCode,
		string description);

	Task<PrescriptionItem?> AddPrescriptionItemAsync(
		int visitId,
		Guid currentUserId,
		int medicationId,
		string dosage,
		string frequency,
		int days);

	Task<List<Medication>> GetActiveMedicationsAsync();
}