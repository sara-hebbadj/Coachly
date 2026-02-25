# Coachly

Coachly is a booking and membership management platform for coaches.

Status: In active development.

## Google OAuth setup (API + MAUI app)

To allow users to register/login with Google, configure both the API and the mobile app callback.

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

### 3) Configure the MAUI app

Optional environment variables (defaults shown):

- `COACHLY_OAUTH_START_URL` (default: `http://localhost:5114/` on desktop, `http://10.0.2.2:5114/` on Android emulator)
- `COACHLY_MOBILE_AUTH_CALLBACK_URI` (default: `coachly://auth-callback`)

The app is preconfigured to handle `coachly://auth-callback` on Android/iOS.
