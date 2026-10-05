using HospitalInformationSystem.Data;
using HospitalInformationSystem.Models.Constants;
using Microsoft.EntityFrameworkCore;

namespace HospitalInformationSystem.Services;

public class MedicationService : IMedicationService
{
	private readonly HospitalDbContext _context;
	private readonly IAuditService _auditService;

	public MedicationService(
		HospitalDbContext context,
		IAuditService auditService)
	{
		_context = context;
		_auditService = auditService;
	}

	public async Task<MedicationOperationResult> DeactivateAsync(
		int medicationId,
		Guid currentUserId)
	{
		var medication = await _context.Medications
			.FirstOrDefaultAsync(x => x.Id == medicationId);

		if (medication == null)
		{
			return MedicationOperationResult.NotFound;
		}

		if (!medication.IsActive)
		{
			return MedicationOperationResult.Inactive;
		}

		medication.IsActive = false;

		await _auditService.LogAsync(
			currentUserId,
			"DeactivateMedication",
			"Medication",
			medication.Id.ToString());

		await _context.SaveChangesAsync();

		return MedicationOperationResult.Success;
	}

	public async Task<MedicationOperationResult> ActivateAsync(
		int medicationId,
		Guid currentUserId)
	{
		var medication = await _context.Medications
			.FirstOrDefaultAsync(x => x.Id == medicationId);

		if (medication == null)
		{
			return MedicationOperationResult.NotFound;
		}

		if (medication.IsActive)
		{
			return MedicationOperationResult.AlreadyActive;
		}

		medication.IsActive = true;

		await _auditService.LogAsync(
			currentUserId,
			"ActivateMedication",
			"Medication",
			medication.Id.ToString());

		await _context.SaveChangesAsync();

		return MedicationOperationResult.Success;
	}
}