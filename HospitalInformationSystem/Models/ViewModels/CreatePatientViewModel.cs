using System.ComponentModel.DataAnnotations;

namespace HospitalInformationSystem.Models.ViewModels;

public class CreatePatientViewModel
{
	[Required]
	[MaxLength(20)]
	public string MedicalRecordNumber { get; set; } = string.Empty;

	[Required]
	[MaxLength(50)]
	public string FirstName { get; set; } = string.Empty;

	[Required]
	[MaxLength(50)]
	public string LastName { get; set; } = string.Empty;

	[Required]
	[DataType(DataType.Date)]
	public DateOnly? Birthday { get; set; }

	[Required]
	[MaxLength(20)]
	public string Gender { get; set; } = string.Empty;

	[MaxLength(30)]
	[Phone]
	public string? Phone { get; set; }

	[MaxLength(200)]
	public string? Address { get; set; }
}