namespace HospitalInformationSystem.Models.Entities;

public class User
{
	public Guid Id { get; set; }

	public string Account { get; set; } = string.Empty;

	public string Email { get; set; } = string.Empty;

	public string PasswordHash { get; set; } = string.Empty;

	public string FirstName { get; set; } = string.Empty;

	public string LastName { get; set; } = string.Empty;

	public string Role { get; set; } = string.Empty;

	public bool IsActive { get; set; } = true;

	public DateTime CreatedAt { get; set; }

	public Doctor? Doctor { get; set; }
}