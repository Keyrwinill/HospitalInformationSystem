namespace HospitalInformationSystem.Models.Constants;

public enum DoctorOperationResult
{
	Success = 1,
	NotFound = 2,
	Inactive = 3,
	AlreadyActive = 4,
	InactiveDepartment = 5,
	HasFutureScheduledAppointment = 6,
	HasActiveVisit = 7,
	DuplicateEmail = 8,
	DuplicateLicenseNumber = 9
}