namespace HospitalInformationSystem.Services;

public interface IAuditService
{
	Task LogAsync(
		Guid? userId,
		string action,
		string entityName,
		string entityId);
}