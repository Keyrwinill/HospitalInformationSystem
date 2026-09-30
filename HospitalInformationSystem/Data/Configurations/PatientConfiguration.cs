using HospitalInformationSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalInformationSystem.Data.Configurations;

public class PatientConfiguration
	: IEntityTypeConfiguration<Patient>
{
	public void Configure(EntityTypeBuilder<Patient> builder)
	{
		builder.ToTable("Patients");

		builder.HasKey(x => x.Id);

		builder.Property(x => x.MedicalRecordNumber)
			.HasMaxLength(20)
			.IsRequired();

		builder.HasIndex(x => x.MedicalRecordNumber)
			.IsUnique();

		builder.Property(x => x.FirstName)
			.HasMaxLength(50)
			.IsRequired();

		builder.Property(x => x.LastName)
			.HasMaxLength(50)
			.IsRequired();

		builder.Property(x => x.Gender)
			.HasMaxLength(20)
			.IsRequired();

		builder.Property(x => x.Phone)
			.HasMaxLength(30);

		builder.Property(x => x.Address)
			.HasMaxLength(200);

		builder.Property(x => x.CreatedAt)
			.HasDefaultValueSql("GETDATE()");
	}
}