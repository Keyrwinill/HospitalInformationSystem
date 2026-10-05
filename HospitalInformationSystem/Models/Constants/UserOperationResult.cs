namespace HospitalInformationSystem.Models.Constants;

public enum UserOperationResult
{
	Success = 1,
	NotFound = 2,
	Inactive = 3,
	AlreadyActive = 4,
	DoctorManagedSeparately = 5,
	CannotDeactivateSelf = 6,
	InvalidRole = 7,
	DuplicateAccount = 8,
	DuplicateEmail = 9
}