namespace HospitalInformationSystem.Models.Entities;

public class Visit
{
	public int Id { get; set; }

	public int PatientId { get; set; }

	public int DoctorId { get; set; }

	public int? AppointmentId { get; set; }

	public DateTime VisitDateTime { get; set; }

	public string? ChiefComplaint { get; set; }

	public string? Notes { get; set; }

	public DateTime CreatedAt { get; set; }

	// Navigation properties
	public Patient Patient { get; set; } = null!;

	public Doctor Doctor { get; set; } = null!;

	public Appointment? Appointment { get; set; }

	public ICollection<Diagnosis> Diagnoses { get; set; } = [];

	public Prescription? Prescription { get; set; }
}