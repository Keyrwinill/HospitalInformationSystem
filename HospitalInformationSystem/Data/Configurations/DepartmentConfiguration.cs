using HospitalInformationSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalInformationSystem.Data.Configurations;

public class DepartmentConfiguration
	: IEntityTypeConfiguration<Department>
{
	public void Configure(EntityTypeBuilder<Department> builder)
	{
		builder.ToTable("Departments");

		builder.HasKey(x => x.Id);

		builder.Property(x => x.Name)
			.HasMaxLength(100)
			.IsRequired();

		builder.HasIndex(x => x.Name)
			.IsUnique();

		builder.Property(x => x.Description)
			.HasMaxLength(500);

		builder.Property(x => x.IsActive)
			.HasDefaultValue(true);
	}
}