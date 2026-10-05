using HospitalInformationSystem.Data;
using HospitalInformationSystem.Models.Constants;
using HospitalInformationSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalInformationSystem.Controllers;

[Authorize(Roles = UserRoles.Admin)]
public class AuditLogsController : Controller
{
	private readonly HospitalDbContext _context;

	public AuditLogsController(HospitalDbContext context)
	{
		_context = context;
	}

	public async Task<IActionResult> Index(
		string? search,
		string? entity,
		DateOnly? fromDate,
		DateOnly? toDate,
		int page = 1)
	{
		if (page < 1)
		{
			page = 1;
		}

		const int pageSize = 20;

		var query = _context.AuditLogs
			.AsNoTracking()
			.AsQueryable();

		var hasInvalidDateRange =
			fromDate.HasValue &&
			toDate.HasValue &&
			fromDate.Value > toDate.Value;

		if (hasInvalidDateRange)
		{
			ModelState.AddModelError(
				string.Empty,
				"From date cannot be later than To date.");
		}

		if (!string.IsNullOrWhiteSpace(search))
		{
			query = query.Where(x => x.Action.Contains(search));
		}

		if (!string.IsNullOrWhiteSpace(entity))
		{
			query = query.Where(x => x.EntityName == entity);
		}

		if (!hasInvalidDateRange && fromDate.HasValue)
		{
			var start = fromDate.Value.ToDateTime(TimeOnly.MinValue);

			query = query.Where(x => x.CreatedAt >= start);
		}

		if (!hasInvalidDateRange && toDate.HasValue)
		{
			var end = toDate.Value
				.AddDays(1)
				.ToDateTime(TimeOnly.MinValue);

			query = query.Where(x => x.CreatedAt < end);
		}

		var totalCount = await query.CountAsync();

		var totalPages = (int)Math.Ceiling(
			totalCount / (double)pageSize);

		if (totalPages > 0 && page > totalPages)
		{
			page = totalPages;
		}

		var auditLogs = await query
			.OrderByDescending(x => x.CreatedAt)
			.ThenByDescending(x => x.Id)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(log => new AuditLogListItemViewModel
			{
				Id = log.Id,
				UserId = log.UserId,

				UserAccount = _context.Users
					.Where(user => user.Id == log.UserId)
					.Select(user => user.Account)
					.FirstOrDefault(),

				Action = log.Action,
				EntityName = log.EntityName,
				EntityId = log.EntityId,
				CreatedAt = log.CreatedAt
			})
			.ToListAsync();

		ViewBag.Search = search;
		ViewBag.Entity = entity;
		ViewBag.FromDate = fromDate;
		ViewBag.ToDate = toDate;
		ViewBag.Page = page;
		ViewBag.TotalPages = totalPages;

		return View(auditLogs);
	}
}