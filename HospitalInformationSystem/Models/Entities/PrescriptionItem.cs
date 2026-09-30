namespace HospitalInformationSystem.Models.Entities;

public class PrescriptionItem
{
	public int Id { get; set; }

	public int PrescriptionId { get; set; }

	public int MedicationId { get; set; }

	public string Dosage { get; set; } = string.Empty;

	public string Frequency { get; set; } = string.Empty;

	public int Days { get; set; }

	// Navigation properties
	public Prescription Prescription { get; set; } = null!;

	public Medication Medication { get; set; } = null!;
}