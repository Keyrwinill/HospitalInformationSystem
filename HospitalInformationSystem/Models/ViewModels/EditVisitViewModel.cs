using System.ComponentModel.DataAnnotations;

namespace HospitalInformationSystem.Models.ViewModels;

public class EditVisitViewModel
{
	public int Id { get; set; }

	[MaxLength(500)]
	[Display(Name = "Chief Complaint")]
	public string? ChiefComplaint { get; set; }

	[MaxLength(2000)]
	public string? Notes { get; set; }
}