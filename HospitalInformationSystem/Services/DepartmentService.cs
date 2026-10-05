using HospitalInformationSystem.Data;
using HospitalInformationSystem.Models.Constants;
using Microsoft.EntityFrameworkCore;

namespace HospitalInformationSystem.Services;

public class DepartmentService : IDepartmentService
{
	private readonly HospitalDbContext _context;
	private readonly IAuditService _auditService;

	public DepartmentService(
		HospitalDbContext context,
		IAuditService auditService)
	{
		_context = context;
		_auditService = auditService;
	}

	public async Task<DepartmentOperationResult> DeactivateAsync(
		int departmentId,
		Guid currentUserId)
	{
		var department = await _context.Departments
			.FirstOrDefaultAsync(x => x.Id == departmentId);

		if (department == null)
		{
			return DepartmentOperationResult.NotFound;
		}

		if (!department.IsActive)
		{
			return DepartmentOperationResult.Inactive;
		}

		var hasActiveDoctors = await _context.Doctors
			.AnyAsync(x =>
				x.DepartmentId == department.Id &&
				x.IsActive);

		if (hasActiveDoctors)
		{
			return DepartmentOperationResult.HasActiveDoctors;
		}

		department.IsActive = false;

		await _auditService.LogAsync(
			currentUserId,
			"DeactivateDepartment",
			"Department",
			department.Id.ToString());

		await _context.SaveChangesAsync();

		return DepartmentOperationResult.Success;
	}

	public async Task<DepartmentOperationResult> ActivateAsync(
		int departmentId,
		Guid currentUserId)
	{
		var department = await _context.Departments
			.FirstOrDefaultAsync(x => x.Id == departmentId);

		if (department == null)
		{
			return DepartmentOperationResult.NotFound;
		}

		if (department.IsActive)
		{
			return DepartmentOperationResult.AlreadyActive;
		}

		department.IsActive = true;

		await _auditService.LogAsync(
			currentUserId,
			"ActivateDepartment",
			"Department",
			department.Id.ToString());

		await _context.SaveChangesAsync();

		return DepartmentOperationResult.Success;
	}

	public async Task<DepartmentOperationResult> UpdateAsync(
		int departmentId,
		string name,
		string? description,
		Guid currentUserId)
	{
		var department = await _context.Departments
			.FirstOrDefaultAsync(x => x.Id == departmentId);

		if (department == null)
		{
			return DepartmentOperationResult.NotFound;
		}

		if (!department.IsActive)
		{
			return DepartmentOperationResult.Inactive;
		}

		var nameExists = await _context.Departments
			.AnyAsync(x =>
				x.Id != departmentId &&
				x.Name == name);

		if (nameExists)
		{
			return DepartmentOperationResult.DuplicateName;
		}

		department.Name = name;
		department.Description = description;

		await _auditService.LogAsync(
			currentUserId,
			"UpdateDepartment",
			"Department",
			department.Id.ToString());

		await _context.SaveChangesAsync();

		return DepartmentOperationResult.Success;
	}
}