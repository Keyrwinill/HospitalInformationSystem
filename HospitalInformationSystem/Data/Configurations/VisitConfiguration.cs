using HospitalInformationSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalInformationSystem.Data.Configurations;

public class VisitConfiguration : IEntityTypeConfiguration<Visit>
{
	public void Configure(EntityTypeBuilder<Visit> builder)
	{
		builder.ToTable("Visits");

		builder.HasKey(x => x.Id);

		builder.Property(x => x.VisitDateTime)
			.IsRequired();

		builder.Property(x => x.ChiefComplaint)
			.HasMaxLength(500);

		builder.Property(x => x.Notes)
			.HasMaxLength(2000);

		builder.Property(x => x.CreatedAt)
			.HasDefaultValueSql("GETDATE()");

		// Patient 1 -> many Visits
		builder.HasOne(x => x.Patient)
			.WithMany(x => x.Visits)
			.HasForeignKey(x => x.PatientId)
			.OnDelete(DeleteBehavior.Restrict);

		// Doctor 1 -> many Visits
		builder.HasOne(x => x.Doctor)
			.WithMany(x => x.Visits)
			.HasForeignKey(x => x.DoctorId)
			.OnDelete(DeleteBehavior.Restrict);

		// Appointment 1 -> 0 or 1 Visit
		builder.HasOne(x => x.Appointment)
			.WithOne(x => x.Visit)
			.HasForeignKey<Visit>(x => x.AppointmentId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}