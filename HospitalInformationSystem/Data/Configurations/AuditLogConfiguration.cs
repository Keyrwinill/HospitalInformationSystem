using HospitalInformationSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalInformationSystem.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
	public void Configure(EntityTypeBuilder<AuditLog> builder)
	{
		builder.HasKey(x => x.Id);

		builder.Property(x => x.Action)
			.HasMaxLength(100)
			.IsRequired();

		builder.Property(x => x.EntityName)
			.HasMaxLength(100)
			.IsRequired();

		builder.Property(x => x.EntityId)
			.HasMaxLength(100)
			.IsRequired();

		builder.Property(x => x.CreatedAt)
			.HasDefaultValueSql("GETDATE()");
	}
}