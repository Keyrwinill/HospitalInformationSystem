using System.ComponentModel.DataAnnotations;

namespace HospitalInformationSystem.Models.ViewModels;

public class RescheduleAppointmentViewModel
{
	public int Id { get; set; }

	[Required]
	[Display(Name = "Appointment Date and Time")]
	public DateTime AppointmentDateTime { get; set; }
}