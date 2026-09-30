namespace HospitalInformationSystem.Models.Entities;

public class Patient
{
	public int Id { get; set; }

	public string MedicalRecordNumber { get; set; } = string.Empty;

	public string FirstName { get; set; } = string.Empty;

	public string LastName { get; set; } = string.Empty;

	public DateOnly Birthday { get; set; }

	public string Gender { get; set; } = string.Empty;

	public string? Phone { get; set; }

	public string? Address { get; set; }

	public DateTime CreatedAt { get; set; }

	public ICollection<Appointment> Appointments { get; set; } = [];

	public ICollection<Visit> Visits { get; set; } = [];
}