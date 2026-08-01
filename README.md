# Family Budget — Mobile

A .NET MAUI (Android) client for tracking family expenses, budgets, and shared wallets — built to replace a shared Google Sheet with a proper day-to-day app for two family members.

This app is a client for a companion REST API (a separate Cloudflare Workers project, not included in this repository) and doesn't work standalone — see [Configuration](#configuration) below.

## Features

- **Auth** — email/password login, session persisted on-device
- **Wallets** — balances across multiple wallets (cash, bank accounts, etc.), combined family total
- **Transactions** — income, expense, and transfer entries, with filtering by period/wallet/type
- **Categories** — one level of subcategories, plus an automatic "catch-all" category tracking uncommitted balance
- **Budgets** — planned vs. actual vs. remaining per category per period
- **Periods & period close ("tutup buku")** — a guided wallet-reconciliation wizard that locks a period, records any counted-vs-system balance adjustments, and opens the next period with budgets carried forward
- **Family members** — add a new family member, or reset another member's password

## Tech stack

- **.NET MAUI**, targeting `net10.0-android` only (Android-only by design — see `FamilyBudget.Mobile/REQUIREMENTS.md`)
- **MVVM** via [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- **Shell navigation** (`AppShell.xaml`) with a splash screen, login flow, and a bottom-tab main area
- Typed `HttpClient` (`System.Net.Http.Json`) talking to the backend API, with a `DelegatingHandler` attaching the auth token to every request
- [CommunityToolkit.Maui](https://learn.microsoft.com/dotnet/communitytoolkit/maui/) for snackbars and other UI extras
- Material You–inspired visual design, hand-built (no design system package)
- No local database — the app is online-only and calls the API directly for every screen

## Project structure

```
FamilyBudget.Mobile/
  MauiProgram.cs        # DI registration (services, pages, viewmodels)
  AppShell.xaml(.cs)     # Shell routes, startup routing, session-expiry handling
  Common/                # API base URL config, shared JSON options
  Services/
    Api/                  # Typed API client, request/response DTOs, error handling
    Auth/                 # Token storage, session state
    Feedback/             # Error dialogs / snackbars
  ViewModels/             # One per page (CommunityToolkit.Mvvm observable objects)
  Views/                  # One XAML page per ViewModel
  Controls/               # Reusable custom controls (e.g. bottom navigation bar)
  Converters/             # XAML value converters (currency formatting, etc.)
  Resources/              # Styles, fonts, app icon, splash screen assets
```

## Getting started

### Prerequisites

- .NET 10 SDK with the `android` workload installed (`dotnet workload install android`)
- An Android emulator or physical device for testing

### Build

```
dotnet build
```

### Run on an emulator/device

```
dotnet build -f net10.0-android -t:Run
```

This deploys to whatever device `adb` currently targets.

### Build a Release APK

```
dotnet publish -f net10.0-android -c Release
```

Produces a signed, installable APK under `bin/Release/net10.0-android/publish/`. No release keystore is configured by default, so the build falls back to the local machine's debug keystore — that's fine for sideloading onto your own device, but **not** suitable for Play Store distribution. Set up proper release signing (`AndroidSigningKeyStore` and related MSBuild properties) before distributing this any other way.

## Configuration

The API base URL is set in `FamilyBudget.Mobile/Common/Constants.cs`:

```csharp
public static class ApiConfig
{
#if DEBUG
    public const string BaseUrl = "http://10.0.2.2:8787"; // Android emulator alias for localhost
#else
    public const string BaseUrl = "https://your-deployed-api.example.com";
#endif
}
```

- **Debug** builds point at `10.0.2.2:8787`, the Android emulator's alias for the host machine — matching the companion API's local dev server (`wrangler dev`) default port.
- **Release** builds need to be pointed at your own deployed instance of the companion API before publishing.

## Testing

There's no automated test suite for this app (a deliberate simplicity choice for a two-user app) — changes are verified manually against a running instance of the API, on an emulator or physical device.
