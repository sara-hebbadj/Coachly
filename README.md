# Coachly

Coachly is a booking and membership management platform for coaches.

Status: In active development.

## Auth flow status

The app now supports:

- Register/login with email + password via `api/auth/register` and `api/auth/login`.
- Session restore after app restart (token in secure storage + `api/auth/session` validation).
- Google OAuth login via API callback relay to MAUI custom URI.
- Apple OAuth login via API callback relay to MAUI custom URI.

User records are persisted to the API database (`Users` table through EF Core), so registrations and external logins are saved for developer visibility in SQL tooling.

## Google OAuth setup (API + MAUI app)

### 1) Create Google OAuth credentials

In Google Cloud Console:

- Create an OAuth 2.0 Client ID (Web application).
- Add an authorized redirect URI that matches your API callback endpoint:
  - `http://localhost:5114/api/auth/external/google/callback` (local dev)

### 2) Configure the API

Set these values in `Coachly.Api/appsettings.Development.json` (or user secrets / environment variables):

- `ExternalAuth:Google:ClientId`
- `ExternalAuth:Google:ClientSecret`
- `ExternalAuth:Google:CallbackPath` (default: `/api/auth/external/google/callback`)

## Apple OAuth setup (API + MAUI app)

Set these values in `Coachly.Api/appsettings.Development.json` (or user secrets / environment variables):

- `ExternalAuth:Apple:ClientId` (Service ID)
- `ExternalAuth:Apple:TeamId`
- `ExternalAuth:Apple:KeyId`
- `ExternalAuth:Apple:PrivateKey` (PEM contents)
- `ExternalAuth:Apple:CallbackPath` (default: `/api/auth/external/apple/callback`)

Also register callback URL in Apple Developer settings:

- `http://localhost:5114/api/auth/external/apple/callback`

### 3) Configure the MAUI app

Optional environment variables (defaults shown):

- `COACHLY_OAUTH_START_URL` (default: `http://localhost:5114/` on desktop, `http://10.0.2.2:5114/` on Android emulator)
- `COACHLY_MOBILE_AUTH_CALLBACK_URI` (default: `coachly://auth-callback`)

The app is preconfigured to handle `coachly://auth-callback` on Android/iOS.
