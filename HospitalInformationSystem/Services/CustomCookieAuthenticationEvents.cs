using System.Security.Claims;
using HospitalInformationSystem.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace HospitalInformationSystem.Services;

public class CustomCookieAuthenticationEvents : CookieAuthenticationEvents
{
	private readonly HospitalDbContext _context;

	public CustomCookieAuthenticationEvents(
		HospitalDbContext context)
	{
		_context = context;
	}

	public override async Task ValidatePrincipal(
		CookieValidatePrincipalContext context)
	{
		var userIdValue = context.Principal?
			.FindFirstValue(ClaimTypes.NameIdentifier);

		if (!Guid.TryParse(userIdValue, out var userId))
		{
			context.RejectPrincipal();

			await context.HttpContext.SignOutAsync(
				CookieAuthenticationDefaults.AuthenticationScheme);

			return;
		}

		var isActive = await _context.Users
			.AsNoTracking()
			.AnyAsync(x =>
				x.Id == userId &&
				x.IsActive);

		if (!isActive)
		{
			context.RejectPrincipal();

			await context.HttpContext.SignOutAsync(
				CookieAuthenticationDefaults.AuthenticationScheme);
		}
	}
}