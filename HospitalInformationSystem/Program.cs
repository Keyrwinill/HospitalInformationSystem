using HospitalInformationSystem.Data;
using HospitalInformationSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<HospitalDbContext>(options =>
	options.UseSqlServer(
		builder.Configuration.GetConnectionString("DefaultConnection")));

// Authentication and security services.
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CustomCookieAuthenticationEvents>();

// Application services.
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IMedicationService, MedicationService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IVisitService, VisitService>();

builder.Services
	.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(options =>
	{
		// Redirect unauthenticated users here when authentication is required.
		options.LoginPath = "/Account/Login";

		// Redirect authenticated users here when they do not have permission.
		options.AccessDeniedPath = "/Account/AccessDenied";

		// Prevent JavaScript from reading the authentication cookie.
		options.Cookie.HttpOnly = true;

		// Send the authentication cookie only over HTTPS.
		options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

		// Helps reduce cross-site cookie exposure while allowing normal navigation.
		options.Cookie.SameSite = SameSiteMode.Lax;

		// Authentication cookie expires after 30 minutes.
		options.ExpireTimeSpan = TimeSpan.FromMinutes(30);

		// Refresh the expiration period when an active user makes requests.
		options.SlidingExpiration = true;

		// Use a custom events class to validate the user's active status on each request.
		options.EventsType = typeof(CustomCookieAuthenticationEvents);
	});

builder.Services.AddAuthorization();

var app = builder.Build();

// Seed development-only initial data.
// A scope is created manually because database seeding runs during
// application startup rather than inside a normal HTTP request.
if (app.Environment.IsDevelopment())
{
	using var scope = app.Services.CreateScope();

	// Resolve scoped services from the startup scope.
	var context = scope.ServiceProvider
		.GetRequiredService<HospitalDbContext>();

	var passwordService = scope.ServiceProvider
		.GetRequiredService<PasswordService>();

	// Create the initial Admin account if the database has no users.
	await DbSeeder.SeedAsync(context, passwordService);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}")
	.WithStaticAssets();


app.Run();
