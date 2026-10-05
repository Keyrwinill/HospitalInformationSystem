using System.ComponentModel.DataAnnotations;

namespace HospitalInformationSystem.Models.ViewModels;

public class EditDoctorViewModel
{
	public int Id { get; set; }

	[Required]
	[MaxLength(30)]
	[Display(Name = "First Name")]
	public string FirstName { get; set; } = string.Empty;

	[Required]
	[MaxLength(30)]
	[Display(Name = "Last Name")]
	public string LastName { get; set; } = string.Empty;

	[Required]
	[EmailAddress]
	[MaxLength(100)]
	public string Email { get; set; } = string.Empty;

	[Required]
	[MaxLength(50)]
	[Display(Name = "License Number")]
	public string LicenseNumber { get; set; } = string.Empty;

	[Required]
	[Display(Name = "Department")]
	public int? DepartmentId { get; set; }
}