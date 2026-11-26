# PUSH API

> **Work in progress.** PUSH is a capstone project at Techtorium NZIIT and is still being built and tested. Endpoints, data and setup steps may change.

The backend for **PUSH**, a .NET MAUI fitness app for Android. The app never connects to the database directly. Every screen gets its data through this API.

**App repo:** [CapstoneProjectPUSH](https://github.com/JamesB62/CapstoneProjectPUSH)

## Tech stack

- ASP.NET Core Web API (.NET 10)
- Entity Framework Core with Microsoft SQL Server
- JWT bearer authentication with roles
- PBKDF2 password hashing
- MailKit (Gmail SMTP) for welcome and password reset emails
- Hosted on MonsterASP (API and SQL Server database)

## How it works

```
PUSH app (.NET MAUI, Android)
        │  HTTPS + JWT login token
        ▼
PUSH API (this repo)  ──►  Microsoft SQL Server
```

- **Login tokens:** register, login and guest login return a JWT (valid for 7 days). The app sends it with every request. The user always comes from the token, so nobody can ask for someone else's data.
- **Roles:** every token carries one role: **Admin**, **Member** or **Guest**. Endpoints are locked to the roles allowed to use them.
- **Deactivated accounts:** every request checks the account still exists and is active, so deactivating a user takes effect straight away.
- **Validation:** the API checks every rule, including the ones the app also checks, because requests can be sent without the app.
- **Dates** are stored in UTC. "Today" (for weight logs) is worked out in New Zealand time.
- **Photos** are saved in an `uploads` folder on the server and served from `/uploads/...`.
- **Deleting** a gym, category, trainer or exercise never fails because something uses it. Anything that pointed at it shows "Unknown" or "Uncategorised", and deleted exercises are removed from workouts.

## Endpoints

All routes start with `/api`.

| Area | Endpoints | Who |
|---|---|---|
| **Auth** | `POST auth/register`, `auth/login`, `auth/guest` | Anyone |
| **Password reset** | `POST passwordreset/request`, `passwordreset/reset` | Anyone |
| **Users** | `GET/PUT/DELETE users/me` | Logged in (delete: members only) |
| | `GET users`, `users/count`, `users/active-count`, `PUT users/{id}/status` | Admin |
| **Categories, gyms** | `GET` list, count, by id | Anyone |
| | `POST`, `PUT`, `DELETE` | Admin |
| **Exercises** | `GET` list, count, by id | Anyone |
| | `POST`, `PUT`, `DELETE` | Admin |
| **Trainers** | `GET` list, `favourite-first`, counts, by id | Members, admins |
| | `POST`, `PUT`, `DELETE` | Admin |
| **Articles** | `GET` list, count, by id | Anyone |
| **Workouts** | `GET workouts/mine`, `{id}/exercises`, favourites | Logged in |
| | `GET workouts/community`, `POST {id}/favourite`, `POST`, `PUT`, `DELETE` | Members, admins |
| **Notes** | `GET notes/mine`, `notes/workout/{id}`, by id | Logged in |
| | `POST`, `PUT`, `DELETE` | Members, admins |
| **Workout / weight logs** | `GET workoutlogs/mine`, `weightlogs/mine` | Logged in |
| | `POST workoutlogs`, `POST weightlogs/today` | Members, admins |
| **Reports** | `POST reports` | Members, admins |
| | `GET reports`, `pending-count`, by id, `POST {id}/dismiss`, `{id}/remove-content` | Admin |
| **Images** | `POST/DELETE images/profile` | Members, admins |
| | `POST/DELETE images/trainers/{id}`, `images/exercises/{id}` | Admin |

## Running it locally

1. Open the project in Visual Studio (with the ASP.NET and web development workload).
2. Right-click the project → **Manage User Secrets** and add your own values:

   ```json
   {
     "ConnectionStrings": {
       "PushDb": "Server=(localdb)\\MSSQLLocalDB;Database=PushDb;Trusted_Connection=True;TrustServerCertificate=True;"
     },
     "Jwt": {
       "Key": "any long random string, at least 32 characters"
     },
     "SeedAdmin": {
       "Password": "the password for the seeded admin account"
     },
     "Email": {
       "SenderName": "PUSH",
       "SenderEmail": "your Gmail address",
       "AppPassword": "a Gmail app password"
     }
   }
   ```

   The connection string above uses **LocalDB**, which comes with Visual Studio. `Email` is optional: without it, emails are written to the output window instead of being sent. **Never commit real values.**

3. Create the database in the **Package Manager Console**:

   ```
   Update-Database
   ```

4. Run the API with the **http** launch profile. Empty tables are filled with seed data on startup (categories, gyms, trainers, exercises, articles, an admin account `admin@push.app`, and a guest account with sample workouts).

### Connecting the app to a local API

- On the Android emulator, the PC is `10.0.2.2`, so set the app's `BaseUrl` to `http://10.0.2.2:PORT/` (port from `Properties/launchSettings.json`).
- Android blocks plain http, so the app needs a `network_security_config.xml` allowing cleartext traffic to `10.0.2.2`.

## Known limitations

- No rate limiting on login, register or password reset yet
- Photos are stored on the server's disk rather than in cloud storage
- Still being tested – expect bugs
