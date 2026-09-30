using HospitalInformationSystem.Models.Constants;
using HospitalInformationSystem.Models.Entities;
using HospitalInformationSystem.Services;
using Microsoft.EntityFrameworkCore;

namespace HospitalInformationSystem.Data;

public static class DbSeeder
{
	/// <summary>
	/// Creates the initial administrator account when the database
	/// does not contain any users.
	/// This allows the system to have its first Admin without
	/// providing a public registration page.
	/// </summary>
	public static async Task SeedAsync(
		HospitalDbContext context,
		PasswordService passwordService)
	{
		// Do not seed another Admin after users have already been created.
		if (await context.Users.AnyAsync())
		{
			return;
		}

		var admin = new User
		{
			Id = Guid.NewGuid(),
			Account = "admin",
			Email = "admin@hospital.local",
			FirstName = "System",
			LastName = "Administrator",
			Role = UserRoles.Admin,
			IsActive = true
		};

		// Never store the plaintext password in the database.
		// PasswordService generates a salted password hash for storage.
		admin.PasswordHash =
			passwordService.HashPassword(admin, "Admin123!");

		context.Users.Add(admin);

		// Persist the initial Admin account to the database.
		await context.SaveChangesAsync();
	}
}