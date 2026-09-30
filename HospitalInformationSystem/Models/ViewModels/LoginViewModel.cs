using System.ComponentModel.DataAnnotations;

namespace HospitalInformationSystem.Models.ViewModels;

public class LoginViewModel
{
	[Required]
	[MaxLength(30)]
	public string Account { get; set; } = string.Empty;

	[Required]
	[DataType(DataType.Password)]
	public string Password { get; set; } = string.Empty;

	public bool RememberMe { get; set; }
}