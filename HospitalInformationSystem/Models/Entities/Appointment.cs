namespace HospitalInformationSystem.Models.Entities;

public class Appointment
{
	public int Id { get; set; }

	public int PatientId { get; set; }

	public int DoctorId { get; set; }

	public DateTime AppointmentDateTime { get; set; }

	public AppointmentStatus Status { get; set; }
		= AppointmentStatus.Scheduled;

	public string? Reason { get; set; }

	public DateTime CreatedAt { get; set; }

	// Navigation properties
	public Patient Patient { get; set; } = null!;

	public Doctor Doctor { get; set; } = null!;

	public Visit? Visit { get; set; }
}