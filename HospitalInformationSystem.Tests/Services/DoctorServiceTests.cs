using HospitalInformationSystem.Data;
using HospitalInformationSystem.Models.Constants;
using HospitalInformationSystem.Models.Entities;
using HospitalInformationSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HospitalInformationSystem.Tests.Services;

public class DoctorServiceTests
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

	private static DoctorService CreateService(
		HospitalDbContext context)
	{
		var auditService = new AuditService(context);

		return new DoctorService(
			context,
			auditService);
	}

	[Fact]
	public async Task DeactivateAsync_WhenDoctorDoesNotExist_ReturnsNotFound()
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
			DoctorOperationResult.NotFound,
			result);

		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task DeactivateAsync_WhenDoctorIsAlreadyInactive_ReturnsInactive()
	{
		// Arrange
		await using var context = CreateDbContext();

		var user = new User
		{
			Id = Guid.NewGuid(),
			Account = "doctor1",
			Email = "doctor1@test.com",
			PasswordHash = "test",
			FirstName = "Test",
			LastName = "Doctor",
			Role = UserRoles.Doctor,
			IsActive = false
		};

		var department = new Department
		{
			Name = "Test Department",
			IsActive = true
		};

		var doctor = new Doctor
		{
			User = user,
			Department = department,
			LicenseNumber = "TEST-LICENSE-001",
			IsActive = false
		};

		context.Doctors.Add(doctor);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.DeactivateAsync(
			doctor.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			DoctorOperationResult.Inactive,
			result);

		Assert.False(doctor.IsActive);
		Assert.False(user.IsActive);
		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task DeactivateAsync_WhenDoctorHasFutureScheduledAppointment_ReturnsHasFutureScheduledAppointment()
	{
		// Arrange
		await using var context = CreateDbContext();

		var user = new User
		{
			Id = Guid.NewGuid(),
			Account = "doctor1",
			Email = "doctor1@test.com",
			PasswordHash = "test",
			FirstName = "Test",
			LastName = "Doctor",
			Role = UserRoles.Doctor,
			IsActive = true
		};

		var department = new Department
		{
			Name = "Test Department",
			IsActive = true
		};

		var doctor = new Doctor
		{
			User = user,
			Department = department,
			LicenseNumber = "TEST-LICENSE-001",
			IsActive = true
		};

		var patient = new Patient
		{
			MedicalRecordNumber = "TEST001",
			FirstName = "Test",
			LastName = "Patient",
			Birthday = new DateOnly(1990, 1, 1),
			Gender = "Male",
			IsActive = true
		};

		var appointment = new Appointment
		{
			Patient = patient,
			Doctor = doctor,
			AppointmentDateTime = DateTime.Now.AddDays(1),
			Status = AppointmentStatus.Scheduled
		};

		context.Appointments.Add(appointment);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.DeactivateAsync(
			doctor.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			DoctorOperationResult.HasFutureScheduledAppointment,
			result);

		Assert.True(doctor.IsActive);
		Assert.True(user.IsActive);
		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task DeactivateAsync_WhenDoctorHasActiveVisit_ReturnsHasActiveVisit()
	{
		// Arrange
		await using var context = CreateDbContext();

		var user = new User
		{
			Id = Guid.NewGuid(),
			Account = "doctor1",
			Email = "doctor1@test.com",
			PasswordHash = "test",
			FirstName = "Test",
			LastName = "Doctor",
			Role = UserRoles.Doctor,
			IsActive = true
		};

		var department = new Department
		{
			Name = "Test Department",
			IsActive = true
		};

		var doctor = new Doctor
		{
			User = user,
			Department = department,
			LicenseNumber = "TEST-LICENSE-001",
			IsActive = true
		};

		var patient = new Patient
		{
			MedicalRecordNumber = "TEST001",
			FirstName = "Test",
			LastName = "Patient",
			Birthday = new DateOnly(1990, 1, 1),
			Gender = "Male",
			IsActive = true
		};

		var appointment = new Appointment
		{
			Patient = patient,
			Doctor = doctor,
			AppointmentDateTime = DateTime.Now.AddHours(-1),
			Status = AppointmentStatus.Scheduled
		};

		var visit = new Visit
		{
			Patient = patient,
			Doctor = doctor,
			Appointment = appointment,
			VisitDateTime = DateTime.Now,
			CreatedAt = DateTime.Now
		};

		context.Visits.Add(visit);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.DeactivateAsync(
			doctor.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			DoctorOperationResult.HasActiveVisit,
			result);

		Assert.True(doctor.IsActive);
		Assert.True(user.IsActive);
		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task DeactivateAsync_WhenDoctorCanBeDeactivated_DeactivatesDoctorAndUserAndCreatesAuditLog()
	{
		// Arrange
		await using var context = CreateDbContext();

		var user = new User
		{
			Id = Guid.NewGuid(),
			Account = "doctor1",
			Email = "doctor1@test.com",
			PasswordHash = "test",
			FirstName = "Test",
			LastName = "Doctor",
			Role = UserRoles.Doctor,
			IsActive = true
		};

		var department = new Department
		{
			Name = "Test Department",
			IsActive = true
		};

		var doctor = new Doctor
		{
			User = user,
			Department = department,
			LicenseNumber = "TEST-LICENSE-001",
			IsActive = true
		};

		context.Doctors.Add(doctor);
		await context.SaveChangesAsync();

		var currentUserId = Guid.NewGuid();
		var service = CreateService(context);

		// Act
		var result = await service.DeactivateAsync(
			doctor.Id,
			currentUserId);

		// Assert
		Assert.Equal(
			DoctorOperationResult.Success,
			result);

		Assert.False(doctor.IsActive);
		Assert.False(user.IsActive);

		var auditLog = await context.AuditLogs.SingleAsync();

		Assert.Equal(currentUserId, auditLog.UserId);
		Assert.Equal("DeactivateDoctor", auditLog.Action);
		Assert.Equal("Doctor", auditLog.EntityName);
		Assert.Equal(doctor.Id.ToString(), auditLog.EntityId);
	}

	[Fact]
	public async Task ActivateAsync_WhenDepartmentIsInactive_ReturnsInactiveDepartment()
	{
		// Arrange
		await using var context = CreateDbContext();

		var user = new User
		{
			Id = Guid.NewGuid(),
			Account = "doctor1",
			Email = "doctor1@test.com",
			PasswordHash = "test",
			FirstName = "Test",
			LastName = "Doctor",
			Role = UserRoles.Doctor,
			IsActive = false
		};

		var department = new Department
		{
			Name = "Test Department",
			IsActive = false
		};

		var doctor = new Doctor
		{
			User = user,
			Department = department,
			LicenseNumber = "TEST-LICENSE-001",
			IsActive = false
		};

		context.Doctors.Add(doctor);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.ActivateAsync(
			doctor.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			DoctorOperationResult.InactiveDepartment,
			result);

		Assert.False(doctor.IsActive);
		Assert.False(user.IsActive);
		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task ActivateAsync_WhenDoctorCanBeActivated_ActivatesDoctorAndUserAndCreatesAuditLog()
	{
		// Arrange
		await using var context = CreateDbContext();

		var user = new User
		{
			Id = Guid.NewGuid(),
			Account = "doctor1",
			Email = "doctor1@test.com",
			PasswordHash = "test",
			FirstName = "Test",
			LastName = "Doctor",
			Role = UserRoles.Doctor,
			IsActive = false
		};

		var department = new Department
		{
			Name = "Test Department",
			IsActive = true
		};

		var doctor = new Doctor
		{
			User = user,
			Department = department,
			LicenseNumber = "TEST-LICENSE-001",
			IsActive = false
		};

		context.Doctors.Add(doctor);
		await context.SaveChangesAsync();

		var currentUserId = Guid.NewGuid();
		var service = CreateService(context);

		// Act
		var result = await service.ActivateAsync(
			doctor.Id,
			currentUserId);

		// Assert
		Assert.Equal(
			DoctorOperationResult.Success,
			result);

		Assert.True(doctor.IsActive);
		Assert.True(user.IsActive);

		var auditLog = await context.AuditLogs.SingleAsync();

		Assert.Equal(currentUserId, auditLog.UserId);
		Assert.Equal("ActivateDoctor", auditLog.Action);
		Assert.Equal("Doctor", auditLog.EntityName);
		Assert.Equal(doctor.Id.ToString(), auditLog.EntityId);
	}

	[Fact]
	public async Task ActivateAsync_WhenDoctorIsAlreadyActive_ReturnsAlreadyActive()
	{
		// Arrange
		await using var context = CreateDbContext();

		var user = new User
		{
			Id = Guid.NewGuid(),
			Account = "doctor1",
			Email = "doctor1@test.com",
			PasswordHash = "test",
			FirstName = "Test",
			LastName = "Doctor",
			Role = UserRoles.Doctor,
			IsActive = true
		};

		var department = new Department
		{
			Name = "Test Department",
			IsActive = true
		};

		var doctor = new Doctor
		{
			User = user,
			Department = department,
			LicenseNumber = "TEST-LICENSE-001",
			IsActive = true
		};

		context.Doctors.Add(doctor);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.ActivateAsync(
			doctor.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			DoctorOperationResult.AlreadyActive,
			result);

		Assert.True(doctor.IsActive);
		Assert.True(user.IsActive);
		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task ActivateAsync_WhenDoctorDoesNotExist_ReturnsNotFound()
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
			DoctorOperationResult.NotFound,
			result);

		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task UpdateAsync_WhenDoctorIsValid_UpdatesDoctorAndUserAndCreatesAuditLog()
	{
		// Arrange
		await using var context = CreateDbContext();

		var user = new User
		{
			Id = Guid.NewGuid(),
			Account = "doctor1",
			Email = "old@test.com",
			PasswordHash = "test",
			FirstName = "Old",
			LastName = "Name",
			Role = UserRoles.Doctor,
			IsActive = true
		};

		var oldDepartment = new Department
		{
			Name = "Old Department",
			IsActive = true
		};

		var newDepartment = new Department
		{
			Name = "New Department",
			IsActive = true
		};

		var doctor = new Doctor
		{
			User = user,
			Department = oldDepartment,
			LicenseNumber = "OLD-LICENSE",
			IsActive = true
		};

		context.Doctors.Add(doctor);
		context.Departments.Add(newDepartment);
		await context.SaveChangesAsync();

		var currentUserId = Guid.NewGuid();
		var service = CreateService(context);

		// Act
		var result = await service.UpdateAsync(
			doctor.Id,
			"New",
			"Name",
			"new@test.com",
			"NEW-LICENSE",
			newDepartment.Id,
			currentUserId);

		// Assert
		Assert.Equal(
			DoctorOperationResult.Success,
			result);

		Assert.Equal("New", user.FirstName);
		Assert.Equal("Name", user.LastName);
		Assert.Equal("new@test.com", user.Email);
		Assert.Equal("NEW-LICENSE", doctor.LicenseNumber);
		Assert.Equal(newDepartment.Id, doctor.DepartmentId);

		var auditLog = await context.AuditLogs.SingleAsync();

		Assert.Equal(currentUserId, auditLog.UserId);
		Assert.Equal("UpdateDoctor", auditLog.Action);
		Assert.Equal("Doctor", auditLog.EntityName);
		Assert.Equal(doctor.Id.ToString(), auditLog.EntityId);
	}

	[Fact]
	public async Task UpdateAsync_WhenDoctorIsInactive_ReturnsInactive()
	{
		// Arrange
		await using var context = CreateDbContext();

		var user = new User
		{
			Id = Guid.NewGuid(),
			Account = "doctor1",
			Email = "doctor1@test.com",
			PasswordHash = "test",
			FirstName = "Test",
			LastName = "Doctor",
			Role = UserRoles.Doctor,
			IsActive = false
		};

		var department = new Department
		{
			Name = "Test Department",
			IsActive = true
		};

		var doctor = new Doctor
		{
			User = user,
			Department = department,
			LicenseNumber = "OLD-LICENSE",
			IsActive = false
		};

		context.Doctors.Add(doctor);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.UpdateAsync(
			doctor.Id,
			"New",
			"Name",
			"new@test.com",
			"NEW-LICENSE",
			department.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			DoctorOperationResult.Inactive,
			result);

		Assert.Equal("Test", user.FirstName);
		Assert.Equal("doctor1@test.com", user.Email);
		Assert.Equal("OLD-LICENSE", doctor.LicenseNumber);
		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task UpdateAsync_WhenDoctorIsActiveButUserIsInactive_ReturnsInactive()
	{
		// Arrange
		await using var context = CreateDbContext();

		var user = new User
		{
			Id = Guid.NewGuid(),
			Account = "doctor1",
			Email = "doctor1@test.com",
			PasswordHash = "test",
			FirstName = "Test",
			LastName = "Doctor",
			Role = UserRoles.Doctor,
			IsActive = false
		};

		var department = new Department
		{
			Name = "Test Department",
			IsActive = true
		};

		var doctor = new Doctor
		{
			User = user,
			Department = department,
			LicenseNumber = "OLD-LICENSE",
			IsActive = true
		};

		context.Doctors.Add(doctor);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.UpdateAsync(
			doctor.Id,
			"New",
			"Name",
			"new@test.com",
			"NEW-LICENSE",
			department.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			DoctorOperationResult.Inactive,
			result);

		Assert.Equal("Test", user.FirstName);
		Assert.Equal("doctor1@test.com", user.Email);
		Assert.Equal("OLD-LICENSE", doctor.LicenseNumber);
		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task UpdateAsync_WhenDepartmentIsInactive_ReturnsInactiveDepartment()
	{
		// Arrange
		await using var context = CreateDbContext();

		var user = new User
		{
			Id = Guid.NewGuid(),
			Account = "doctor1",
			Email = "doctor1@test.com",
			PasswordHash = "test",
			FirstName = "Test",
			LastName = "Doctor",
			Role = UserRoles.Doctor,
			IsActive = true
		};

		var currentDepartment = new Department
		{
			Name = "Current Department",
			IsActive = true
		};

		var inactiveDepartment = new Department
		{
			Name = "Inactive Department",
			IsActive = false
		};

		var doctor = new Doctor
		{
			User = user,
			Department = currentDepartment,
			LicenseNumber = "OLD-LICENSE",
			IsActive = true
		};

		context.Doctors.Add(doctor);
		context.Departments.Add(inactiveDepartment);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.UpdateAsync(
			doctor.Id,
			"New",
			"Name",
			"new@test.com",
			"NEW-LICENSE",
			inactiveDepartment.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			DoctorOperationResult.InactiveDepartment,
			result);

		Assert.Equal(currentDepartment.Id, doctor.DepartmentId);
		Assert.Equal("doctor1@test.com", user.Email);
		Assert.Equal("OLD-LICENSE", doctor.LicenseNumber);
		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task UpdateAsync_WhenEmailIsDuplicate_ReturnsDuplicateEmail()
	{
		// Arrange
		await using var context = CreateDbContext();

		var department = new Department
		{
			Name = "Test Department",
			IsActive = true
		};

		var existingUser = new User
		{
			Id = Guid.NewGuid(),
			Account = "doctor2",
			Email = "existing@test.com",
			PasswordHash = "test",
			FirstName = "Existing",
			LastName = "Doctor",
			Role = UserRoles.Doctor,
			IsActive = true
		};

		var user = new User
		{
			Id = Guid.NewGuid(),
			Account = "doctor1",
			Email = "doctor1@test.com",
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
			LicenseNumber = "LICENSE-001",
			IsActive = true
		};

		context.Users.Add(existingUser);
		context.Doctors.Add(doctor);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.UpdateAsync(
			doctor.Id,
			"New",
			"Name",
			"existing@test.com",
			"NEW-LICENSE",
			department.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			DoctorOperationResult.DuplicateEmail,
			result);

		Assert.Equal("doctor1@test.com", user.Email);
		Assert.Equal("LICENSE-001", doctor.LicenseNumber);
		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task UpdateAsync_WhenLicenseNumberIsDuplicate_ReturnsDuplicateLicenseNumber()
	{
		// Arrange
		await using var context = CreateDbContext();

		var department = new Department
		{
			Name = "Test Department",
			IsActive = true
		};

		var user1 = new User
		{
			Id = Guid.NewGuid(),
			Account = "doctor1",
			Email = "doctor1@test.com",
			PasswordHash = "test",
			FirstName = "First",
			LastName = "Doctor",
			Role = UserRoles.Doctor,
			IsActive = true
		};

		var user2 = new User
		{
			Id = Guid.NewGuid(),
			Account = "doctor2",
			Email = "doctor2@test.com",
			PasswordHash = "test",
			FirstName = "Second",
			LastName = "Doctor",
			Role = UserRoles.Doctor,
			IsActive = true
		};

		var doctor1 = new Doctor
		{
			User = user1,
			Department = department,
			LicenseNumber = "LICENSE-001",
			IsActive = true
		};

		var doctor2 = new Doctor
		{
			User = user2,
			Department = department,
			LicenseNumber = "LICENSE-002",
			IsActive = true
		};

		context.Doctors.AddRange(doctor1, doctor2);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.UpdateAsync(
			doctor2.Id,
			"Updated",
			"Doctor",
			"updated@test.com",
			"LICENSE-001",
			department.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			DoctorOperationResult.DuplicateLicenseNumber,
			result);

		Assert.Equal("LICENSE-002", doctor2.LicenseNumber);
		Assert.Equal("doctor2@test.com", user2.Email);
		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task UpdateAsync_WhenDoctorDoesNotExist_ReturnsNotFound()
	{
		// Arrange
		await using var context = CreateDbContext();

		var service = CreateService(context);

		// Act
		var result = await service.UpdateAsync(
			999,
			"Test",
			"Doctor",
			"doctor@test.com",
			"LICENSE-001",
			1,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			DoctorOperationResult.NotFound,
			result);

		Assert.Empty(context.AuditLogs);
	}
}