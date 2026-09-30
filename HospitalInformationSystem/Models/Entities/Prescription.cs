namespace HospitalInformationSystem.Models.Entities;

public class Prescription
{
	public int Id { get; set; }

	public int VisitId { get; set; }

	public DateTime CreatedAt { get; set; }

	// Navigation property
	public Visit Visit { get; set; } = null!;

	public ICollection<PrescriptionItem> Items { get; set; } = [];
}