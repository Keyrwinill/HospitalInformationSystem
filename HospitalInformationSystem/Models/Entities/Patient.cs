using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalInformationSystem.Models.Entities;

public class Patient
{
	public int Id { get; set; }

	public string MedicalRecordNumber { get; set; } = string.Empty;

	public string FirstName { get; set; } = string.Empty;

	public string LastName { get; set; } = string.Empty;

	[NotMapped]
	public string FullName => $"{FirstName} {LastName}";

	public DateOnly Birthday { get; set; }

	[NotMapped]
	public int Age
	{
		get
		{
			var today = DateOnly.FromDateTime(DateTime.Today);
			var age = today.Year - Birthday.Year;

			if (Birthday > today.AddYears(-age))
			{
				age--;
			}

			return age;
		}
	}

	public string Gender { get; set; } = string.Empty;

	public string? Phone { get; set; }

	public string? Address { get; set; }

	public DateTime CreatedAt { get; set; }

	public bool IsActive { get; set; } = true;

	// Navigation properties
	public ICollection<Appointment> Appointments { get; set; } = [];

	public ICollection<Visit> Visits { get; set; } = [];
}