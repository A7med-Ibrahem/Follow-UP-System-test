# 🏥 Smart Follow Up

<div align="center">

![Smart Follow Up](https://img.shields.io/badge/Smart%20Follow%20Up-Post--Op%20Patient%20Monitoring-00d4ff?style=for-the-badge&logo=heartbeat&logoColor=white)

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927?style=flat-square&logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![Entity Framework](https://img.shields.io/badge/EF%20Core-10-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://docs.microsoft.com/ef/)
[![Hangfire](https://img.shields.io/badge/Hangfire-Background%20Jobs-2C3E50?style=flat-square)](https://www.hangfire.io/)
[![SignalR](https://img.shields.io/badge/SignalR-Realtime-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/signalr)
[![JWT](https://img.shields.io/badge/JWT-Authentication-000000?style=flat-square&logo=jsonwebtokens&logoColor=white)](https://jwt.io/)
[![Swagger](https://img.shields.io/badge/Swagger-API%20Docs-85EA2D?style=flat-square&logo=swagger&logoColor=black)](https://swagger.io/)

**A post-operative patient monitoring platform connecting doctors and patients through daily health reports, automatic risk scoring, real-time alerts, and structured medication reminders.**

[Features](#-features) • [Tech Stack](#-tech-stack) • [Getting Started](#-getting-started) • [API Reference](#-api-reference) • [Architecture](#-architecture) • [Security](#-security)

</div>

---

## 📋 Overview

**Smart Follow Up** helps surgeons and clinics track patients remotely during recovery. A doctor opens a case right after an operation; the patient then submits daily health reports (temperature, pain level, swelling, bleeding, wound photos) from home. The system automatically classifies each report's risk level, escalates critical cases to the doctor in real time, and keeps the patient on schedule with their prescribed medication.

Three dedicated dashboards, one shared login:

| Role | What they can do |
|---|---|
| 👨‍⚕️ **Doctor** | Open and manage patient cases, review daily reports and wound-photo timelines, write prescriptions and clinical notes, get instant critical-case alerts |
| 🧑‍🦱 **Patient** | Submit and edit same-day daily reports, view their doctor's notes/prescriptions, follow a computed medication schedule with reminders, manage their own profile |
| 👑 **Admin** | Review and approve/reject doctor applications, manage doctor and patient accounts, view platform analytics, and audit every account-level action |

---

## ✨ Features

### Clinical workflow
- Case creation tied to an operation date and type, with automatic patient account provisioning
- Daily health reports with automatic risk classification (**Stable / Moderate / Critical**)
- Same-day report editing for patients; unrestricted editing for doctors, with risk level automatically recalculated on every edit
- Wound photo uploads, with the ability to remove a mistakenly uploaded photo before or after submission
- Doctor's notes and prescriptions, both pushed to the patient instantly

### Medication & reminders
- Doctors set **times per day** instead of typing free-text frequency — the system computes evenly-spaced dose times automatically
- A recurring background job checks every 30 minutes and sends a reminder notification exactly when a dose is due
- Patients confirm doses taken; adherence is tracked per scheduled slot

### Real-time & notifications
- SignalR-powered live notifications — no page refresh needed
- Clicking a notification navigates straight to the relevant page (a prescription notification opens Medications, a note opens Notes, a critical alert opens Alerts)
- Branded HTML email notifications for account welcome, password-reset OTP, prescription updates, doctor approval/rejection, and critical alerts — all sent as background jobs so a slow SMTP server never blocks an API response

### Access control & safety
- JWT authentication with 15-minute access tokens and rotating refresh tokens (race-condition-safe on the client)
- Forced password change on first login for any account created with a temporary password
- Role selection at login is verified against the account's real role — you cannot log into a doctor account through the patient tab or vice versa
- Every case-scoped endpoint (reports, prescriptions, notes, wound images) enforces ownership — a user can only ever reach their own data
- Rate limiting on login, forgot-password, and OTP verification, partitioned per IP address
- Server-side validation on every input: names must be text, phone numbers must be digits, emails must be valid, numeric ranges (pain level, temperature, age) are enforced both in the API and at the database level

### Admin tools
- Approve or reject doctor applications with a required rejection reason
- Full doctor and patient directories, including each patient's assigned doctor
- Activate/deactivate or permanently remove a doctor account (soft delete — history is preserved)
- Audit log of every admin action (who approved/rejected/deleted what, and when)

---

## 🛠 Tech Stack

### Backend
| Technology | Purpose |
|---|---|
| ASP.NET Core 10 (Web API) | REST API framework |
| Entity Framework Core 10 | ORM, migrations, and query filters |
| SQL Server | Relational database |
| JWT Bearer Authentication | Stateless auth with refresh-token rotation |
| FluentValidation | Request validation |
| BCrypt.Net | Password hashing |
| Hangfire (SQL Server storage) | Background jobs, recurring tasks, automatic retry |
| SignalR | Real-time push notifications |
| MailKit | SMTP email delivery |
| Swashbuckle (Swagger) | Interactive API documentation |

### Frontend
| Technology | Purpose |
|---|---|
| HTML5 + CSS3 | Structure & styling, no build step |
| Vanilla JavaScript | API integration, SignalR client, UI logic |
| Microsoft SignalR JS client | Live notification delivery |

### Infrastructure
- SQL Server (hosted, or containerized via Docker Compose for local dev)
- Docker & Docker Compose (optional)

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (local, remote, or via Docker)
- A modern web browser
- An SMTP provider for email (e.g. [Brevo](https://www.brevo.com), SendGrid). **Avoid personal Gmail accounts in production** — Google throttles automated sending far below the documented daily limit, and outbound mail will silently stop working.

### 1. Clone and configure

```bash
git clone <repository-url>
cd smartFollowup/smartFollowup.API
```

Set your own values in `appsettings.json` (or `dotnet user-secrets` for local development):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=SmartFollowUp;User Id=...;Password=...;"
  },
  "Jwt": {
    "Key": "a-long-random-secret-key",
    "Issuer": "SmartFollowUpAPI",
    "Audience": "SmartFollowUpClient"
  },
  "Email": {
    "Host": "smtp-relay.brevo.com",
    "Port": "587",
    "Username": "your-smtp-username",
    "Password": "your-smtp-key",
    "FromName": "Smart Follow Up"
  },
  "Frontend": {
    "LoginUrl": "http://localhost:5500/Frontend/login.html"
  }
}
```

### 2. Apply database migrations

```bash
dotnet ef database update
```

### 3. Run the API

```bash
dotnet run
```

- API base URL → `http://localhost:5290`
- Swagger UI → `http://localhost:5290/swagger`
- Hangfire dashboard → `http://localhost:5290/hangfire` *(local requests only by default — see [Security](#-security))*

### 4. Serve the frontend

`Frontend/` is a set of static HTML files with no build step — serve it with any static file server (VS Code Live Server, `npx serve`, IIS, etc.), then point the `API` constant at the top of each HTML file's `<script>` block to your running backend.

### Alternative: Docker Compose

```bash
cd smartFollowup
docker-compose up --build
```

Spins up a containerized SQL Server alongside the API for local development, isolated from any shared or production database. Requires a `.env` file in `smartFollowup/` with `JWT_KEY`, `EMAIL_USERNAME`, and `EMAIL_PASSWORD`.

---

## 🏗 Architecture

```
smartFollowup/
├── smartFollowup.slnx                 # Solution file
├── docker-compose.yml                 # Optional local API + SQL Server stack
└── smartFollowup.API/
    ├── Controllers/       # Auth, Cases, Reports, Prescriptions, Notes, WoundImages,
    │                      # Notifications, Alerts, Doctor, Patient, Admin
    ├── Services/          # Business logic + background job services
    │                      # (EmailService, EscalationService, MedicationReminderService...)
    ├── Models/            # EF Core entities
    ├── DTOs/               # Request / response contracts
    ├── Validators/        # FluentValidation rules
    ├── Data/               # AppDbContext + Fluent API configuration
    ├── Migrations/         # EF Core migrations
    ├── Hubs/                # SignalR NotificationHub
    ├── EmailTemplates/     # Branded HTML email templates
    ├── Enums/               # Shared enums
    └── appsettings.json     # DB, JWT, SMTP, frontend URL configuration

Frontend/
├── login.html               # Shared login + forgot-password flow
├── doctor-dashboard.html
├── patient-dashboard.html
├── admin-dashboard.html
├── case-details.html
├── prescriptions.html
├── reports.html
└── alerts.html
```

### Database schema (simplified)

```
Users ──────────── DoctorProfiles
  │                PatientProfiles
  │
  ├── Cases ─────── DailyReports ── AiAnalyses
  │     │                │
  │     │                └── WoundImages
  │     │
  │     ├── Prescriptions ── PrescriptionMedications ── MedicationAdherences
  │     ├── DoctorNotes
  │     └── Alerts ──────── Notifications
  │
  ├── DoctorRequests
  └── AuditLogs
```

### Background jobs (Hangfire)

| Job | Schedule | Purpose |
|---|---|---|
| `emergency-escalation` | Every 30 minutes | Escalates unresolved critical alerts to the assigned doctor via notification + email |
| `medication-reminders` | Every 30 minutes | Computes each patient's due medication doses and sends timed reminders |
| Email delivery | Enqueued on demand | Every outbound email runs as a background job with automatic retry (3 attempts, backing off over 1 → 5 → 15 minutes) |

---

## 📡 API Reference

Base path: `/api`

### Auth (`/Auth`)
| Method | Endpoint | Description | Auth |
|---|---|---|---|
| POST | `/login` | Log in (returns access + refresh token) | ❌ |
| POST | `/forgot-password` | Request a password-reset OTP | ❌ *(rate-limited)* |
| POST | `/verify-otp` | Verify an OTP before allowing a reset | ❌ *(rate-limited)* |
| POST | `/reset-password` | Reset password using a verified OTP | ❌ |
| POST | `/request-doctor` | Submit a doctor registration request | ❌ |
| POST | `/refresh-token` | Exchange a refresh token for a new access token | ❌ |
| POST | `/logout` | Invalidate the current refresh token | ✅ |
| POST | `/change-password` | Change password while logged in | ✅ |

### Cases (`/Cases`)
| Method | Endpoint | Description | Role |
|---|---|---|---|
| POST | `/` | Create a new patient case | Doctor |
| GET | `/` | List the doctor's cases | Doctor |
| GET | `/{id}` | Get a case by ID | Doctor |
| GET | `/search` | Search patients by name/phone | Doctor |
| PUT | `/{id}/close` | Close a case | Doctor |
| DELETE | `/{id}` | Delete a case | Doctor |

### Daily Reports (`/Reports`)
| Method | Endpoint | Description | Role |
|---|---|---|---|
| POST | `/` | Submit a daily report | Patient |
| GET | `/case/{caseId}` | Get a case's reports | Doctor, Patient *(own case only)* |
| PUT | `/{id}` | Edit a report (same-day for patients; anytime for doctors) | Doctor, Patient |

### Prescriptions (`/Prescriptions`)
| Method | Endpoint | Description | Role |
|---|---|---|---|
| POST | `/` | Create a prescription | Doctor |
| PUT | `/{id}` | Update a prescription | Doctor |
| GET | `/case/{caseId}` | Get a case's prescriptions | Doctor, Patient *(own case only)* |
| POST | `/medications/{medicationId}/confirm` | Confirm a dose was taken | Patient |

### Notes (`/Notes`)
| Method | Endpoint | Description | Role |
|---|---|---|---|
| POST | `/` | Add a clinical note | Doctor |
| GET | `/case/{caseId}` | Get a case's notes | Doctor, Patient *(own case only)* |

### Wound Images (`/WoundImages`)
| Method | Endpoint | Description | Role |
|---|---|---|---|
| POST | `/upload/{reportId}` | Upload a wound photo | Patient |
| GET | `/case/{caseId}` | Get a case's photo timeline | Doctor, Patient *(own case only)* |
| DELETE | `/{id}` | Remove a mistakenly uploaded photo | Patient |

### Alerts (`/Alerts`)
| Method | Endpoint | Description | Role |
|---|---|---|---|
| GET | `/` | List alerts | Doctor |
| PUT | `/{id}/handle` | Mark an alert as handled | Doctor |
| GET | `/count` | Get open-alert count | Doctor |

### Notifications (`/Notifications`)
| Method | Endpoint | Description | Auth |
|---|---|---|---|
| GET | `/` | List notifications | ✅ |
| PUT | `/{id}/read` | Mark one as read | ✅ |
| PUT | `/read-all` | Mark all as read | ✅ |
| GET | `/count` | Get unread count | ✅ |

### Doctor (`/Doctor`)
| Method | Endpoint | Description | Role |
|---|---|---|---|
| GET | `/Profile` | Get own doctor profile | Doctor |
| PUT | `/Profile` | Update own doctor profile | Doctor |

### Patient (`/Patient`)
| Method | Endpoint | Description | Role |
|---|---|---|---|
| GET | `/Profile` | Get own patient profile (includes assigned doctor) | Patient |
| PUT | `/Profile` | Update own patient profile | Patient |
| GET | `/risk-status` | Get current risk level, case ID, operation date | Patient |

### Admin (`/Admin`)
| Method | Endpoint | Description | Role |
|---|---|---|---|
| GET | `/Analytics` | Platform-wide statistics | Admin |
| GET | `/doctor-requests` | List doctor registration requests | Admin |
| PUT | `/doctor-requests/{id}/approve` | Approve a doctor request | Admin |
| PUT | `/doctor-requests/{id}/reject` | Reject a doctor request (with reason) | Admin |
| GET | `/doctors` | List all doctor accounts | Admin |
| PUT | `/doctors/{id}/toggle-status` | Activate/deactivate a doctor | Admin |
| DELETE | `/doctors/{id}` | Soft-delete a doctor account | Admin |
| GET | `/patients` | List all patients (with assigned doctor) | Admin |
| GET | `/audit-logs` | View the account-action audit trail | Admin |

Full interactive documentation is available via Swagger at `/swagger` once the API is running.

---

## 🔐 Security

- Access tokens expire after **15 minutes**; refresh tokens rotate on every use, and the frontend guards against refresh race conditions when multiple requests expire at once.
- `forgot-password` and `verify-otp` are rate-limited per IP address. Admin accounts cannot use self-service password reset.
- Every case-scoped endpoint verifies the requester is either the case's doctor or its patient before returning data.
- Passwords are hashed with BCrypt. Temporary passwords issued on account creation force a password change before the account can be used for anything else.
- The Hangfire dashboard is restricted to local requests only by default. Replace `LocalRequestsOnlyAuthorizationFilter` in `Program.cs` with a role-based filter before exposing it remotely.
- Database-level `CHECK` constraints back up API-level validation for numeric ranges (pain level, temperature, age, medication duration).

---

## 🧪 Post-Deployment Smoke Test

- [ ] Log in as Doctor, Patient, and Admin — confirm each lands on the correct dashboard, and that logging in on the wrong role tab is rejected
- [ ] Create a patient case → confirm the welcome email arrives and the temporary password forces a change on first login
- [ ] Submit a daily report with a wound photo → confirm the doctor sees it, including the photo, in real time
- [ ] Add a prescription → confirm the patient sees computed dose times and gets a reminder at the scheduled time
- [ ] Use "Forgot Password" → confirm the OTP email arrives, a wrong code is rejected, and the right code resets the password successfully
- [ ] Approve/reject a doctor request as Admin → confirm the correct email template sends and the action appears in the audit log

---

## 📄 License

Private / proprietary project. All rights reserved.
