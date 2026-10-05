using HospitalInformationSystem.Data;
using HospitalInformationSystem.Models.Constants;
using HospitalInformationSystem.Models.Entities;
using HospitalInformationSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HospitalInformationSystem.Tests.Services;

public class UserServiceTests
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

	private static UserService CreateService(
		HospitalDbContext context)
	{
		var auditService = new AuditService(context);
		var passwordService = new PasswordService();

		return new UserService(
			context,
			auditService,
			passwordService);
	}

	[Fact]
	public async Task DeactivateAsync_WhenUserDoesNotExist_ReturnsNotFound()
	{
		// Arrange
		await using var context = CreateDbContext();

		var service = CreateService(context);

		// Act
		var result = await service.DeactivateAsync(
			Guid.NewGuid(),
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			UserOperationResult.NotFound,
			result);

		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task DeactivateAsync_WhenUserIsAlreadyInactive_ReturnsInactive()
	{
		// Arrange
		await using var context = CreateDbContext();

		var user = new User
		{
			Id = Guid.NewGuid(),
			Account = "inactiveuser",
			Email = "inactive@hospital.local",
			PasswordHash = "test",
			FirstName = "Inactive",
			LastName = "User",
			Role = UserRoles.Receptionist,
			IsActive = false
		};

		context.Users.Add(user);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.DeactivateAsync(
			user.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			UserOperationResult.Inactive,
			result);

		Assert.False(user.IsActive);
		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task DeactivateAsync_WhenUserIsDoctor_ReturnsDoctorManagedSeparately()
	{
		// Arrange
		await using var context = CreateDbContext();

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

		context.Users.Add(user);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.DeactivateAsync(
			user.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			UserOperationResult.DoctorManagedSeparately,
			result);

		Assert.True(user.IsActive);
		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task DeactivateAsync_WhenDeactivatingSelf_ReturnsCannotDeactivateSelf()
	{
		// Arrange
		await using var context = CreateDbContext();

		var user = new User
		{
			Id = Guid.NewGuid(),
			Account = "admin1",
			Email = "admin1@hospital.local",
			PasswordHash = "test",
			FirstName = "Test",
			LastName = "Admin",
			Role = UserRoles.Admin,
			IsActive = true
		};

		context.Users.Add(user);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.DeactivateAsync(
			user.Id,
			user.Id);

		// Assert
		Assert.Equal(
			UserOperationResult.CannotDeactivateSelf,
			result);

		Assert.True(user.IsActive);
		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task DeactivateAsync_WhenValid_DeactivatesUserAndCreatesAuditLog()
	{
		// Arrange
		await using var context = CreateDbContext();

		var user = new User
		{
			Id = Guid.NewGuid(),
			Account = "receptionist1",
			Email = "receptionist1@hospital.local",
			PasswordHash = "test",
			FirstName = "Test",
			LastName = "Receptionist",
			Role = UserRoles.Receptionist,
			IsActive = true
		};

		context.Users.Add(user);
		await context.SaveChangesAsync();

		var currentUserId = Guid.NewGuid();
		var service = CreateService(context);

		// Act
		var result = await service.DeactivateAsync(
			user.Id,
			currentUserId);

		// Assert
		Assert.Equal(
			UserOperationResult.Success,
			result);

		Assert.False(user.IsActive);

		var auditLog = Assert.Single(context.AuditLogs);

		Assert.Equal(currentUserId, auditLog.UserId);
		Assert.Equal("DeactivateUser", auditLog.Action);
		Assert.Equal("User", auditLog.EntityName);
		Assert.Equal(user.Id.ToString(), auditLog.EntityId);
	}

	[Fact]
	public async Task ActivateAsync_WhenValid_ActivatesUserAndCreatesAuditLog()
	{
		// Arrange
		await using var context = CreateDbContext();

		var user = new User
		{
			Id = Guid.NewGuid(),
			Account = "receptionist1",
			Email = "receptionist1@hospital.local",
			PasswordHash = "test",
			FirstName = "Test",
			LastName = "Receptionist",
			Role = UserRoles.Receptionist,
			IsActive = false
		};

		context.Users.Add(user);
		await context.SaveChangesAsync();

		var currentUserId = Guid.NewGuid();
		var service = CreateService(context);

		// Act
		var result = await service.ActivateAsync(
			user.Id,
			currentUserId);

		// Assert
		Assert.Equal(
			UserOperationResult.Success,
			result);

		Assert.True(user.IsActive);

		var auditLog = Assert.Single(context.AuditLogs);

		Assert.Equal(currentUserId, auditLog.UserId);
		Assert.Equal("ActivateUser", auditLog.Action);
		Assert.Equal("User", auditLog.EntityName);
		Assert.Equal(user.Id.ToString(), auditLog.EntityId);
	}

	[Fact]
	public async Task ActivateAsync_WhenUserIsAlreadyActive_ReturnsAlreadyActive()
	{
		// Arrange
		await using var context = CreateDbContext();

		var user = new User
		{
			Id = Guid.NewGuid(),
			Account = "receptionist1",
			Email = "receptionist1@hospital.local",
			PasswordHash = "test",
			FirstName = "Test",
			LastName = "Receptionist",
			Role = UserRoles.Receptionist,
			IsActive = true
		};

		context.Users.Add(user);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.ActivateAsync(
			user.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			UserOperationResult.AlreadyActive,
			result);

		Assert.True(user.IsActive);
		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task ActivateAsync_WhenUserIsDoctor_ReturnsDoctorManagedSeparately()
	{
		// Arrange
		await using var context = CreateDbContext();

		var user = new User
		{
			Id = Guid.NewGuid(),
			Account = "doctor1",
			Email = "doctor1@hospital.local",
			PasswordHash = "test",
			FirstName = "Test",
			LastName = "Doctor",
			Role = UserRoles.Doctor,
			IsActive = false
		};

		context.Users.Add(user);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.ActivateAsync(
			user.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			UserOperationResult.DoctorManagedSeparately,
			result);

		Assert.False(user.IsActive);
		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task ActivateAsync_WhenUserDoesNotExist_ReturnsNotFound()
	{
		// Arrange
		await using var context = CreateDbContext();

		var service = CreateService(context);

		// Act
		var result = await service.ActivateAsync(
			Guid.NewGuid(),
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			UserOperationResult.NotFound,
			result);

		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task CreateAsync_WhenRoleIsInvalid_ReturnsInvalidRole()
	{
		// Arrange
		await using var context = CreateDbContext();

		var service = CreateService(context);

		// Act
		var result = await service.CreateAsync(
			"doctor1",
			"doctor1@hospital.local",
			"Test",
			"Doctor",
			UserRoles.Doctor,
			"Password123!",
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			UserOperationResult.InvalidRole,
			result);

		Assert.Empty(context.Users);
		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task CreateAsync_WhenAccountAlreadyExists_ReturnsDuplicateAccount()
	{
		// Arrange
		await using var context = CreateDbContext();

		var existingUser = new User
		{
			Id = Guid.NewGuid(),
			Account = "admin1",
			Email = "existing@hospital.local",
			PasswordHash = "test",
			FirstName = "Existing",
			LastName = "User",
			Role = UserRoles.Admin,
			IsActive = true
		};

		context.Users.Add(existingUser);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.CreateAsync(
			"admin1",
			"new@hospital.local",
			"New",
			"User",
			UserRoles.Receptionist,
			"Password123!",
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			UserOperationResult.DuplicateAccount,
			result);

		Assert.Single(context.Users);
		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task CreateAsync_WhenEmailAlreadyExists_ReturnsDuplicateEmail()
	{
		// Arrange
		await using var context = CreateDbContext();

		var existingUser = new User
		{
			Id = Guid.NewGuid(),
			Account = "admin1",
			Email = "existing@hospital.local",
			PasswordHash = "test",
			FirstName = "Existing",
			LastName = "User",
			Role = UserRoles.Admin,
			IsActive = true
		};

		context.Users.Add(existingUser);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.CreateAsync(
			"receptionist1",
			"existing@hospital.local",
			"New",
			"User",
			UserRoles.Receptionist,
			"Password123!",
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			UserOperationResult.DuplicateEmail,
			result);

		Assert.Single(context.Users);
		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task CreateAsync_WhenValid_CreatesUserAndAuditLog()
	{
		// Arrange
		await using var context = CreateDbContext();

		var currentUserId = Guid.NewGuid();
		var service = CreateService(context);

		// Act
		var result = await service.CreateAsync(
			"receptionist1",
			"receptionist1@hospital.local",
			"Test",
			"Receptionist",
			UserRoles.Receptionist,
			"Password123!",
			currentUserId);

		// Assert
		Assert.Equal(
			UserOperationResult.Success,
			result);

		var user = Assert.Single(context.Users);

		Assert.Equal("receptionist1", user.Account);
		Assert.Equal("receptionist1@hospital.local", user.Email);
		Assert.Equal("Test", user.FirstName);
		Assert.Equal("Receptionist", user.LastName);
		Assert.Equal(UserRoles.Receptionist, user.Role);
		Assert.True(user.IsActive);

		Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
		Assert.NotEqual("Password123!", user.PasswordHash);

		var auditLog = Assert.Single(context.AuditLogs);

		Assert.Equal(currentUserId, auditLog.UserId);
		Assert.Equal("CreateUser", auditLog.Action);
		Assert.Equal("User", auditLog.EntityName);
		Assert.Equal(user.Id.ToString(), auditLog.EntityId);
	}
}