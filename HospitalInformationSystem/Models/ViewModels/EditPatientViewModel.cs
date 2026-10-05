using System.ComponentModel.DataAnnotations;

namespace HospitalInformationSystem.Models.ViewModels;

public class EditPatientViewModel : IValidatableObject
{
	public int Id { get; set; }

	[Required]
	[MaxLength(20)]
	[Display(Name = "Medical Record Number")]
	public string MedicalRecordNumber { get; set; } = string.Empty;

	[Required]
	[MaxLength(50)]
	[Display(Name = "First Name")]
	public string FirstName { get; set; } = string.Empty;

	[Required]
	[MaxLength(50)]
	[Display(Name = "Last Name")]
	public string LastName { get; set; } = string.Empty;

	[Required]
	public DateOnly Birthday { get; set; }

	[Required]
	[MaxLength(20)]
	public string Gender { get; set; } = string.Empty;

	[MaxLength(30)]
	[Phone]
	public string? Phone { get; set; }

	[MaxLength(200)]
	public string? Address { get; set; }

	public IEnumerable<ValidationResult> Validate(
	ValidationContext validationContext)
	{
		if (Birthday > DateOnly.FromDateTime(DateTime.Today))
		{
			yield return new ValidationResult(
				"Birthday cannot be in the future.",
				new[] { nameof(Birthday) });
		}
	}
}