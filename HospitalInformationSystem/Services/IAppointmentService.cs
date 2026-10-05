using HospitalInformationSystem.Models.Constants;
using HospitalInformationSystem.Models.Entities;

namespace HospitalInformationSystem.Services;

public interface IAppointmentService
{
	Task<AppointmentOperationResult> CancelAsync(
		int appointmentId,
		Guid currentUserId);

	Task<AppointmentOperationResult> MarkNoShowAsync(
		int appointmentId,
		Guid currentUserId);

	Task<Appointment?> GetReschedulableAppointmentAsync(
		int appointmentId);

	Task<AppointmentOperationResult> RescheduleAsync(
		int appointmentId,
		DateTime appointmentDateTime,
		Guid currentUserId);

	Task<AppointmentOperationResult> CreateAsync(
		int patientId,
		int doctorId,
		DateTime appointmentDateTime,
		string? reason,
		Guid currentUserId);
}