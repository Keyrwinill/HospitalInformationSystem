using HospitalInformationSystem.Models.Constants;

namespace HospitalInformationSystem.Services;

public interface IDepartmentService
{
	Task<DepartmentOperationResult> DeactivateAsync(
		int departmentId,
		Guid currentUserId);

	Task<DepartmentOperationResult> ActivateAsync(
		int departmentId,
		Guid currentUserId);

	Task<DepartmentOperationResult> UpdateAsync(
		int departmentId,
		string name,
		string? description,
		Guid currentUserId);
}