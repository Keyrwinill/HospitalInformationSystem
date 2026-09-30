using System.ComponentModel.DataAnnotations;

namespace HospitalInformationSystem.Models.ViewModels;

public class CreateUserViewModel
{
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
	[DataType(DataType.Password)]
	public string Password { get; set; } = string.Empty;

	[Required]
	[DataType(DataType.Password)]
	[Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
	public string ConfirmPassword { get; set; } = string.Empty;

	[Required]
	public string Role { get; set; } = string.Empty;
}