using HospitalInformationSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalInformationSystem.Data.Configurations;

public class AppointmentConfiguration
	: IEntityTypeConfiguration<Appointment>
{
	public void Configure(EntityTypeBuilder<Appointment> builder)
	{
		builder.ToTable("Appointments");

		builder.HasKey(x => x.Id);

		builder.Property(x => x.AppointmentDateTime)
			.IsRequired();

		builder.Property(x => x.Status)
			.HasConversion<int>()
			.HasDefaultValue(AppointmentStatus.Scheduled);

		builder.Property(x => x.Reason)
			.HasMaxLength(500);

		builder.Property(x => x.CreatedAt)
			.HasDefaultValueSql("GETDATE()");

		// Patient 1 -> many Appointments
		builder.HasOne(x => x.Patient)
			.WithMany(x => x.Appointments)
			.HasForeignKey(x => x.PatientId)
			.OnDelete(DeleteBehavior.Restrict);

		// Doctor 1 -> many Appointments
		builder.HasOne(x => x.Doctor)
			.WithMany(x => x.Appointments)
			.HasForeignKey(x => x.DoctorId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}