using System.ComponentModel.DataAnnotations;

namespace HospitalInformationSystem.Models.ViewModels;

public class CreateMedicationViewModel
{
	[Required]
	[MaxLength(30)]
	public string Code { get; set; } = string.Empty;

	[Required]
	[MaxLength(100)]
	public string Name { get; set; } = string.Empty;

	[Required]
	[MaxLength(30)]
	public string Unit { get; set; } = string.Empty;
}