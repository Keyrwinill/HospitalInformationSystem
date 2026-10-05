using HospitalInformationSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using HospitalInformationSystem.Services;
using HospitalInformationSystem.Models.Constants;
using HospitalInformationSystem.Models.Entities;

namespace HospitalInformationSystem.Tests.Services;

public class VisitServiceTests
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

	private static VisitService CreateService(
		HospitalDbContext context)
	{
		var auditService = new AuditService(context);

		return new VisitService(
			context,
			auditService);
	}

	private static async Task<(Appointment Appointment, Guid DoctorUserId)>
		SeedValidAppointmentAsync(
			HospitalDbContext context,
			DateTime appointmentDateTime)
	{
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
			AppointmentDateTime = appointmentDateTime,
			Status = AppointmentStatus.Scheduled,
			Reason = "Test appointment"
		};

		context.Appointments.Add(appointment);
		await context.SaveChangesAsync();

		return (appointment, user.Id);
	}

	private static async Task<(Visit Visit, Guid DoctorUserId)>
		SeedActiveVisitAsync(HospitalDbContext context)
	{
		var (appointment, doctorUserId) =
			await SeedValidAppointmentAsync(
				context,
				DateTime.Now.AddHours(-1));

		var service = CreateService(context);

		var visit = await service.CreateFromAppointmentAsync(
			appointment.Id,
			doctorUserId);

		Assert.NotNull(visit);

		return (visit, doctorUserId);
	}

	private static async Task<Medication> SeedActiveMedicationAsync(
		HospitalDbContext context)
	{
		var medication = new Medication
		{
			Code = "MED001",
			Name = "Test Medication",
			Unit = "tablet",
			IsActive = true
		};

		context.Medications.Add(medication);
		await context.SaveChangesAsync();

		return medication;
	}

	[Fact]
	public async Task CreateFromAppointmentAsync_WhenAppointmentIsInFuture_ReturnsNull()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (appointment, doctorUserId) =
			await SeedValidAppointmentAsync(
				context,
				DateTime.Now.AddDays(1));

		var service = CreateService(context);

		// Act
		var result = await service.CreateFromAppointmentAsync(
			appointment.Id,
			doctorUserId);

		// Assert
		Assert.Null(result);

		Assert.Empty(context.Visits);
	}

	[Fact]
	public async Task CreateFromAppointmentAsync_WhenAppointmentIsValid_CreatesVisit()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (appointment, doctorUserId) =
			await SeedValidAppointmentAsync(
				context,
				DateTime.Now.AddHours(-1));

		var service = CreateService(context);

		// Act
		var result = await service.CreateFromAppointmentAsync(
			appointment.Id,
			doctorUserId);

		// Assert
		Assert.NotNull(result);

		Assert.Equal(appointment.PatientId, result.PatientId);
		Assert.Equal(appointment.DoctorId, result.DoctorId);
		Assert.Equal(appointment.Id, result.AppointmentId);

		Assert.Single(context.Visits);

		var auditLog = await context.AuditLogs.SingleAsync();

		Assert.Equal(doctorUserId, auditLog.UserId);
		Assert.Equal("StartVisit", auditLog.Action);
		Assert.Equal("Visit", auditLog.EntityName);
		Assert.Equal(result.Id.ToString(), auditLog.EntityId);
	}

	[Fact]
	public async Task CreateFromAppointmentAsync_WhenVisitAlreadyExists_ReturnsNull()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (appointment, doctorUserId) =
			await SeedValidAppointmentAsync(
				context,
				DateTime.Now.AddHours(-1));

		var existingVisit = new Visit
		{
			PatientId = appointment.PatientId,
			DoctorId = appointment.DoctorId,
			AppointmentId = appointment.Id,
			VisitDateTime = DateTime.Now
		};

		context.Visits.Add(existingVisit);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.CreateFromAppointmentAsync(
			appointment.Id,
			doctorUserId);

		// Assert
		Assert.Null(result);

		Assert.Single(context.Visits);
	}

	[Fact]
	public async Task CreateFromAppointmentAsync_WhenPatientIsInactive_ReturnsNull()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (appointment, doctorUserId) =
			await SeedValidAppointmentAsync(
				context,
				DateTime.Now.AddHours(-1));

		appointment.Patient.IsActive = false;
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.CreateFromAppointmentAsync(
			appointment.Id,
			doctorUserId);

		// Assert
		Assert.Null(result);
		Assert.Empty(context.Visits);
	}

	[Fact]
	public async Task CreateFromAppointmentAsync_WhenDoctorIsInactive_ReturnsNull()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (appointment, doctorUserId) =
			await SeedValidAppointmentAsync(
				context,
				DateTime.Now.AddHours(-1));

		appointment.Doctor.IsActive = false;
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.CreateFromAppointmentAsync(
			appointment.Id,
			doctorUserId);

		// Assert
		Assert.Null(result);
		Assert.Empty(context.Visits);
	}

	[Fact]
	public async Task CreateFromAppointmentAsync_WhenDoctorUserIsInactive_ReturnsNull()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (appointment, doctorUserId) =
			await SeedValidAppointmentAsync(
				context,
				DateTime.Now.AddHours(-1));

		appointment.Doctor.User.IsActive = false;
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.CreateFromAppointmentAsync(
			appointment.Id,
			doctorUserId);

		// Assert
		Assert.Null(result);
		Assert.Empty(context.Visits);
	}

	[Fact]
	public async Task CreateFromAppointmentAsync_WhenDepartmentIsInactive_ReturnsNull()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (appointment, doctorUserId) =
			await SeedValidAppointmentAsync(
				context,
				DateTime.Now.AddHours(-1));

		appointment.Doctor.Department.IsActive = false;
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.CreateFromAppointmentAsync(
			appointment.Id,
			doctorUserId);

		// Assert
		Assert.Null(result);
		Assert.Empty(context.Visits);
	}

	[Fact]
	public async Task AddDiagnosisAsync_WhenVisitIsActive_AddsDiagnosis()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (visit, doctorUserId) =
			await SeedActiveVisitAsync(context);

		var service = CreateService(context);

		// Act
		var result = await service.AddDiagnosisAsync(
			visit.Id,
			doctorUserId,
			"J00",
			"Test diagnosis");

		// Assert
		Assert.NotNull(result);

		Assert.Equal(visit.Id, result.VisitId);
		Assert.Equal("J00", result.DiagnosisCode);
		Assert.Equal("Test diagnosis", result.Description);

		Assert.Single(context.Diagnoses);

		var auditLog = context.AuditLogs
			.Single(x => x.Action == "AddDiagnosis");

		Assert.Equal(doctorUserId, auditLog.UserId);
		Assert.Equal("Diagnosis", auditLog.EntityName);
		Assert.Equal(result.Id.ToString(), auditLog.EntityId);
	}

	[Fact]
	public async Task AddDiagnosisAsync_WhenDiagnosisCodeIsEmpty_ReturnsNull()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (visit, doctorUserId) =
			await SeedActiveVisitAsync(context);

		var service = CreateService(context);

		// Act
		var result = await service.AddDiagnosisAsync(
			visit.Id,
			doctorUserId,
			"",
			"Test diagnosis");

		// Assert
		Assert.Null(result);

		Assert.Empty(context.Diagnoses);
	}

	[Fact]
	public async Task AddDiagnosisAsync_WhenDescriptionIsEmpty_ReturnsNull()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (visit, doctorUserId) =
			await SeedActiveVisitAsync(context);

		var service = CreateService(context);

		// Act
		var result = await service.AddDiagnosisAsync(
			visit.Id,
			doctorUserId,
			"J00",
			"");

		// Assert
		Assert.Null(result);

		Assert.Empty(context.Diagnoses);
	}

	[Fact]
	public async Task AddPrescriptionItemAsync_WhenValid_AddsPrescriptionItem()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (visit, doctorUserId) =
			await SeedActiveVisitAsync(context);

		var medication =
			await SeedActiveMedicationAsync(context);

		var service = CreateService(context);

		// Act
		var result = await service.AddPrescriptionItemAsync(
			visit.Id,
			doctorUserId,
			medication.Id,
			"1 tablet",
			"Twice daily",
			7);

		// Assert
		Assert.NotNull(result);

		Assert.Equal(medication.Id, result.MedicationId);
		Assert.Equal("1 tablet", result.Dosage);
		Assert.Equal("Twice daily", result.Frequency);
		Assert.Equal(7, result.Days);

		Assert.Single(context.Prescriptions);
		Assert.Single(context.PrescriptionItems);

		var auditLog = context.AuditLogs
			.Single(x => x.Action == "AddPrescriptionItem");

		Assert.Equal(doctorUserId, auditLog.UserId);
		Assert.Equal("PrescriptionItem", auditLog.EntityName);
		Assert.Equal(result.Id.ToString(), auditLog.EntityId);
	}

	[Fact]
	public async Task AddPrescriptionItemAsync_WhenDaysIsZero_ReturnsNull()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (visit, doctorUserId) =
			await SeedActiveVisitAsync(context);

		var medication =
			await SeedActiveMedicationAsync(context);

		var service = CreateService(context);

		// Act
		var result = await service.AddPrescriptionItemAsync(
			visit.Id,
			doctorUserId,
			medication.Id,
			"1 tablet",
			"Twice daily",
			0);

		// Assert
		Assert.Null(result);

		Assert.Empty(context.Prescriptions);
		Assert.Empty(context.PrescriptionItems);
	}

	[Fact]
	public async Task AddPrescriptionItemAsync_WhenMedicationIsInactive_ReturnsNull()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (visit, doctorUserId) =
			await SeedActiveVisitAsync(context);

		var medication =
			await SeedActiveMedicationAsync(context);

		medication.IsActive = false;
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.AddPrescriptionItemAsync(
			visit.Id,
			doctorUserId,
			medication.Id,
			"1 tablet",
			"Twice daily",
			7);

		// Assert
		Assert.Null(result);

		Assert.Empty(context.Prescriptions);
		Assert.Empty(context.PrescriptionItems);
	}

	[Fact]
	public async Task CompleteVisitAsync_WhenVisitHasNoDiagnosis_ReturnsValidationError()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (visit, doctorUserId) =
			await SeedActiveVisitAsync(context);

		var service = CreateService(context);

		// Act
		var result = await service.CompleteVisitAsync(
			visit.Id,
			doctorUserId);

		// Assert
		Assert.Equal(
			VisitOperationResult.ValidationError,
			result);

		Assert.Equal(
			AppointmentStatus.Scheduled,
			visit.Appointment!.Status);
	}

	[Fact]
	public async Task CompleteVisitAsync_WhenVisitHasDiagnosis_CompletesVisit()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (visit, doctorUserId) =
			await SeedActiveVisitAsync(context);

		var service = CreateService(context);

		var diagnosis = await service.AddDiagnosisAsync(
			visit.Id,
			doctorUserId,
			"J00",
			"Test diagnosis");

		Assert.NotNull(diagnosis);

		// Act
		var result = await service.CompleteVisitAsync(
			visit.Id,
			doctorUserId);

		// Assert
		Assert.Equal(
			VisitOperationResult.Success,
			result);

		Assert.Equal(
			AppointmentStatus.Completed,
			visit.Appointment!.Status);

		var auditLog = context.AuditLogs
			.Single(x => x.Action == "CompleteVisit");

		Assert.Equal(doctorUserId, auditLog.UserId);
		Assert.Equal("Visit", auditLog.EntityName);
		Assert.Equal(visit.Id.ToString(), auditLog.EntityId);
	}

	[Fact]
	public async Task AddDiagnosisAsync_WhenVisitIsCompleted_ReturnsNull()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (visit, doctorUserId) =
			await SeedActiveVisitAsync(context);

		var service = CreateService(context);

		var diagnosis = await service.AddDiagnosisAsync(
			visit.Id,
			doctorUserId,
			"J00",
			"Initial diagnosis");

		Assert.NotNull(diagnosis);

		var completed = await service.CompleteVisitAsync(
			visit.Id,
			doctorUserId);

		Assert.Equal(
			VisitOperationResult.Success,
			completed);

		// Act
		var result = await service.AddDiagnosisAsync(
			visit.Id,
			doctorUserId,
			"J01",
			"Diagnosis after completion");

		// Assert
		Assert.Null(result);

		Assert.Single(context.Diagnoses);
	}

	[Fact]
	public async Task AddPrescriptionItemAsync_WhenVisitIsCompleted_ReturnsNull()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (visit, doctorUserId) =
			await SeedActiveVisitAsync(context);

		var medication =
			await SeedActiveMedicationAsync(context);

		var service = CreateService(context);

		// A diagnosis is required before the Visit can be completed.
		var diagnosis = await service.AddDiagnosisAsync(
			visit.Id,
			doctorUserId,
			"J00",
			"Test diagnosis");

		Assert.NotNull(diagnosis);

		var completed = await service.CompleteVisitAsync(
			visit.Id,
			doctorUserId);

		Assert.Equal(
			VisitOperationResult.Success,
			completed);

		// Act
		var result = await service.AddPrescriptionItemAsync(
			visit.Id,
			doctorUserId,
			medication.Id,
			"1 tablet",
			"Twice daily",
			7);

		// Assert
		Assert.Null(result);

		Assert.Empty(context.Prescriptions);
		Assert.Empty(context.PrescriptionItems);
	}

	[Fact]
	public async Task UpdateDiagnosisAsync_WhenVisitIsActive_UpdatesDiagnosis()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (visit, doctorUserId) =
			await SeedActiveVisitAsync(context);

		var service = CreateService(context);

		var diagnosis = await service.AddDiagnosisAsync(
			visit.Id,
			doctorUserId,
			"J00",
			"Original diagnosis");

		Assert.NotNull(diagnosis);

		// Act
		var visitId = await service.UpdateDiagnosisAsync(
			diagnosis.Id,
			doctorUserId,
			"J01",
			"Updated diagnosis");

		// Assert
		Assert.Equal(visit.Id, visitId);

		Assert.Equal("J01", diagnosis.DiagnosisCode);
		Assert.Equal("Updated diagnosis", diagnosis.Description);
	}

	[Fact]
	public async Task UpdateDiagnosisAsync_WhenVisitIsCompleted_ReturnsNull()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (visit, doctorUserId) =
			await SeedActiveVisitAsync(context);

		var service = CreateService(context);

		var diagnosis = await service.AddDiagnosisAsync(
			visit.Id,
			doctorUserId,
			"J00",
			"Original diagnosis");

		Assert.NotNull(diagnosis);

		var completed = await service.CompleteVisitAsync(
			visit.Id,
			doctorUserId);

		Assert.Equal(
			VisitOperationResult.Success,
			completed);

		// Act
		var visitId = await service.UpdateDiagnosisAsync(
			diagnosis.Id,
			doctorUserId,
			"J01",
			"Changed after completion");

		// Assert
		Assert.Null(visitId);

		Assert.Equal("J00", diagnosis.DiagnosisCode);
		Assert.Equal("Original diagnosis", diagnosis.Description);
	}

	[Fact]
	public async Task DeleteDiagnosisAsync_WhenVisitIsActive_DeletesDiagnosis()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (visit, doctorUserId) =
			await SeedActiveVisitAsync(context);

		var service = CreateService(context);

		var diagnosis = await service.AddDiagnosisAsync(
			visit.Id,
			doctorUserId,
			"J00",
			"Test diagnosis");

		Assert.NotNull(diagnosis);

		// Act
		var visitId = await service.DeleteDiagnosisAsync(
			diagnosis.Id,
			doctorUserId);

		// Assert
		Assert.Equal(visit.Id, visitId);

		Assert.Empty(context.Diagnoses);
	}

	[Fact]
	public async Task DeleteDiagnosisAsync_WhenVisitIsCompleted_ReturnsNull()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (visit, doctorUserId) =
			await SeedActiveVisitAsync(context);

		var service = CreateService(context);

		var diagnosis = await service.AddDiagnosisAsync(
			visit.Id,
			doctorUserId,
			"J00",
			"Test diagnosis");

		Assert.NotNull(diagnosis);

		var completed = await service.CompleteVisitAsync(
			visit.Id,
			doctorUserId);

		Assert.Equal(
			VisitOperationResult.Success,
			completed);

		// Act
		var visitId = await service.DeleteDiagnosisAsync(
			diagnosis.Id,
			doctorUserId);

		// Assert
		Assert.Null(visitId);

		Assert.Single(context.Diagnoses);
	}

	[Fact]
	public async Task UpdatePrescriptionItemAsync_WhenVisitIsActive_UpdatesItem()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (visit, doctorUserId) =
			await SeedActiveVisitAsync(context);

		var medication =
			await SeedActiveMedicationAsync(context);

		var service = CreateService(context);

		var item = await service.AddPrescriptionItemAsync(
			visit.Id,
			doctorUserId,
			medication.Id,
			"1 tablet",
			"Once daily",
			7);

		Assert.NotNull(item);

		// Act
		var visitId = await service.UpdatePrescriptionItemAsync(
			item.Id,
			doctorUserId,
			"2 tablets",
			"Twice daily",
			14);

		// Assert
		Assert.Equal(visit.Id, visitId);

		Assert.Equal("2 tablets", item.Dosage);
		Assert.Equal("Twice daily", item.Frequency);
		Assert.Equal(14, item.Days);
	}

	[Fact]
	public async Task UpdatePrescriptionItemAsync_WhenVisitIsCompleted_ReturnsNull()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (visit, doctorUserId) =
			await SeedActiveVisitAsync(context);

		var medication =
			await SeedActiveMedicationAsync(context);

		var service = CreateService(context);

		var item = await service.AddPrescriptionItemAsync(
			visit.Id,
			doctorUserId,
			medication.Id,
			"1 tablet",
			"Once daily",
			7);

		Assert.NotNull(item);

		// A diagnosis is required to complete the Visit.
		var diagnosis = await service.AddDiagnosisAsync(
			visit.Id,
			doctorUserId,
			"J00",
			"Test diagnosis");

		Assert.NotNull(diagnosis);

		var completed = await service.CompleteVisitAsync(
			visit.Id,
			doctorUserId);

		Assert.Equal(
			VisitOperationResult.Success,
			completed);

		// Act
		var visitId = await service.UpdatePrescriptionItemAsync(
			item.Id,
			doctorUserId,
			"2 tablets",
			"Twice daily",
			14);

		// Assert
		Assert.Null(visitId);

		Assert.Equal("1 tablet", item.Dosage);
		Assert.Equal("Once daily", item.Frequency);
		Assert.Equal(7, item.Days);
	}

	[Fact]
	public async Task DeletePrescriptionItemAsync_WhenVisitIsActive_DeletesItem()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (visit, doctorUserId) =
			await SeedActiveVisitAsync(context);

		var medication =
			await SeedActiveMedicationAsync(context);

		var service = CreateService(context);

		var item = await service.AddPrescriptionItemAsync(
			visit.Id,
			doctorUserId,
			medication.Id,
			"1 tablet",
			"Once daily",
			7);

		Assert.NotNull(item);

		// Act
		var visitId = await service.DeletePrescriptionItemAsync(
			item.Id,
			doctorUserId);

		// Assert
		Assert.Equal(visit.Id, visitId);

		Assert.Empty(context.PrescriptionItems);
	}

	[Fact]
	public async Task DeletePrescriptionItemAsync_WhenVisitIsCompleted_ReturnsNull()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (visit, doctorUserId) =
			await SeedActiveVisitAsync(context);

		var medication =
			await SeedActiveMedicationAsync(context);

		var service = CreateService(context);

		var item = await service.AddPrescriptionItemAsync(
			visit.Id,
			doctorUserId,
			medication.Id,
			"1 tablet",
			"Once daily",
			7);

		Assert.NotNull(item);

		var diagnosis = await service.AddDiagnosisAsync(
			visit.Id,
			doctorUserId,
			"J00",
			"Test diagnosis");

		Assert.NotNull(diagnosis);

		var completed = await service.CompleteVisitAsync(
			visit.Id,
			doctorUserId);

		Assert.Equal(
			VisitOperationResult.Success,
			completed);

		// Act
		var visitId = await service.DeletePrescriptionItemAsync(
			item.Id,
			doctorUserId);

		// Assert
		Assert.Null(visitId);

		Assert.Single(context.PrescriptionItems);
	}

	[Fact]
	public async Task UpdateVisitAsync_WhenVisitIsActive_UpdatesVisit()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (visit, doctorUserId) =
			await SeedActiveVisitAsync(context);

		var service = CreateService(context);

		// Act
		var result = await service.UpdateVisitAsync(
			visit.Id,
			doctorUserId,
			"Headache",
			"Patient reports headache for two days.");

		// Assert
		Assert.Equal(
			VisitOperationResult.Success,
			result);

		Assert.Equal("Headache", visit.ChiefComplaint);
		Assert.Equal(
			"Patient reports headache for two days.",
			visit.Notes);
	}

	[Fact]
	public async Task UpdateVisitAsync_WhenVisitIsCompleted_ReturnsNotFound()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (visit, doctorUserId) =
			await SeedActiveVisitAsync(context);

		var service = CreateService(context);

		visit.ChiefComplaint = "Original complaint";
		visit.Notes = "Original notes";
		await context.SaveChangesAsync();

		var diagnosis = await service.AddDiagnosisAsync(
			visit.Id,
			doctorUserId,
			"J00",
			"Test diagnosis");

		Assert.NotNull(diagnosis);

		var completed = await service.CompleteVisitAsync(
			visit.Id,
			doctorUserId);

		Assert.Equal(
			VisitOperationResult.Success,
			completed);

		// Act
		var result = await service.UpdateVisitAsync(
			visit.Id,
			doctorUserId,
			"Changed complaint",
			"Changed notes");

		// Assert
		Assert.Equal(
			VisitOperationResult.NotFound,
			result);

		Assert.Equal("Original complaint", visit.ChiefComplaint);
		Assert.Equal("Original notes", visit.Notes);
	}

	[Fact]
	public async Task GetVisitForDoctorAsync_WhenVisitBelongsToAnotherDoctor_ReturnsNull()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (visit, _) =
			await SeedActiveVisitAsync(context);

		var anotherDoctorUserId = Guid.NewGuid();

		var service = CreateService(context);

		// Act
		var result = await service.GetVisitForDoctorAsync(
			visit.Id,
			anotherDoctorUserId);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task GetVisitForDoctorAsync_WhenVisitBelongsToDoctor_ReturnsVisit()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (visit, doctorUserId) =
			await SeedActiveVisitAsync(context);

		var service = CreateService(context);

		// Act
		var result = await service.GetVisitForDoctorAsync(
			visit.Id,
			doctorUserId);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(visit.Id, result.Id);
	}

	[Fact]
	public async Task GetVisitForDoctorAsync_WhenDoctorUserIsInactive_ReturnsNull()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (visit, doctorUserId) =
			await SeedActiveVisitAsync(context);

		var user = await context.Users
			.SingleAsync(x => x.Id == doctorUserId);

		user.IsActive = false;
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.GetVisitForDoctorAsync(
			visit.Id,
			doctorUserId);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task GetVisitForDoctorAsync_WhenDoctorIsInactive_ReturnsNull()
	{
		// Arrange
		await using var context = CreateDbContext();

		var (visit, doctorUserId) =
			await SeedActiveVisitAsync(context);

		var doctor = await context.Doctors
			.SingleAsync(x => x.UserId == doctorUserId);

		doctor.IsActive = false;
		await context.SaveChangesAsync();

		var service = CreateService(context);

		// Act
		var result = await service.GetVisitForDoctorAsync(
			visit.Id,
			doctorUserId);

		// Assert
		Assert.Null(result);
	}
}