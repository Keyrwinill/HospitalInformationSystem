using System.ComponentModel.DataAnnotations;

namespace HospitalInformationSystem.Models.ViewModels;

public class CreateAppointmentViewModel
{
	[Required]
	public int? PatientId { get; set; }

	[Required]
	public int? DoctorId { get; set; }

	[Required]
	[DataType(DataType.DateTime)]
	public DateTime? AppointmentDateTime { get; set; }

	[MaxLength(500)]
	public string? Reason { get; set; }
}