namespace HospitalInformationSystem.Models.Entities;

public class Diagnosis
{
	public int Id { get; set; }

	public int VisitId { get; set; }

	public string DiagnosisCode { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public DateTime CreatedAt { get; set; }

	// Navigation property
	public Visit Visit { get; set; } = null!;
}