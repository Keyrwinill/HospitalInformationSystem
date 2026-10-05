using System.ComponentModel.DataAnnotations;

namespace HospitalInformationSystem.Models.ViewModels;

public class CreateDiagnosisViewModel
{
	public int VisitId { get; set; }

	[Required]
	[MaxLength(20)]
	[Display(Name = "Diagnosis Code")]
	public string DiagnosisCode { get; set; } = string.Empty;

	[Required]
	[MaxLength(500)]
	public string Description { get; set; } = string.Empty;
}