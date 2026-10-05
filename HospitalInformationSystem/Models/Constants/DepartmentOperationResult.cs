namespace HospitalInformationSystem.Models.Constants;

public enum DepartmentOperationResult
{
	Success = 1,
	NotFound = 2,
	Inactive = 3,
	AlreadyActive = 4,
	HasActiveDoctors = 5,
	DuplicateName = 6
}