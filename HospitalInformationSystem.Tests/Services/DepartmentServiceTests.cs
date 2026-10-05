using HospitalInformationSystem.Data;
using HospitalInformationSystem.Models.Constants;
using HospitalInformationSystem.Models.Entities;
using HospitalInformationSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HospitalInformationSystem.Tests.Services;

public class DepartmentServiceTests
{
	private static HospitalDbContext CreateDbContext()
	{
		var options = new DbContextOptionsBuilder<HospitalDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(w =>
				w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
			.Options;

		return new HospitalDbContext(options);
	}

	private static DepartmentService CreateService(
		HospitalDbContext context)
	{
		var auditService = new AuditService(context);

		return new DepartmentService(
			context,
			auditService);
	}

	[Fact]
	public async Task DeactivateAsync_WhenDepartmentDoesNotExist_ReturnsNotFound()
	{
		// Arrange
		await using var context = CreateDbContext();

		var service = CreateService(context);

		// Act
		var result = await service.DeactivateAsync(
			999,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			DepartmentOperationResult.NotFound,
			result);

		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task DeactivateAsync_WhenDepartmentIsAlreadyInactive_ReturnsInactive()
	{
		// Arrange
		await using var context = CreateDbContext();

		var department = new Department
		{
			Name = "Inactive Department",
			IsActive = false
		};

		context.Departments.Add(department);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.DeactivateAsync(
			department.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			DepartmentOperationResult.Inactive,
			result);

		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task DeactivateAsync_WhenDepartmentHasActiveDoctor_ReturnsHasActiveDoctors()
	{
		// Arrange
		await using var context = CreateDbContext();

		var department = new Department
		{
			Name = "Cardiology",
			IsActive = true
		};

		var user = new User
		{
			Id = Guid.NewGuid(),
			Account = "doctor1",
			Email = "doctor1@hospital.local",
			PasswordHash = "test",
			FirstName = "Test",
			LastName = "Doctor",
			Role = UserRoles.Doctor,
			IsActive = true
		};

		var doctor = new Doctor
		{
			User = user,
			Department = department,
			LicenseNumber = "DOC001",
			IsActive = true
		};

		context.Doctors.Add(doctor);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.DeactivateAsync(
			department.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			DepartmentOperationResult.HasActiveDoctors,
			result);

		Assert.True(department.IsActive);
		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task DeactivateAsync_WhenValid_DeactivatesDepartmentAndCreatesAuditLog()
	{
		// Arrange
		await using var context = CreateDbContext();

		var department = new Department
		{
			Name = "Cardiology",
			IsActive = true
		};

		context.Departments.Add(department);
		await context.SaveChangesAsync();

		var currentUserId = Guid.NewGuid();
		var service = CreateService(context);

		// Act
		var result = await service.DeactivateAsync(
			department.Id,
			currentUserId);

		// Assert
		Assert.Equal(
			DepartmentOperationResult.Success,
			result);

		Assert.False(department.IsActive);

		var auditLog = Assert.Single(context.AuditLogs);

		Assert.Equal(currentUserId, auditLog.UserId);
		Assert.Equal("DeactivateDepartment", auditLog.Action);
		Assert.Equal("Department", auditLog.EntityName);
		Assert.Equal(department.Id.ToString(), auditLog.EntityId);
	}

	[Fact]
	public async Task ActivateAsync_WhenValid_ActivatesDepartmentAndCreatesAuditLog()
	{
		// Arrange
		await using var context = CreateDbContext();

		var department = new Department
		{
			Name = "Cardiology",
			IsActive = false
		};

		context.Departments.Add(department);
		await context.SaveChangesAsync();

		var currentUserId = Guid.NewGuid();
		var service = CreateService(context);

		// Act
		var result = await service.ActivateAsync(
			department.Id,
			currentUserId);

		// Assert
		Assert.Equal(
			DepartmentOperationResult.Success,
			result);

		Assert.True(department.IsActive);

		var auditLog = Assert.Single(context.AuditLogs);

		Assert.Equal(currentUserId, auditLog.UserId);
		Assert.Equal("ActivateDepartment", auditLog.Action);
		Assert.Equal("Department", auditLog.EntityName);
		Assert.Equal(department.Id.ToString(), auditLog.EntityId);
	}

	[Fact]
	public async Task ActivateAsync_WhenDepartmentIsAlreadyActive_ReturnsAlreadyActive()
	{
		// Arrange
		await using var context = CreateDbContext();

		var department = new Department
		{
			Name = "Cardiology",
			IsActive = true
		};

		context.Departments.Add(department);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.ActivateAsync(
			department.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			DepartmentOperationResult.AlreadyActive,
			result);

		Assert.True(department.IsActive);
		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task ActivateAsync_WhenDepartmentDoesNotExist_ReturnsNotFound()
	{
		// Arrange
		await using var context = CreateDbContext();

		var service = CreateService(context);

		// Act
		var result = await service.ActivateAsync(
			999,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			DepartmentOperationResult.NotFound,
			result);

		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task UpdateAsync_WhenValid_UpdatesDepartmentAndCreatesAuditLog()
	{
		// Arrange
		await using var context = CreateDbContext();

		var department = new Department
		{
			Name = "Old Name",
			Description = "Old Description",
			IsActive = true
		};

		context.Departments.Add(department);
		await context.SaveChangesAsync();

		var currentUserId = Guid.NewGuid();
		var service = CreateService(context);

		// Act
		var result = await service.UpdateAsync(
			department.Id,
			"New Name",
			"New Description",
			currentUserId);

		// Assert
		Assert.Equal(
			DepartmentOperationResult.Success,
			result);

		Assert.Equal("New Name", department.Name);
		Assert.Equal("New Description", department.Description);

		var auditLog = Assert.Single(context.AuditLogs);

		Assert.Equal(currentUserId, auditLog.UserId);
		Assert.Equal("UpdateDepartment", auditLog.Action);
		Assert.Equal("Department", auditLog.EntityName);
		Assert.Equal(department.Id.ToString(), auditLog.EntityId);
	}

	[Fact]
	public async Task UpdateAsync_WhenDepartmentIsInactive_ReturnsInactive()
	{
		// Arrange
		await using var context = CreateDbContext();

		var department = new Department
		{
			Name = "Cardiology",
			IsActive = false
		};

		context.Departments.Add(department);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.UpdateAsync(
			department.Id,
			"New Cardiology",
			"New Description",
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			DepartmentOperationResult.Inactive,
			result);

		Assert.Equal("Cardiology", department.Name);
		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task UpdateAsync_WhenNameAlreadyExists_ReturnsDuplicateName()
	{
		// Arrange
		await using var context = CreateDbContext();

		var department1 = new Department
		{
			Name = "Cardiology",
			IsActive = true
		};

		var department2 = new Department
		{
			Name = "Neurology",
			IsActive = true
		};

		context.Departments.AddRange(
			department1,
			department2);

		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.UpdateAsync(
			department2.Id,
			"Cardiology",
			"New Description",
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			DepartmentOperationResult.DuplicateName,
			result);

		Assert.Equal("Neurology", department2.Name);
		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task UpdateAsync_WhenDepartmentDoesNotExist_ReturnsNotFound()
	{
		// Arrange
		await using var context = CreateDbContext();

		var service = CreateService(context);

		// Act
		var result = await service.UpdateAsync(
			999,
			"Cardiology",
			"Description",
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			DepartmentOperationResult.NotFound,
			result);

		Assert.Empty(context.AuditLogs);
	}
}