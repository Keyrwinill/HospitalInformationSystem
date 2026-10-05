using HospitalInformationSystem.Data;
using HospitalInformationSystem.Models.Constants;
using HospitalInformationSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HospitalInformationSystem.Services;

public class UserService : IUserService
{
	private readonly HospitalDbContext _context;
	private readonly IAuditService _auditService;
	private readonly PasswordService _passwordService;

	public UserService(
		HospitalDbContext context,
		IAuditService auditService,
		PasswordService passwordService)
	{
		_context = context;
		_auditService = auditService;
		_passwordService = passwordService;
	}

	public async Task<UserOperationResult> DeactivateAsync(
		Guid userId,
		Guid currentUserId)
	{
		var user = await _context.Users
			.FirstOrDefaultAsync(x => x.Id == userId);

		if (user == null)
		{
			return UserOperationResult.NotFound;
		}

		if (!user.IsActive)
		{
			return UserOperationResult.Inactive;
		}

		// Doctor lifecycle is managed through DoctorService.
		if (user.Role == UserRoles.Doctor)
		{
			return UserOperationResult.DoctorManagedSeparately;
		}

		if (user.Id == currentUserId)
		{
			return UserOperationResult.CannotDeactivateSelf;
		}

		user.IsActive = false;

		await _auditService.LogAsync(
			currentUserId,
			"DeactivateUser",
			"User",
			user.Id.ToString());

		await _context.SaveChangesAsync();

		return UserOperationResult.Success;
	}

	public async Task<UserOperationResult> ActivateAsync(
		Guid userId,
		Guid currentUserId)
	{
		var user = await _context.Users
			.FirstOrDefaultAsync(x => x.Id == userId);

		if (user == null)
		{
			return UserOperationResult.NotFound;
		}

		if (user.IsActive)
		{
			return UserOperationResult.AlreadyActive;
		}

		// Doctor lifecycle is managed through DoctorService.
		if (user.Role == UserRoles.Doctor)
		{
			return UserOperationResult.DoctorManagedSeparately;
		}

		user.IsActive = true;

		await _auditService.LogAsync(
			currentUserId,
			"ActivateUser",
			"User",
			user.Id.ToString());

		await _context.SaveChangesAsync();

		return UserOperationResult.Success;
	}

	public async Task<UserOperationResult> CreateAsync(
	string account,
	string email,
	string firstName,
	string lastName,
	string role,
	string password,
	Guid currentUserId)
	{
		var allowedRoles = new[]
		{
		UserRoles.Admin,
		UserRoles.Receptionist
	};

		if (!allowedRoles.Contains(role))
		{
			return UserOperationResult.InvalidRole;
		}

		var accountExists = await _context.Users
			.AnyAsync(x => x.Account == account);

		if (accountExists)
		{
			return UserOperationResult.DuplicateAccount;
		}

		var emailExists = await _context.Users
			.AnyAsync(x => x.Email == email);

		if (emailExists)
		{
			return UserOperationResult.DuplicateEmail;
		}

		var user = new User
		{
			Id = Guid.NewGuid(),
			Account = account,
			Email = email,
			FirstName = firstName,
			LastName = lastName,
			Role = role,
			IsActive = true
		};

		user.PasswordHash =
			_passwordService.HashPassword(user, password);

		_context.Users.Add(user);

		await _auditService.LogAsync(
			currentUserId,
			"CreateUser",
			"User",
			user.Id.ToString());

		await _context.SaveChangesAsync();

		return UserOperationResult.Success;
	}
}