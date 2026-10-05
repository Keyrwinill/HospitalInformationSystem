using HospitalInformationSystem.Data;
using HospitalInformationSystem.Models.Constants;
using HospitalInformationSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HospitalInformationSystem.Services;

public class AppointmentService : IAppointmentService
{
	private readonly HospitalDbContext _context;
	private readonly IAuditService _auditService;

	public AppointmentService(HospitalDbContext context, IAuditService auditService)
	{
		_context = context;
		_auditService = auditService;
	}

	public async Task<AppointmentOperationResult> CancelAsync(
		int appointmentId,
		Guid currentUserId)
	{
		var appointment = await _context.Appointments
			.Include(x => x.Visit)
			.FirstOrDefaultAsync(x => x.Id == appointmentId);

		if (appointment == null)
		{
			return AppointmentOperationResult.NotFound;
		}

		if (appointment.Status != AppointmentStatus.Scheduled ||
			appointment.Visit != null)
		{
			return AppointmentOperationResult.InvalidState;
		}

		if (appointment.AppointmentDateTime <= DateTime.Now)
		{
			return AppointmentOperationResult.PastDateTime;
		}

		appointment.Status = AppointmentStatus.Cancelled;

		await _auditService.LogAsync(
			currentUserId,
			"CancelAppointment",
			"Appointment",
			appointment.Id.ToString());

		await _context.SaveChangesAsync();

		return AppointmentOperationResult.Success;
	}

	public async Task<AppointmentOperationResult> MarkNoShowAsync(
		int appointmentId,
		Guid currentUserId)
	{
		var appointment = await _context.Appointments
			.Include(x => x.Visit)
			.FirstOrDefaultAsync(x => x.Id == appointmentId);

		if (appointment == null)
		{
			return AppointmentOperationResult.NotFound;
		}

		if (appointment.Status != AppointmentStatus.Scheduled ||
			appointment.Visit != null)
		{
			return AppointmentOperationResult.InvalidState;
		}

		if (appointment.AppointmentDateTime >= DateTime.Now)
		{
			return AppointmentOperationResult.FutureDateTime;
		}

		appointment.Status = AppointmentStatus.NoShow;

		await _auditService.LogAsync(
			currentUserId,
			"MarkAppointmentNoShow",
			"Appointment",
			appointment.Id.ToString());

		await _context.SaveChangesAsync();

		return AppointmentOperationResult.Success;
	}

	public async Task<Appointment?> GetReschedulableAppointmentAsync(
		int appointmentId)
	{
		return await _context.Appointments
			.AsNoTracking()
			.FirstOrDefaultAsync(x =>
				x.Id == appointmentId &&
				x.Status == AppointmentStatus.Scheduled &&
				x.Visit == null &&
				x.AppointmentDateTime > DateTime.Now);
	}

	public async Task<AppointmentOperationResult> RescheduleAsync(
		int appointmentId,
		DateTime appointmentDateTime,
		Guid currentUserId)
	{
		var appointment = await _context.Appointments
			.Include(x => x.Visit)
			.FirstOrDefaultAsync(x => x.Id == appointmentId);

		if (appointment == null)
		{
			return AppointmentOperationResult.NotFound;
		}

		if (appointment.Status != AppointmentStatus.Scheduled ||
			appointment.Visit != null)
		{
			return AppointmentOperationResult.InvalidState;
		}

		if (appointmentDateTime <= DateTime.Now)
		{
			return AppointmentOperationResult.PastDateTime;
		}

		if (appointment.AppointmentDateTime <= DateTime.Now)
		{
			return AppointmentOperationResult.PastDateTime;
		}

		var patientExists = await _context.Patients
			.AnyAsync(x =>
				x.Id == appointment.PatientId &&
				x.IsActive);

		if (!patientExists)
		{
			return AppointmentOperationResult.InvalidPatient;
		}

		var doctorExists = await _context.Doctors
			.AnyAsync(x =>
				x.Id == appointment.DoctorId &&
				x.IsActive &&
				x.User.IsActive &&
				x.Department.IsActive);

		if (!doctorExists)
		{
			return AppointmentOperationResult.InvalidDoctor;
		}

		var hasConflict = await _context.Appointments
			.AnyAsync(x =>
				x.Id != appointment.Id &&
				(x.DoctorId == appointment.DoctorId ||
				 x.PatientId == appointment.PatientId) &&
				x.AppointmentDateTime == appointmentDateTime &&
				x.Status != AppointmentStatus.Cancelled);

		if (hasConflict)
		{
			return AppointmentOperationResult.ScheduleConflict;
		}

		appointment.AppointmentDateTime = appointmentDateTime;

		await _auditService.LogAsync(
			currentUserId,
			"RescheduleAppointment",
			"Appointment",
			appointment.Id.ToString());

		await _context.SaveChangesAsync();

		return AppointmentOperationResult.Success;
	}

	public async Task<AppointmentOperationResult> CreateAsync(
		int patientId,
		int doctorId,
		DateTime appointmentDateTime,
		string? reason,
		Guid currentUserId)
	{
		if (appointmentDateTime <= DateTime.Now)
		{
			return AppointmentOperationResult.PastDateTime;
		}

		var patientExists = await _context.Patients
			.AnyAsync(x =>
				x.Id == patientId &&
				x.IsActive);

		if (!patientExists)
		{
			return AppointmentOperationResult.InvalidPatient;
		}

		var doctorExists = await _context.Doctors
			.AnyAsync(x =>
				x.Id == doctorId &&
				x.IsActive &&
				x.User.IsActive &&
				x.Department.IsActive);

		if (!doctorExists)
		{
			return AppointmentOperationResult.InvalidDoctor;
		}

		var appointmentConflict = await _context.Appointments
			.AnyAsync(x =>
				(x.DoctorId == doctorId ||
				 x.PatientId == patientId) &&
				x.AppointmentDateTime == appointmentDateTime &&
				x.Status != AppointmentStatus.Cancelled);

		if (appointmentConflict)
		{
			return AppointmentOperationResult.ScheduleConflict;
		}

		await using var transaction =
			await _context.Database.BeginTransactionAsync();

		var appointment = new Appointment
		{
			PatientId = patientId,
			DoctorId = doctorId,
			AppointmentDateTime = appointmentDateTime,
			Status = AppointmentStatus.Scheduled,
			Reason = reason
		};

		_context.Appointments.Add(appointment);

		// First save is required so SQL Server generates appointment.Id.
		await _context.SaveChangesAsync();

		await _auditService.LogAsync(
			currentUserId,
			"CreateAppointment",
			"Appointment",
			appointment.Id.ToString());

		// Saves the staged AuditLog.
		await _context.SaveChangesAsync();

		await transaction.CommitAsync();

		return AppointmentOperationResult.Success;
	}
}