using HospitalInformationSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HospitalInformationSystem.Data;

public class HospitalDbContext : DbContext
{
	public HospitalDbContext(DbContextOptions<HospitalDbContext> options)
		: base(options)
	{
	}
	
	public DbSet<User> Users { get; set; }

	public DbSet<Department> Departments { get; set; }

	public DbSet<Doctor> Doctors { get; set; }

	public DbSet<Patient> Patients { get; set; }

	public DbSet<Appointment> Appointments { get; set; }

	public DbSet<Visit> Visits { get; set; }

	public DbSet<Diagnosis> Diagnoses { get; set; }

	public DbSet<Medication> Medications { get; set; }

	public DbSet<Prescription> Prescriptions { get; set; }

	public DbSet<PrescriptionItem> PrescriptionItems { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.ApplyConfigurationsFromAssembly(
			typeof(HospitalDbContext).Assembly);
	}
}