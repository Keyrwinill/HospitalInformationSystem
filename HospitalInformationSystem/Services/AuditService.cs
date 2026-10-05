using HospitalInformationSystem.Data;
using HospitalInformationSystem.Models.Entities;

namespace HospitalInformationSystem.Services;

public class AuditService : IAuditService
{
	private readonly HospitalDbContext _context;

	public AuditService(HospitalDbContext context)
	{
		_context = context;
	}

	public Task LogAsync(
		Guid? userId,
		string action,
		string entityName,
		string entityId)
	{
		var auditLog = new AuditLog
		{
			UserId = userId,
			Action = action,
			EntityName = entityName,
			EntityId = entityId
		};

		_context.AuditLogs.Add(auditLog);

		return Task.CompletedTask;
	}
}