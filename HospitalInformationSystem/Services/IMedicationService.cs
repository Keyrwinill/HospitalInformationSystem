using HospitalInformationSystem.Models.Constants;

namespace HospitalInformationSystem.Services;

public interface IMedicationService
{
	Task<MedicationOperationResult> DeactivateAsync(
		int medicationId,
		Guid currentUserId);

	Task<MedicationOperationResult> ActivateAsync(
		int medicationId,
		Guid currentUserId);
}