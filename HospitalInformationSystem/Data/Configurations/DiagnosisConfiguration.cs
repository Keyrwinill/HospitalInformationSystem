using HospitalInformationSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalInformationSystem.Data.Configurations;

public class DiagnosisConfiguration
	: IEntityTypeConfiguration<Diagnosis>
{
	public void Configure(EntityTypeBuilder<Diagnosis> builder)
	{
		builder.ToTable("Diagnoses");

		builder.HasKey(x => x.Id);

		builder.Property(x => x.DiagnosisCode)
			.HasMaxLength(20)
			.IsRequired();

		builder.Property(x => x.Description)
			.HasMaxLength(500)
			.IsRequired();

		builder.Property(x => x.CreatedAt)
			.HasDefaultValueSql("GETDATE()");

		builder.HasOne(x => x.Visit)
			.WithMany(x => x.Diagnoses)
			.HasForeignKey(x => x.VisitId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}