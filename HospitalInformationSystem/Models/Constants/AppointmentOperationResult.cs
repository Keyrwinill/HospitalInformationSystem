namespace HospitalInformationSystem.Models.Constants;

public enum AppointmentOperationResult
{
	Success = 1,
	NotFound = 2,
	InvalidState = 3,
	PastDateTime = 4,
	ScheduleConflict = 5,
	FutureDateTime = 6,
	InvalidPatient = 7,
	InvalidDoctor = 8
}