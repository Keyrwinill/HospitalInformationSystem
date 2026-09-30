using HospitalInformationSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalInformationSystem.Data.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
	public void Configure(EntityTypeBuilder<Doctor> builder)
	{
		builder.ToTable("Doctors");

		builder.HasKey(x => x.Id);

		builder.Property(x => x.LicenseNumber)
			.HasMaxLength(50)
			.IsRequired();

		builder.HasIndex(x => x.LicenseNumber)
			.IsUnique();

		builder.Property(x => x.IsActive)
			.HasDefaultValue(true);

		// Department 1 -> many Doctors
		builder.HasOne(x => x.Department)
			.WithMany(x => x.Doctors)
			.HasForeignKey(x => x.DepartmentId)
			.OnDelete(DeleteBehavior.Restrict);

		// User 1 -> 0 or 1 Doctor
		builder.HasOne(x => x.User)
			.WithOne(x => x.Doctor)
			.HasForeignKey<Doctor>(x => x.UserId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}