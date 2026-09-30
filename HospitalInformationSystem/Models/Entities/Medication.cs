namespace HospitalInformationSystem.Models.Entities;

public class Medication
{
	public int Id { get; set; }

	public string Code { get; set; } = string.Empty;

	public string Name { get; set; } = string.Empty;

	public string Unit { get; set; } = string.Empty;

	public bool IsActive { get; set; } = true;

	// Navigation property
	public ICollection<PrescriptionItem> PrescriptionItems { get; set; } = [];
}