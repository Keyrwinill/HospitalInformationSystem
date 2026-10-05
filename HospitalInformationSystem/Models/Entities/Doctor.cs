using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalInformationSystem.Models.Entities;

public class Doctor
{
	public int Id { get; set; }

	public Guid UserId { get; set; }

	public int DepartmentId { get; set; }

	public string LicenseNumber { get; set; } = string.Empty;

	[NotMapped]
	public string FullName =>
		User.FullName;

	public bool IsActive { get; set; } = true;

	// Navigation properties
	public User User { get; set; } = null!;

	public Department Department { get; set; } = null!;

	public ICollection<Appointment> Appointments { get; set; } = [];

	public ICollection<Visit> Visits { get; set; } = [];
}