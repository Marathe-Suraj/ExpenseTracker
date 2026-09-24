# Deploy API to MonsterASP

The Android app calls the REST API on your hosted site:

**Base URL:** `https://trackexpense.runasp.net`

## What changed

- JWT authentication for `/api/*` endpoints
- Cookie authentication for the existing web UI (unchanged)
- New config section `Jwt` in `appsettings.json`

## Before you publish

1. Set a strong JWT secret on MonsterASP (do not use the sample key in source control for production).

   In MonsterASP → your site → **Configuration / App Settings**, add:

   | Key | Example value |
   |-----|----------------|
   | `Jwt:Key` | long random string, at least 32 characters |
   | `Jwt:Issuer` | `ExpenseTracker` |
   | `Jwt:Audience` | `ExpenseTracker.Maui` |

2. Publish this web project the same way you normally deploy to MonsterASP (Visual Studio Publish, or `dotnet publish` + upload).

## Smoke test after deploy

### Login

```http
POST https://trackexpense.runasp.net/api/auth/login
Content-Type: application/json

{
  "username": "YOUR_WEB_USERNAME",
  "password": "YOUR_WEB_PASSWORD"
}
```

Expected: `200` with `{ "token", "userId", "username" }`.

### Authenticated call

```http
GET https://trackexpense.runasp.net/api/dashboard
Authorization: Bearer YOUR_TOKEN
```

Expected: `200` with dashboard totals JSON.

### Web UI check

Open `https://trackexpense.runasp.net/` and confirm login with cookies still works.

## MAUI APK

After smoke tests pass, the Android APK (built separately in `ExpenseTracker.Maui`) will use this base URL automatically.
