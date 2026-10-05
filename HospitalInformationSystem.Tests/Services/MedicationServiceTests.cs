using HospitalInformationSystem.Data;
using HospitalInformationSystem.Models.Constants;
using HospitalInformationSystem.Models.Entities;
using HospitalInformationSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HospitalInformationSystem.Tests.Services;

public class MedicationServiceTests
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

	private static MedicationService CreateService(
		HospitalDbContext context)
	{
		var auditService = new AuditService(context);

		return new MedicationService(
			context,
			auditService);
	}

	[Fact]
	public async Task DeactivateAsync_WhenMedicationDoesNotExist_ReturnsNotFound()
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
			MedicationOperationResult.NotFound,
			result);

		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task DeactivateAsync_WhenMedicationIsAlreadyInactive_ReturnsInactive()
	{
		// Arrange
		await using var context = CreateDbContext();

		var medication = new Medication
		{
			Code = "MED001",
			Name = "Test Medication",
			Unit = "Tablet",
			IsActive = false
		};

		context.Medications.Add(medication);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.DeactivateAsync(
			medication.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			MedicationOperationResult.Inactive,
			result);

		Assert.False(medication.IsActive);
		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task DeactivateAsync_WhenValid_DeactivatesMedicationAndCreatesAuditLog()
	{
		// Arrange
		await using var context = CreateDbContext();

		var medication = new Medication
		{
			Code = "MED001",
			Name = "Test Medication",
			Unit = "Tablet",
			IsActive = true
		};

		context.Medications.Add(medication);
		await context.SaveChangesAsync();

		var currentUserId = Guid.NewGuid();
		var service = CreateService(context);

		// Act
		var result = await service.DeactivateAsync(
			medication.Id,
			currentUserId);

		// Assert
		Assert.Equal(
			MedicationOperationResult.Success,
			result);

		Assert.False(medication.IsActive);

		var auditLog = Assert.Single(context.AuditLogs);

		Assert.Equal(currentUserId, auditLog.UserId);
		Assert.Equal("DeactivateMedication", auditLog.Action);
		Assert.Equal("Medication", auditLog.EntityName);
		Assert.Equal(medication.Id.ToString(), auditLog.EntityId);
	}

	[Fact]
	public async Task ActivateAsync_WhenValid_ActivatesMedicationAndCreatesAuditLog()
	{
		// Arrange
		await using var context = CreateDbContext();

		var medication = new Medication
		{
			Code = "MED001",
			Name = "Test Medication",
			Unit = "Tablet",
			IsActive = false
		};

		context.Medications.Add(medication);
		await context.SaveChangesAsync();

		var currentUserId = Guid.NewGuid();
		var service = CreateService(context);

		// Act
		var result = await service.ActivateAsync(
			medication.Id,
			currentUserId);

		// Assert
		Assert.Equal(
			MedicationOperationResult.Success,
			result);

		Assert.True(medication.IsActive);

		var auditLog = Assert.Single(context.AuditLogs);

		Assert.Equal(currentUserId, auditLog.UserId);
		Assert.Equal("ActivateMedication", auditLog.Action);
		Assert.Equal("Medication", auditLog.EntityName);
		Assert.Equal(medication.Id.ToString(), auditLog.EntityId);
	}

	[Fact]
	public async Task ActivateAsync_WhenMedicationIsAlreadyActive_ReturnsAlreadyActive()
	{
		// Arrange
		await using var context = CreateDbContext();

		var medication = new Medication
		{
			Code = "MED001",
			Name = "Test Medication",
			Unit = "Tablet",
			IsActive = true
		};

		context.Medications.Add(medication);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.ActivateAsync(
			medication.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			MedicationOperationResult.AlreadyActive,
			result);

		Assert.True(medication.IsActive);
		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task ActivateAsync_WhenMedicationDoesNotExist_ReturnsNotFound()
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
			MedicationOperationResult.NotFound,
			result);

		Assert.Empty(context.AuditLogs);
	}
}