using HospitalInformationSystem.Models.Constants;

namespace HospitalInformationSystem.Services;

public interface IDoctorService
{
	Task<DoctorOperationResult> DeactivateAsync(
		int doctorId,
		Guid currentUserId);

	Task<DoctorOperationResult> ActivateAsync(
		int doctorId,
		Guid currentUserId);

	Task<DoctorOperationResult> UpdateAsync(
		int doctorId,
		string firstName,
		string lastName,
		string email,
		string licenseNumber,
		int departmentId,
		Guid currentUserId);
}