using System.ComponentModel.DataAnnotations;

namespace HospitalInformationSystem.Models.ViewModels;

public class EditPrescriptionItemViewModel
{
	public int Id { get; set; }

	[Required]
	[MaxLength(100)]
	public string Dosage { get; set; } = string.Empty;

	[Required]
	[MaxLength(100)]
	public string Frequency { get; set; } = string.Empty;

	[Range(1, 365)]
	public int Days { get; set; }
}