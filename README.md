# Hospital Information System

A learning and portfolio project built with ASP.NET Core MVC, Entity Framework Core, and SQL Server.

The system demonstrates common hospital information workflows, including patient management, doctor and department management, appointment scheduling, clinical visits, diagnoses, prescriptions, user authorization, and audit logging.

## Technologies

- ASP.NET Core MVC (.NET 10)
- Entity Framework Core 10
- SQL Server
- Cookie Authentication
- Role-based Authorization
- xUnit

## Features

### Authentication and Authorization
- Cookie-based authentication
- Role-based authorization for Admin, Doctor, and Receptionist
- Password hashing
- Active-user validation for authenticated sessions

### Administration
- User management
- Department management
- Doctor management
- Medication management
- Audit log viewer with filtering and pagination

### Patient and Appointment Management
- Patient registration and profile management
- Patient search and active/inactive status management
- Appointment scheduling and rescheduling
- Appointment cancellation and no-show handling
- Scheduling conflict validation

### Clinical Workflow
- Start a visit from a scheduled appointment
- Record chief complaints and clinical notes
- Add and manage diagnoses
- Add and manage prescription items
- Complete visits with business-rule validation

### Audit Logging
- Records important create, update, activate/deactivate, and clinical workflow actions
- Stores the acting user, action, entity type, entity identifier, and timestamp

## User Roles

The application uses three roles with different responsibilities:

| Role | Main Responsibilities |
| --- | --- |
| Admin | Manage users, departments, doctors, medications, and view audit logs |
| Receptionist | Manage patients and appointments |
| Doctor | View assigned appointments and manage clinical visits, diagnoses, and prescriptions |

## Architecture

The project uses a pragmatic layered structure:

- **Controllers** handle HTTP requests, model validation, authorization, and view navigation.
- **Services** contain business rules and application workflows such as appointment scheduling, visit management, and entity activation/deactivation.
- **Entity Framework Core** provides database access through `HospitalDbContext`.
- **Entity Configurations** define database relationships, constraints, indexes, and field configuration using Fluent API.
- **ViewModels** separate form and presentation data from database entities.
- **AuditService** records important state-changing operations.

Simple read-only queries may use `HospitalDbContext` directly in controllers, while workflows with meaningful business rules are handled by services.

A separate Repository layer is intentionally not used because EF Core already provides `DbContext` and `DbSet` abstractions for data access.

## Database Model

The main entities are:

- **User** — Application account and role information
- **Department** — Hospital department
- **Doctor** — Doctor profile linked to a User and Department
- **Patient** — Patient demographic and medical record information
- **Appointment** — Scheduled appointment between a Patient and Doctor
- **Visit** — Clinical encounter, optionally created from an Appointment
- **Diagnosis** — Diagnosis recorded during a Visit
- **Medication** — Medication master data
- **Prescription** — Prescription associated with a Visit
- **PrescriptionItem** — Medication, dosage, frequency, and duration within a Prescription
- **AuditLog** — History of important application actions

Key relationships include:

```text
Department 1 ─── N Doctor
User       1 ─── 0..1 Doctor

Patient    1 ─── N Appointment
Doctor     1 ─── N Appointment

Patient    1 ─── N Visit
Doctor     1 ─── N Visit
Appointment 1 ── 0..1 Visit

Visit      1 ─── N Diagnosis
Visit      1 ─── 0..1 Prescription
Prescription 1 ─ N PrescriptionItem
Medication   1 ─ N PrescriptionItem
```

## Business Rules

Examples of business rules implemented in the service layer include:

- Appointments must be scheduled for a future date and time.
- A doctor or patient cannot have conflicting non-cancelled appointments at the same date and time.
- Appointments can only be cancelled while they are still scheduled and have not started a visit.
- Only past scheduled appointments can be marked as no-show.
- A visit can only be started from a valid scheduled appointment.
- Inactive patients, doctors, doctor accounts, or departments cannot be used to start new visits.
- Completed visits cannot be modified.
- A visit requires at least one diagnosis before it can be completed.
- Prescription dosage and frequency are required, and prescription duration must be greater than zero.
- Patients and doctors with active clinical workflows cannot be deactivated when doing so would invalidate those workflows.

## Automated Tests

The solution includes an xUnit test project covering important service-layer business rules.

Current test suite:

- 113 automated tests
- Appointment workflow and scheduling rules
- Visit and clinical workflow rules
- Patient activation and deactivation rules
- Doctor activation, deactivation, and update rules
- Department management rules
- Medication activation and deactivation rules
- User creation, activation, and deactivation rules
- Audit log creation for state-changing operations

The service tests use the EF Core InMemory provider to isolate and verify application business logic.

The InMemory provider is not intended to validate SQL Server-specific behavior such as relational constraints or transaction semantics.

## Getting Started

### Prerequisites

To run the project locally, install:

- .NET 10 SDK
- SQL Server
- Visual Studio 2026 or another .NET-compatible IDE
- Entity Framework Core tools

### Database Setup

The application uses SQL Server with Entity Framework Core Code First migrations.

The default development connection string is:

```json
"DefaultConnection": "Data Source=localhost;Database=HospitalInformationDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

Update the connection string in `appsettings.json` if your SQL Server environment is different.

From the project directory, apply the existing migrations:

```powershell
dotnet ef database update
```

This creates or updates the `HospitalInformationDb` database using the migrations included in the project.

### Initial Administrator

In the Development environment, the application creates an initial administrator account when the `Users` table is empty.

```text
Account: admin
Password: Admin123!
```

This account is intended for local development and demonstration only.

After signing in as Admin, additional Admin or Receptionist accounts can be created through the user management interface. Doctor accounts are created as part of doctor management.

### Run the Application

From the project directory:

```powershell
dotnet run
```

Then open the HTTPS URL shown in the terminal.

> The initial administrator credentials and automatic database seeding are intended for development only and should not be used as a production provisioning strategy.

## Project Structure

```text
HospitalInformationSystem/
├── Controllers/        # MVC controllers and HTTP request handling
├── Data/
│   ├── Configurations/ # EF Core entity configurations
│   ├── DbSeeder.cs
│   └── HospitalDbContext.cs
├── Migrations/         # EF Core database migrations
├── Models/
│   ├── Constants/
│   ├── Entities/
│   └── ViewModels/
├── Services/           # Business logic, authentication, and application workflows
├── Views/              # Razor views
├── wwwroot/            # Static assets
├── Program.cs          # Dependency injection and application configuration
└── appsettings.json    # Application and database configuration

HospitalInformationSystem.Tests/
└── ...                 # xUnit service-layer tests
```

## Security Notes

The project includes several security-related practices for learning and demonstration purposes:

- Passwords are stored as salted hashes rather than plaintext.
- Authentication uses ASP.NET Core Cookie Authentication.
- Authentication cookies are configured as `HttpOnly`, `Secure`, and `SameSite=Lax`.
- Role-based authorization restricts functionality by user role.
- Authenticated users are revalidated against their active account status.
- State-changing form actions use anti-forgery validation.
- Inactive users cannot authenticate or continue using an existing authenticated session.

This project is intended as a learning and portfolio application and has not undergone a production security review.

## Project Status

The core scope of this learning project is complete.

Implemented areas include authentication and authorization, administration, patient and appointment management, clinical visit workflows, prescriptions, audit logging, business-rule validation, and automated service-layer testing.

Possible future improvements include integration testing with SQL Server, enhanced error handling and logging, deployment configuration, and additional production security hardening.
