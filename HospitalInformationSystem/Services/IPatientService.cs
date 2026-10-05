using HospitalInformationSystem.Models.Constants;

namespace HospitalInformationSystem.Services;

public interface IPatientService
{
	Task<PatientOperationResult> DeactivateAsync(
		int patientId,
		Guid currentUserId);

	Task<PatientOperationResult> ActivateAsync(
		int patientId,
		Guid currentUserId);

	Task<PatientOperationResult> UpdateAsync(
		int patientId,
		string medicalRecordNumber,
		string firstName,
		string lastName,
		DateOnly birthday,
		string gender,
		string? phone,
		string? address,
		Guid currentUserId);
}