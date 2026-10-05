using HospitalInformationSystem.Data;
using HospitalInformationSystem.Models.Constants;
using HospitalInformationSystem.Models.Entities;
using HospitalInformationSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HospitalInformationSystem.Tests.Services;

public class PatientServiceTests
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

	private static PatientService CreateService(
		HospitalDbContext context)
	{
		var auditService = new AuditService(context);

		return new PatientService(
			context,
			auditService);
	}

	[Fact]
	public async Task DeactivateAsync_WhenPatientDoesNotExist_ReturnsNotFound()
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
			PatientOperationResult.NotFound,
			result);
	}

	[Fact]
	public async Task DeactivateAsync_WhenPatientIsAlreadyInactive_ReturnsInactive()
	{
		// Arrange
		await using var context = CreateDbContext();

		var patient = new Patient
		{
			MedicalRecordNumber = "TEST001",
			FirstName = "Test",
			LastName = "Patient",
			Birthday = new DateOnly(1990, 1, 1),
			Gender = "Male",
			IsActive = false
		};

		context.Patients.Add(patient);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.DeactivateAsync(
			patient.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			PatientOperationResult.Inactive,
			result);

		Assert.False(patient.IsActive);
	}

	[Fact]
	public async Task DeactivateAsync_WhenPatientHasFutureScheduledAppointment_ReturnsHasFutureScheduledAppointment()
	{
		// Arrange
		await using var context = CreateDbContext();

		var patient = new Patient
		{
			MedicalRecordNumber = "TEST001",
			FirstName = "Test",
			LastName = "Patient",
			Birthday = new DateOnly(1990, 1, 1),
			Gender = "Male",
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
			patient.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			PatientOperationResult.HasFutureScheduledAppointment,
			result);

		Assert.True(patient.IsActive);
	}

	[Fact]
	public async Task DeactivateAsync_WhenPatientHasActiveVisit_ReturnsHasActiveVisit()
	{
		// Arrange
		await using var context = CreateDbContext();

		var patient = new Patient
		{
			MedicalRecordNumber = "TEST001",
			FirstName = "Test",
			LastName = "Patient",
			Birthday = new DateOnly(1990, 1, 1),
			Gender = "Male",
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

		// Use a past appointment so the future-appointment rule
		// does not trigger before the active-visit rule.
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
			patient.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			PatientOperationResult.HasActiveVisit,
			result);

		Assert.True(patient.IsActive);
	}

	[Fact]
	public async Task DeactivateAsync_WhenPatientCanBeDeactivated_DeactivatesPatientAndCreatesAuditLog()
	{
		// Arrange
		await using var context = CreateDbContext();

		var patient = new Patient
		{
			MedicalRecordNumber = "TEST001",
			FirstName = "Test",
			LastName = "Patient",
			Birthday = new DateOnly(1990, 1, 1),
			Gender = "Male",
			IsActive = true
		};

		context.Patients.Add(patient);
		await context.SaveChangesAsync();

		var currentUserId = Guid.NewGuid();

		var service = CreateService(context);

		// Act
		var result = await service.DeactivateAsync(
			patient.Id,
			currentUserId);

		// Assert
		Assert.Equal(
			PatientOperationResult.Success,
			result);

		Assert.False(patient.IsActive);

		var auditLog = await context.AuditLogs.SingleAsync();

		Assert.Equal(currentUserId, auditLog.UserId);
		Assert.Equal("DeactivatePatient", auditLog.Action);
		Assert.Equal("Patient", auditLog.EntityName);
		Assert.Equal(patient.Id.ToString(), auditLog.EntityId);
	}

	[Fact]
	public async Task ActivateAsync_WhenPatientIsInactive_ActivatesPatientAndCreatesAuditLog()
	{
		// Arrange
		await using var context = CreateDbContext();

		var patient = new Patient
		{
			MedicalRecordNumber = "TEST001",
			FirstName = "Test",
			LastName = "Patient",
			Birthday = new DateOnly(1990, 1, 1),
			Gender = "Male",
			IsActive = false
		};

		context.Patients.Add(patient);
		await context.SaveChangesAsync();

		var currentUserId = Guid.NewGuid();

		var service = CreateService(context);

		// Act
		var result = await service.ActivateAsync(
			patient.Id,
			currentUserId);

		// Assert
		Assert.Equal(
			PatientOperationResult.Success,
			result);

		Assert.True(patient.IsActive);

		var auditLog = await context.AuditLogs.SingleAsync();

		Assert.Equal(currentUserId, auditLog.UserId);
		Assert.Equal("ActivatePatient", auditLog.Action);
		Assert.Equal("Patient", auditLog.EntityName);
		Assert.Equal(patient.Id.ToString(), auditLog.EntityId);
	}

	[Fact]
	public async Task ActivateAsync_WhenPatientIsAlreadyActive_ReturnsAlreadyActive()
	{
		// Arrange
		await using var context = CreateDbContext();

		var patient = new Patient
		{
			MedicalRecordNumber = "TEST001",
			FirstName = "Test",
			LastName = "Patient",
			Birthday = new DateOnly(1990, 1, 1),
			Gender = "Male",
			IsActive = true
		};

		context.Patients.Add(patient);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.ActivateAsync(
			patient.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			PatientOperationResult.AlreadyActive,
			result);

		Assert.True(patient.IsActive);
		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task ActivateAsync_WhenPatientDoesNotExist_ReturnsNotFound()
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
			PatientOperationResult.NotFound,
			result);

		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task UpdateAsync_WhenPatientIsValid_UpdatesPatientAndCreatesAuditLog()
	{
		// Arrange
		await using var context = CreateDbContext();

		var patient = new Patient
		{
			MedicalRecordNumber = "OLD001",
			FirstName = "Old",
			LastName = "Name",
			Birthday = new DateOnly(1990, 1, 1),
			Gender = "Male",
			IsActive = true
		};

		context.Patients.Add(patient);
		await context.SaveChangesAsync();

		var currentUserId = Guid.NewGuid();
		var service = CreateService(context);

		// Act
		var result = await service.UpdateAsync(
			patient.Id,
			"NEW001",
			"New",
			"Name",
			new DateOnly(1995, 5, 10),
			"Female",
			"0912345678",
			"Test Address",
			currentUserId);

		// Assert
		Assert.Equal(
			PatientOperationResult.Success,
			result);

		Assert.Equal("NEW001", patient.MedicalRecordNumber);
		Assert.Equal("New", patient.FirstName);
		Assert.Equal("Name", patient.LastName);
		Assert.Equal(new DateOnly(1995, 5, 10), patient.Birthday);
		Assert.Equal("Female", patient.Gender);
		Assert.Equal("0912345678", patient.Phone);
		Assert.Equal("Test Address", patient.Address);

		var auditLog = await context.AuditLogs.SingleAsync();

		Assert.Equal(currentUserId, auditLog.UserId);
		Assert.Equal("UpdatePatient", auditLog.Action);
		Assert.Equal("Patient", auditLog.EntityName);
		Assert.Equal(patient.Id.ToString(), auditLog.EntityId);
	}

	[Fact]
	public async Task UpdateAsync_WhenMedicalRecordNumberIsDuplicate_ReturnsDuplicateMedicalRecordNumber()
	{
		// Arrange
		await using var context = CreateDbContext();

		var patient1 = new Patient
		{
			MedicalRecordNumber = "MRN001",
			FirstName = "First",
			LastName = "Patient",
			Birthday = new DateOnly(1990, 1, 1),
			Gender = "Male",
			IsActive = true
		};

		var patient2 = new Patient
		{
			MedicalRecordNumber = "MRN002",
			FirstName = "Second",
			LastName = "Patient",
			Birthday = new DateOnly(1995, 1, 1),
			Gender = "Female",
			IsActive = true
		};

		context.Patients.AddRange(patient1, patient2);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.UpdateAsync(
			patient2.Id,
			"MRN001",
			patient2.FirstName,
			patient2.LastName,
			patient2.Birthday,
			patient2.Gender,
			patient2.Phone,
			patient2.Address,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			PatientOperationResult.DuplicateMedicalRecordNumber,
			result);

		Assert.Equal(
			"MRN002",
			patient2.MedicalRecordNumber);

		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task UpdateAsync_WhenPatientIsInactive_ReturnsInactive()
	{
		// Arrange
		await using var context = CreateDbContext();

		var patient = new Patient
		{
			MedicalRecordNumber = "MRN001",
			FirstName = "Old",
			LastName = "Name",
			Birthday = new DateOnly(1990, 1, 1),
			Gender = "Male",
			IsActive = false
		};

		context.Patients.Add(patient);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.UpdateAsync(
			patient.Id,
			"NEW001",
			"New",
			"Name",
			new DateOnly(1995, 5, 10),
			"Female",
			"0912345678",
			"New Address",
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			PatientOperationResult.Inactive,
			result);

		Assert.Equal("MRN001", patient.MedicalRecordNumber);
		Assert.Equal("Old", patient.FirstName);

		Assert.Empty(context.AuditLogs);
	}

	[Fact]
	public async Task UpdateAsync_WhenPatientDoesNotExist_ReturnsNotFound()
	{
		// Arrange
		await using var context = CreateDbContext();

		var service = CreateService(context);

		// Act
		var result = await service.UpdateAsync(
			999,
			"MRN001",
			"Test",
			"Patient",
			new DateOnly(1990, 1, 1),
			"Male",
			null,
			null,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			PatientOperationResult.NotFound,
			result);

		Assert.Empty(context.AuditLogs);
	}
}