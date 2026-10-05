using System.ComponentModel.DataAnnotations;

namespace HospitalInformationSystem.Models.ViewModels;

public class CreateDoctorViewModel
{
	// User account information

	[Required]
	[MaxLength(30)]
	public string Account { get; set; } = string.Empty;

	[Required]
	[EmailAddress]
	[MaxLength(100)]
	public string Email { get; set; } = string.Empty;

	[Required]
	[MaxLength(30)]
	public string FirstName { get; set; } = string.Empty;

	[Required]
	[MaxLength(30)]
	public string LastName { get; set; } = string.Empty;

	[Required]
	[MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
	[DataType(DataType.Password)]
	public string Password { get; set; } = string.Empty;

	[Required]
	[DataType(DataType.Password)]
	[Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
	public string ConfirmPassword { get; set; } = string.Empty;


	// Doctor information

	[Required]
	[MaxLength(50)]
	public string LicenseNumber { get; set; } = string.Empty;

	[Required]
	public int? DepartmentId { get; set; }
}