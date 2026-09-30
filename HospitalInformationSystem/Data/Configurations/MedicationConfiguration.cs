using HospitalInformationSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalInformationSystem.Data.Configurations;

public class MedicationConfiguration
	: IEntityTypeConfiguration<Medication>
{
	public void Configure(EntityTypeBuilder<Medication> builder)
	{
		builder.ToTable("Medications");

		builder.HasKey(x => x.Id);

		builder.Property(x => x.Code)
			.HasMaxLength(30)
			.IsRequired();

		builder.HasIndex(x => x.Code)
			.IsUnique();

		builder.Property(x => x.Name)
			.HasMaxLength(100)
			.IsRequired();

		builder.Property(x => x.Unit)
			.HasMaxLength(30)
			.IsRequired();

		builder.Property(x => x.IsActive)
			.HasDefaultValue(true);
	}
}