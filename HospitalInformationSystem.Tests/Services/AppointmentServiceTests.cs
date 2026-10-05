using HospitalInformationSystem.Data;
using HospitalInformationSystem.Models.Constants;
using HospitalInformationSystem.Models.Entities;
using HospitalInformationSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HospitalInformationSystem.Tests.Services;

public class AppointmentServiceTests
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

	private static AppointmentService CreateService(
		HospitalDbContext context)
	{
		var auditService = new AuditService(context);

		return new AppointmentService(
			context,
			auditService);
	}

	[Fact]
	public async Task CreateAsync_WhenAppointmentIsInPast_ReturnsPastDateTime()
	{
		// Arrange
		await using var context = CreateDbContext();
		var service = CreateService(context);

		var appointmentDateTime = DateTime.Now.AddDays(-1);

		// Act
		var result = await service.CreateAsync(
			patientId: 1,
			doctorId: 1,
			appointmentDateTime: appointmentDateTime,
			reason: "Test",
			currentUserId: Guid.NewGuid());

		// Assert
		Assert.Equal(
			AppointmentOperationResult.PastDateTime,
			result);
	}

	[Fact]
	public async Task CreateAsync_WhenPatientIsInactive_ReturnsInvalidPatient()
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
		var result = await service.CreateAsync(
			patientId: patient.Id,
			doctorId: 1,
			appointmentDateTime: DateTime.Now.AddDays(1),
			reason: "Test",
			currentUserId: Guid.NewGuid());

		// Assert
		Assert.Equal(
			AppointmentOperationResult.InvalidPatient,
			result);
	}

	[Fact]
	public async Task CreateAsync_WhenDoctorDoesNotExist_ReturnsInvalidDoctor()
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
		var result = await service.CreateAsync(
			patientId: patient.Id,
			doctorId: 1,
			appointmentDateTime: DateTime.Now.AddDays(1),
			reason: "Test",
			currentUserId: Guid.NewGuid());

		// Assert
		Assert.Equal(
			AppointmentOperationResult.InvalidDoctor,
			result);
	}

	[Fact]
	public async Task CreateAsync_WhenDoctorIsInactive_ReturnsInvalidDoctor()
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
			IsActive = false
		};

		context.Patients.Add(patient);
		context.Doctors.Add(doctor);

		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.CreateAsync(
			patientId: patient.Id,
			doctorId: doctor.Id,
			appointmentDateTime: DateTime.Now.AddDays(1),
			reason: "Test",
			currentUserId: Guid.NewGuid());

		// Assert
		Assert.Equal(
			AppointmentOperationResult.InvalidDoctor,
			result);
	}

	[Fact]
	public async Task CreateAsync_WhenDepartmentIsInactive_ReturnsInvalidDoctor()
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
			IsActive = false
		};

		var doctor = new Doctor
		{
			User = user,
			Department = department,
			LicenseNumber = "TEST-LICENSE-001",
			IsActive = true
		};

		context.Patients.Add(patient);
		context.Doctors.Add(doctor);

		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.CreateAsync(
			patientId: patient.Id,
			doctorId: doctor.Id,
			appointmentDateTime: DateTime.Now.AddDays(1),
			reason: "Test",
			currentUserId: Guid.NewGuid());

		// Assert
		Assert.Equal(
			AppointmentOperationResult.InvalidDoctor,
			result);
	}

	[Fact]
	public async Task CreateAsync_WhenDoctorUserIsInactive_ReturnsInvalidDoctor()
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
			IsActive = true
		};

		context.Patients.Add(patient);
		context.Doctors.Add(doctor);

		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.CreateAsync(
			patientId: patient.Id,
			doctorId: doctor.Id,
			appointmentDateTime: DateTime.Now.AddDays(1),
			reason: "Test",
			currentUserId: Guid.NewGuid());

		// Assert
		Assert.Equal(
			AppointmentOperationResult.InvalidDoctor,
			result);
	}

	private static async Task<(Patient Patient, Doctor Doctor)>
		SeedValidPatientAndDoctorAsync(
			HospitalDbContext context,
			string suffix = "1")
	{
		var patient = new Patient
		{
			MedicalRecordNumber = $"TEST{suffix}",
			FirstName = "Test",
			LastName = "Patient",
			Birthday = new DateOnly(1990, 1, 1),
			Gender = "Male",
			IsActive = true
		};

		var user = new User
		{
			Id = Guid.NewGuid(),
			Account = $"doctor{suffix}",
			Email = $"doctor{suffix}@test.com",
			PasswordHash = "test",
			FirstName = "Test",
			LastName = "Doctor",
			Role = UserRoles.Doctor,
			IsActive = true
		};

		var department = new Department
		{
			Name = $"Test Department {suffix}",
			IsActive = true
		};

		var doctor = new Doctor
		{
			User = user,
			Department = department,
			LicenseNumber = $"TEST-LICENSE-{suffix}",
			IsActive = true
		};

		context.Patients.Add(patient);
		context.Doctors.Add(doctor);

		await context.SaveChangesAsync();

		return (patient, doctor);
	}

	[Fact]
	public async Task CreateAsync_WhenDoctorHasAppointmentAtSameTime_ReturnsScheduleConflict()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (patient, doctor) =
			await SeedValidPatientAndDoctorAsync(context);

		var appointmentDateTime = DateTime.Now.AddDays(1);

		var existingAppointment = new Appointment
		{
			PatientId = patient.Id,
			DoctorId = doctor.Id,
			AppointmentDateTime = appointmentDateTime,
			Status = AppointmentStatus.Scheduled,
			Reason = "Existing appointment"
		};

		context.Appointments.Add(existingAppointment);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.CreateAsync(
			patientId: patient.Id,
			doctorId: doctor.Id,
			appointmentDateTime: appointmentDateTime,
			reason: "New appointment",
			currentUserId: Guid.NewGuid());

		// Assert
		Assert.Equal(
			AppointmentOperationResult.ScheduleConflict,
			result);
	}

	[Fact]
	public async Task CreateAsync_WhenPatientHasAppointmentAtSameTime_ReturnsScheduleConflict()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (patient, doctor1) =
			await SeedValidPatientAndDoctorAsync(context, "1");

		var (_, doctor2) =
			await SeedValidPatientAndDoctorAsync(context, "2");

		var appointmentDateTime = DateTime.Now.AddDays(1);

		var existingAppointment = new Appointment
		{
			PatientId = patient.Id,
			DoctorId = doctor1.Id,
			AppointmentDateTime = appointmentDateTime,
			Status = AppointmentStatus.Scheduled,
			Reason = "Existing appointment"
		};

		context.Appointments.Add(existingAppointment);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.CreateAsync(
			patientId: patient.Id,
			doctorId: doctor2.Id,
			appointmentDateTime: appointmentDateTime,
			reason: "New appointment",
			currentUserId: Guid.NewGuid());

		// Assert
		Assert.Equal(
			AppointmentOperationResult.ScheduleConflict,
			result);
	}

	[Fact]
	public async Task CreateAsync_WhenConflictingAppointmentIsCancelled_ReturnsSuccess()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (patient, doctor) =
			await SeedValidPatientAndDoctorAsync(context);

		var appointmentDateTime = DateTime.Now.AddDays(1);

		var cancelledAppointment = new Appointment
		{
			PatientId = patient.Id,
			DoctorId = doctor.Id,
			AppointmentDateTime = appointmentDateTime,
			Status = AppointmentStatus.Cancelled,
			Reason = "Cancelled appointment"
		};

		context.Appointments.Add(cancelledAppointment);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var currentUserId = Guid.NewGuid();

		var result = await service.CreateAsync(
			patientId: patient.Id,
			doctorId: doctor.Id,
			appointmentDateTime: appointmentDateTime,
			reason: "Replacement appointment",
			currentUserId: currentUserId);

		// Assert
		// 1. returns Success
		Assert.Equal(
			AppointmentOperationResult.Success,
			result);

		// 2. creates Appointment
		var createdAppointment = await context.Appointments
			.SingleAsync(x =>
				x.Status == AppointmentStatus.Scheduled);

		Assert.Equal(patient.Id, createdAppointment.PatientId);
		Assert.Equal(doctor.Id, createdAppointment.DoctorId);
		Assert.Equal(appointmentDateTime, createdAppointment.AppointmentDateTime);
		Assert.Equal("Replacement appointment", createdAppointment.Reason);

		//3. creates matching AuditLog
		var auditLog = await context.AuditLogs.SingleAsync();

		Assert.Equal(currentUserId, auditLog.UserId);
		Assert.Equal("CreateAppointment", auditLog.Action);
		Assert.Equal("Appointment", auditLog.EntityName);
		Assert.Equal(createdAppointment.Id.ToString(), auditLog.EntityId);
	}

	[Fact]
	public async Task CancelAsync_WhenAppointmentIsValid_ReturnsSuccessAndCancelsAppointment()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (patient, doctor) =
			await SeedValidPatientAndDoctorAsync(context);

		var appointment = new Appointment
		{
			PatientId = patient.Id,
			DoctorId = doctor.Id,
			AppointmentDateTime = DateTime.Now.AddDays(1),
			Status = AppointmentStatus.Scheduled,
			Reason = "Test"
		};

		context.Appointments.Add(appointment);
		await context.SaveChangesAsync();

		var service = CreateService(context);
		var currentUserId = Guid.NewGuid();

		// Act
		var result = await service.CancelAsync(
			appointment.Id,
			currentUserId);

		// Assert
		Assert.Equal(
			AppointmentOperationResult.Success,
			result);

		Assert.Equal(
			AppointmentStatus.Cancelled,
			appointment.Status);
	}

	[Fact]
	public async Task CancelAsync_WhenAppointmentIsInPast_ReturnsPastDateTime()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (patient, doctor) =
			await SeedValidPatientAndDoctorAsync(context);

		var appointment = new Appointment
		{
			PatientId = patient.Id,
			DoctorId = doctor.Id,
			AppointmentDateTime = DateTime.Now.AddDays(-1),
			Status = AppointmentStatus.Scheduled,
			Reason = "Past appointment"
		};

		context.Appointments.Add(appointment);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.CancelAsync(
			appointment.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			AppointmentOperationResult.PastDateTime,
			result);

		Assert.Equal(
			AppointmentStatus.Scheduled,
			appointment.Status);
	}

	[Fact]
	public async Task CancelAsync_WhenAppointmentIsAlreadyCancelled_ReturnsInvalidState()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (patient, doctor) =
			await SeedValidPatientAndDoctorAsync(context);

		var appointment = new Appointment
		{
			PatientId = patient.Id,
			DoctorId = doctor.Id,
			AppointmentDateTime = DateTime.Now.AddDays(1),
			Status = AppointmentStatus.Cancelled,
			Reason = "Cancelled appointment"
		};

		context.Appointments.Add(appointment);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.CancelAsync(
			appointment.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			AppointmentOperationResult.InvalidState,
			result);

		Assert.Equal(
			AppointmentStatus.Cancelled,
			appointment.Status);
	}

	[Fact]
	public async Task CancelAsync_WhenAppointmentDoesNotExist_ReturnsNotFound()
	{
		// Arrange
		await using var context = CreateDbContext();
		var service = CreateService(context);

		// Act
		var result = await service.CancelAsync(
			appointmentId: 999,
			currentUserId: Guid.NewGuid());

		// Assert
		Assert.Equal(
			AppointmentOperationResult.NotFound,
			result);
	}

	[Fact]
	public async Task MarkNoShowAsync_WhenPastScheduledAppointment_ReturnsSuccessAndMarksNoShow()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (patient, doctor) =
			await SeedValidPatientAndDoctorAsync(context);

		var appointment = new Appointment
		{
			PatientId = patient.Id,
			DoctorId = doctor.Id,
			AppointmentDateTime = DateTime.Now.AddDays(-1),
			Status = AppointmentStatus.Scheduled,
			Reason = "Test"
		};

		context.Appointments.Add(appointment);
		await context.SaveChangesAsync();

		var service = CreateService(context);
		var currentUserId = Guid.NewGuid();

		// Act
		var result = await service.MarkNoShowAsync(
			appointment.Id,
			currentUserId);

		// Assert
		Assert.Equal(
			AppointmentOperationResult.Success,
			result);

		Assert.Equal(
			AppointmentStatus.NoShow,
			appointment.Status);
	}

	[Fact]
	public async Task MarkNoShowAsync_WhenAppointmentIsInFuture_ReturnsFutureDateTime()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (patient, doctor) =
			await SeedValidPatientAndDoctorAsync(context);

		var appointment = new Appointment
		{
			PatientId = patient.Id,
			DoctorId = doctor.Id,
			AppointmentDateTime = DateTime.Now.AddDays(1),
			Status = AppointmentStatus.Scheduled,
			Reason = "Future appointment"
		};

		context.Appointments.Add(appointment);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.MarkNoShowAsync(
			appointment.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			AppointmentOperationResult.FutureDateTime,
			result);

		Assert.Equal(
			AppointmentStatus.Scheduled,
			appointment.Status);
	}

	[Fact]
	public async Task MarkNoShowAsync_WhenAppointmentIsCancelled_ReturnsInvalidState()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (patient, doctor) =
			await SeedValidPatientAndDoctorAsync(context);

		var appointment = new Appointment
		{
			PatientId = patient.Id,
			DoctorId = doctor.Id,
			AppointmentDateTime = DateTime.Now.AddDays(-1),
			Status = AppointmentStatus.Cancelled,
			Reason = "Cancelled appointment"
		};

		context.Appointments.Add(appointment);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.MarkNoShowAsync(
			appointment.Id,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			AppointmentOperationResult.InvalidState,
			result);

		Assert.Equal(
			AppointmentStatus.Cancelled,
			appointment.Status);
	}

	[Fact]
	public async Task RescheduleAsync_WhenNewDateTimeIsInPast_ReturnsPastDateTime()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (patient, doctor) =
			await SeedValidPatientAndDoctorAsync(context);

		var appointment = new Appointment
		{
			PatientId = patient.Id,
			DoctorId = doctor.Id,
			AppointmentDateTime = DateTime.Now.AddDays(1),
			Status = AppointmentStatus.Scheduled,
			Reason = "Test"
		};

		context.Appointments.Add(appointment);
		await context.SaveChangesAsync();

		var originalDateTime = appointment.AppointmentDateTime;
		var service = CreateService(context);

		// Act
		var result = await service.RescheduleAsync(
			appointment.Id,
			DateTime.Now.AddDays(-1),
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			AppointmentOperationResult.PastDateTime,
			result);

		Assert.Equal(
			originalDateTime,
			appointment.AppointmentDateTime);
	}

	[Fact]
	public async Task RescheduleAsync_WhenDoctorHasAppointmentAtNewTime_ReturnsScheduleConflict()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (patient, doctor) =
			await SeedValidPatientAndDoctorAsync(context);

		var originalDateTime = DateTime.Now.AddDays(1);
		var conflictingDateTime = DateTime.Now.AddDays(2);

		var appointment = new Appointment
		{
			PatientId = patient.Id,
			DoctorId = doctor.Id,
			AppointmentDateTime = originalDateTime,
			Status = AppointmentStatus.Scheduled
		};

		var conflictingAppointment = new Appointment
		{
			PatientId = patient.Id,
			DoctorId = doctor.Id,
			AppointmentDateTime = conflictingDateTime,
			Status = AppointmentStatus.Scheduled
		};

		context.Appointments.AddRange(
			appointment,
			conflictingAppointment);

		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.RescheduleAsync(
			appointment.Id,
			conflictingDateTime,
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			AppointmentOperationResult.ScheduleConflict,
			result);

		Assert.Equal(
			originalDateTime,
			appointment.AppointmentDateTime);
	}

	[Fact]
	public async Task RescheduleAsync_WhenValid_ReturnsSuccessAndUpdatesAppointment()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (patient, doctor) =
			await SeedValidPatientAndDoctorAsync(context);

		var appointment = new Appointment
		{
			PatientId = patient.Id,
			DoctorId = doctor.Id,
			AppointmentDateTime = DateTime.Now.AddDays(1),
			Status = AppointmentStatus.Scheduled,
			Reason = "Test"
		};

		context.Appointments.Add(appointment);
		await context.SaveChangesAsync();

		var newDateTime = DateTime.Now.AddDays(2);
		var currentUserId = Guid.NewGuid();

		var service = CreateService(context);

		// Act
		var result = await service.RescheduleAsync(
			appointment.Id,
			newDateTime,
			currentUserId);

		// Assert
		Assert.Equal(
			AppointmentOperationResult.Success,
			result);

		Assert.Equal(
			newDateTime,
			appointment.AppointmentDateTime);

		var auditLog = await context.AuditLogs.SingleAsync();

		Assert.Equal(currentUserId, auditLog.UserId);
		Assert.Equal("RescheduleAppointment", auditLog.Action);
		Assert.Equal("Appointment", auditLog.EntityName);
		Assert.Equal(appointment.Id.ToString(), auditLog.EntityId);
	}

	[Fact]
	public async Task RescheduleAsync_WhenPatientIsInactive_ReturnsInvalidPatient()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (patient, doctor) =
			await SeedValidPatientAndDoctorAsync(context);

		var originalDateTime = DateTime.Now.AddDays(1);

		var appointment = new Appointment
		{
			PatientId = patient.Id,
			DoctorId = doctor.Id,
			AppointmentDateTime = originalDateTime,
			Status = AppointmentStatus.Scheduled
		};

		context.Appointments.Add(appointment);
		await context.SaveChangesAsync();

		// Patient becomes inactive after the appointment was created.
		patient.IsActive = false;
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.RescheduleAsync(
			appointment.Id,
			DateTime.Now.AddDays(2),
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			AppointmentOperationResult.InvalidPatient,
			result);

		Assert.Equal(
			originalDateTime,
			appointment.AppointmentDateTime);
	}

	[Fact]
	public async Task RescheduleAsync_WhenDoctorIsInactive_ReturnsInvalidDoctor()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (patient, doctor) =
			await SeedValidPatientAndDoctorAsync(context);

		var originalDateTime = DateTime.Now.AddDays(1);

		var appointment = new Appointment
		{
			PatientId = patient.Id,
			DoctorId = doctor.Id,
			AppointmentDateTime = originalDateTime,
			Status = AppointmentStatus.Scheduled
		};

		context.Appointments.Add(appointment);
		await context.SaveChangesAsync();

		// Doctor becomes inactive after the appointment was created.
		doctor.IsActive = false;
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.RescheduleAsync(
			appointment.Id,
			DateTime.Now.AddDays(2),
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			AppointmentOperationResult.InvalidDoctor,
			result);

		Assert.Equal(
			originalDateTime,
			appointment.AppointmentDateTime);
	}

	[Fact]
	public async Task GetReschedulableAppointmentAsync_WhenScheduledWithoutVisit_ReturnsAppointment()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (patient, doctor) =
			await SeedValidPatientAndDoctorAsync(context);

		var appointment = new Appointment
		{
			PatientId = patient.Id,
			DoctorId = doctor.Id,
			AppointmentDateTime = DateTime.Now.AddDays(1),
			Status = AppointmentStatus.Scheduled
		};

		context.Appointments.Add(appointment);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result =
			await service.GetReschedulableAppointmentAsync(appointment.Id);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(appointment.Id, result.Id);
	}

	[Fact]
	public async Task GetReschedulableAppointmentAsync_WhenCancelled_ReturnsNull()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (patient, doctor) =
			await SeedValidPatientAndDoctorAsync(context);

		var appointment = new Appointment
		{
			PatientId = patient.Id,
			DoctorId = doctor.Id,
			AppointmentDateTime = DateTime.Now.AddDays(1),
			Status = AppointmentStatus.Cancelled
		};

		context.Appointments.Add(appointment);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result =
			await service.GetReschedulableAppointmentAsync(appointment.Id);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task RescheduleAsync_WhenOriginalAppointmentIsInPast_ReturnsPastDateTime()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (patient, doctor) =
			await SeedValidPatientAndDoctorAsync(context);

		var originalDateTime = DateTime.Now.AddHours(-1);

		var appointment = new Appointment
		{
			PatientId = patient.Id,
			DoctorId = doctor.Id,
			AppointmentDateTime = originalDateTime,
			Status = AppointmentStatus.Scheduled
		};

		context.Appointments.Add(appointment);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.RescheduleAsync(
			appointment.Id,
			DateTime.Now.AddDays(1),
			Guid.NewGuid());

		// Assert
		Assert.Equal(
			AppointmentOperationResult.PastDateTime,
			result);

		Assert.Equal(
			originalDateTime,
			appointment.AppointmentDateTime);
	}
}