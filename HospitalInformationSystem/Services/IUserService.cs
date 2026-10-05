using HospitalInformationSystem.Models.Constants;

namespace HospitalInformationSystem.Services;

public interface IUserService
{
	Task<UserOperationResult> DeactivateAsync(
		Guid userId,
		Guid currentUserId);

	Task<UserOperationResult> ActivateAsync(
		Guid userId,
		Guid currentUserId);

	Task<UserOperationResult> CreateAsync(
		string account,
		string email,
		string firstName,
		string lastName,
		string role,
		string password,
		Guid currentUserId);
}