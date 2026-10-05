using System.ComponentModel.DataAnnotations;

namespace HospitalInformationSystem.Models.ViewModels;

public class EditDepartmentViewModel
{
	public int Id { get; set; }

	[Required]
	[MaxLength(100)]
	public string Name { get; set; } = string.Empty;

	[MaxLength(500)]
	public string? Description { get; set; }
}