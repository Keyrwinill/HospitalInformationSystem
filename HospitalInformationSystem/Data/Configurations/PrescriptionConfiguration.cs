using HospitalInformationSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalInformationSystem.Data.Configurations;

public class PrescriptionConfiguration
	: IEntityTypeConfiguration<Prescription>
{
	public void Configure(EntityTypeBuilder<Prescription> builder)
	{
		builder.ToTable("Prescriptions");

		builder.HasKey(x => x.Id);

		builder.Property(x => x.CreatedAt)
			.HasDefaultValueSql("GETDATE()");

		// Visit 1 -> 0 or 1 Prescription
		builder.HasOne(x => x.Visit)
			.WithOne(x => x.Prescription)
			.HasForeignKey<Prescription>(x => x.VisitId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}