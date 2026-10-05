using HospitalInformationSystem.Data;
using HospitalInformationSystem.Models.Constants;
using HospitalInformationSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HospitalInformationSystem.Services;

public class DoctorService : IDoctorService
{
	private readonly HospitalDbContext _context;
	private readonly IAuditService _auditService;

	public DoctorService(
		HospitalDbContext context,
		IAuditService auditService)
	{
		_context = context;
		_auditService = auditService;
	}

	public async Task<DoctorOperationResult> DeactivateAsync(
		int doctorId,
		Guid currentUserId)
	{
		var doctor = await _context.Doctors
			.Include(x => x.User)
			.FirstOrDefaultAsync(x => x.Id == doctorId);

		if (doctor == null)
		{
			return DoctorOperationResult.NotFound;
		}

		if (!doctor.IsActive)
		{
			return DoctorOperationResult.Inactive;
		}

		var hasFutureScheduledAppointment =
			await _context.Appointments.AnyAsync(x =>
				x.DoctorId == doctor.Id &&
				x.Status == AppointmentStatus.Scheduled &&
				x.AppointmentDateTime > DateTime.Now);

		if (hasFutureScheduledAppointment)
		{
			return DoctorOperationResult.HasFutureScheduledAppointment;
		}

		var hasActiveVisit = await _context.Visits
			.AnyAsync(x =>
				x.DoctorId == doctor.Id &&
				x.Appointment != null &&
				x.Appointment.Status == AppointmentStatus.Scheduled);

		if (hasActiveVisit)
		{
			return DoctorOperationResult.HasActiveVisit;
		}

		doctor.IsActive = false;
		doctor.User.IsActive = false;

		await _auditService.LogAsync(
			currentUserId,
			"DeactivateDoctor",
			"Doctor",
			doctor.Id.ToString());

		await _context.SaveChangesAsync();

		return DoctorOperationResult.Success;
	}

	public async Task<DoctorOperationResult> ActivateAsync(
		int doctorId,
		Guid currentUserId)
	{
		var doctor = await _context.Doctors
			.Include(x => x.User)
			.Include(x => x.Department)
			.FirstOrDefaultAsync(x => x.Id == doctorId);

		if (doctor == null)
		{
			return DoctorOperationResult.NotFound;
		}

		if (doctor.IsActive)
		{
			return DoctorOperationResult.AlreadyActive;
		}

		if (!doctor.Department.IsActive)
		{
			return DoctorOperationResult.InactiveDepartment;
		}

		doctor.IsActive = true;
		doctor.User.IsActive = true;

		await _auditService.LogAsync(
			currentUserId,
			"ActivateDoctor",
			"Doctor",
			doctor.Id.ToString());

		await _context.SaveChangesAsync();

		return DoctorOperationResult.Success;
	}

	public async Task<DoctorOperationResult> UpdateAsync(
		int doctorId,
		string firstName,
		string lastName,
		string email,
		string licenseNumber,
		int departmentId,
		Guid currentUserId)
	{
		var doctor = await _context.Doctors
			.Include(x => x.User)
			.FirstOrDefaultAsync(x => x.Id == doctorId);

		if (doctor == null)
		{
			return DoctorOperationResult.NotFound;
		}

		if (!doctor.IsActive || !doctor.User.IsActive)
		{
			return DoctorOperationResult.Inactive;
		}

		var departmentExists = await _context.Departments
			.AnyAsync(x =>
				x.Id == departmentId &&
				x.IsActive);

		if (!departmentExists)
		{
			return DoctorOperationResult.InactiveDepartment;
		}

		var emailExists = await _context.Users
			.AnyAsync(x =>
				x.Id != doctor.UserId &&
				x.Email == email);

		if (emailExists)
		{
			return DoctorOperationResult.DuplicateEmail;
		}

		var licenseExists = await _context.Doctors
			.AnyAsync(x =>
				x.Id != doctorId &&
				x.LicenseNumber == licenseNumber);

		if (licenseExists)
		{
			return DoctorOperationResult.DuplicateLicenseNumber;
		}

		doctor.User.FirstName = firstName;
		doctor.User.LastName = lastName;
		doctor.User.Email = email;

		doctor.LicenseNumber = licenseNumber;
		doctor.DepartmentId = departmentId;

		await _auditService.LogAsync(
			currentUserId,
			"UpdateDoctor",
			"Doctor",
			doctor.Id.ToString());

		await _context.SaveChangesAsync();

		return DoctorOperationResult.Success;
	}
}