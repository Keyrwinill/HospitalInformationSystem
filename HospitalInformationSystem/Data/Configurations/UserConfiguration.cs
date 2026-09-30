using HospitalInformationSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalInformationSystem.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
	public void Configure(EntityTypeBuilder<User> builder)
	{
		builder.ToTable("Users");

		builder.HasKey(x => x.Id);

		builder.Property(x => x.Account)
			.HasMaxLength(30)
			.IsRequired();

		builder.HasIndex(x => x.Account)
			.IsUnique();

		builder.Property(x => x.Email)
			.HasMaxLength(100)
			.IsRequired();

		builder.HasIndex(x => x.Email)
			.IsUnique();

		builder.Property(x => x.PasswordHash)
			.HasMaxLength(255)
			.IsRequired();

		builder.Property(x => x.FirstName)
			.HasMaxLength(30)
			.IsRequired();

		builder.Property(x => x.LastName)
			.HasMaxLength(30)
			.IsRequired();

		builder.Property(x => x.Role)
			.HasMaxLength(20)
			.IsRequired();

		builder.Property(x => x.IsActive)
			.HasDefaultValue(true);

		builder.Property(x => x.CreatedAt)
			.HasDefaultValueSql("GETDATE()");
	}
}