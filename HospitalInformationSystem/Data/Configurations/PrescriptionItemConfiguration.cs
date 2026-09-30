using HospitalInformationSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalInformationSystem.Data.Configurations;

public class PrescriptionItemConfiguration
	: IEntityTypeConfiguration<PrescriptionItem>
{
	public void Configure(EntityTypeBuilder<PrescriptionItem> builder)
	{
		builder.ToTable("PrescriptionItems");

		builder.HasKey(x => x.Id);

		builder.Property(x => x.Dosage)
			.HasMaxLength(100)
			.IsRequired();

		builder.Property(x => x.Frequency)
			.HasMaxLength(100)
			.IsRequired();

		// Prescription 1 -> many PrescriptionItems
		builder.HasOne(x => x.Prescription)
			.WithMany(x => x.Items)
			.HasForeignKey(x => x.PrescriptionId)
			.OnDelete(DeleteBehavior.Restrict);

		// Medication 1 -> many PrescriptionItems
		builder.HasOne(x => x.Medication)
			.WithMany(x => x.PrescriptionItems)
			.HasForeignKey(x => x.MedicationId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}