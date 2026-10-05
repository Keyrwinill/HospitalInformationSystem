using HospitalInformationSystem.Models.Constants;
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

	Task<VisitOperationResult> UpdateVisitAsync(
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

	Task<VisitOperationResult> CompleteVisitAsync(
		int visitId,
		Guid currentUserId);

	Task<Diagnosis?> GetDiagnosisForEditAsync(
		int diagnosisId,
		Guid currentUserId);

	Task<int?> UpdateDiagnosisAsync(
		int diagnosisId,
		Guid currentUserId,
		string diagnosisCode,
		string description);

	Task<int?> DeleteDiagnosisAsync(
		int diagnosisId,
		Guid currentUserId);

	Task<PrescriptionItem?> GetPrescriptionItemForEditAsync(
		int prescriptionItemId,
		Guid currentUserId);

	Task<int?> UpdatePrescriptionItemAsync(
		int prescriptionItemId,
		Guid currentUserId,
		string dosage,
		string frequency,
		int days);

	Task<int?> DeletePrescriptionItemAsync(
		int prescriptionItemId,
		Guid currentUserId);
}