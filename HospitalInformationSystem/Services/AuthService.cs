using HospitalInformationSystem.Data;
using HospitalInformationSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HospitalInformationSystem.Services;

public class AuthService
{
	private readonly HospitalDbContext _context;
	private readonly PasswordService _passwordService;

	public AuthService(
		HospitalDbContext context,
		PasswordService passwordService)
	{
		_context = context;
		_passwordService = passwordService;
	}

	public async Task<User?> ValidateUserAsync(
		string account,
		string password)
	{
		var user = await _context.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Account == account);

		if (user == null || !user.IsActive)
		{
			return null;
		}

		var passwordValid = _passwordService.VerifyPassword(
			user,
			user.PasswordHash,
			password);

		if (!passwordValid)
		{
			return null;
		}

		return user;
	}
}