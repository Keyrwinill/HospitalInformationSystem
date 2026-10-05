namespace HospitalInformationSystem.Models.Constants;

public enum PatientOperationResult
{
	Success = 1,
	NotFound = 2,
	DuplicateMedicalRecordNumber = 3,
	Inactive = 4,
	AlreadyActive = 5,
	HasFutureScheduledAppointment = 6,
	HasActiveVisit = 7
}