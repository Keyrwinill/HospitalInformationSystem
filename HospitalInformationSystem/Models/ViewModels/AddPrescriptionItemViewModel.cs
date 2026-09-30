using System.ComponentModel.DataAnnotations;

namespace HospitalInformationSystem.Models.ViewModels;

public class AddPrescriptionItemViewModel
{
	[Required]
	public int VisitId { get; set; }

	[Required]
	[Display(Name = "Medication")]
	public int? MedicationId { get; set; }

	[Required]
	[MaxLength(100)]
	public string Dosage { get; set; } = string.Empty;

	[Required]
	[MaxLength(100)]
	public string Frequency { get; set; } = string.Empty;

	[Required]
	[Range(1, 365)]
	public int? Days { get; set; }
}