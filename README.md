# Family Budget — Mobile

A private .NET MAUI Android client for tracking family expenses, budgets, shared wallets, and real savings accounts — built to replace a shared Google Sheet with a proper day-to-day app for two family members. It is intended for personal sideloading and is not published to Google Play.

This app is a client for a companion REST API (a separate Cloudflare Workers project, not included in this repository) and doesn't work standalone — see [Configuration](#configuration) below.

## Features

- **Auth** — email/password login, session persisted on-device
- **Wallets** — balances across multiple wallets (cash, bank accounts, etc.), combined family total
- **Transactions** — income, expense, and transfer entries, with filtering by period/wallet/type; open-period entries can be edited or deleted (type is locked once created)
- **Savings** — separately-held real money such as Bank Jago pockets, with an opening balance, derived current balance, automatic deposits from mapped budget categories, and direct saving expenses
- **Categories** — one level of subcategories, editable saving mappings, plus an automatic "catch-all" category tracking uncommitted balance
- **Budgets** — planned vs. actual vs. remaining per category per period, with a visible saving badge for mapped categories
- **Periods & period close ("tutup buku")** — a guided wallet-reconciliation wizard that locks a period, records any counted-vs-system balance adjustments, and opens the next period with budgets carried forward
- **Family members** — add a new family member, or reset another member's password
- **Full Bahasa Indonesia UI** — all screens and system dates (`id-ID` culture, set globally at startup)

## Tech stack

- **.NET MAUI**, targeting `net10.0-android` only (Android-only by design)
- **MVVM** via [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- **Shell navigation** (`AppShell.xaml`) with a splash screen, login flow, and a flyout (side drawer) main area — seven flat top-level destinations (Wallets, Transactions, Savings, Budgets, Categories, Periods, Family members)
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
  Converters/             # XAML value converters (currency formatting, icons, etc.)
  Resources/              # Styles, fonts, app icon, splash screen assets
```

Savings-related code follows the same feature split:

```text
Services/Api/
  ApiClient.Savings.cs
  Dtos/SavingDtos.cs
ViewModels/
  SavingsViewModel.cs
  SavingDetailViewModel.cs
  SavingFormViewModel.cs
  SavingExpenseFormViewModel.cs
Views/
  SavingsPage.xaml
  SavingDetailPage.xaml
  SavingFormPage.xaml
  SavingExpenseFormPage.xaml
```

## Savings workflow

A saving represents real money held outside the main spending wallets. For example, moving money from a primary bank account into a dedicated Bank Jago pocket is recorded as:

1. A normal expense from the primary wallet, assigned to a category mapped to the saving.
2. An automatic deposit into the saving for the same amount and date.

The expense still counts toward the category's budget realization. The linked saving deposit is read-only in the app and follows edits or deletion of its source transaction automatically. A direct saving expense reduces only the saving balance; it does not change a wallet or budget.

The app supports:

- Creating and editing a saving, including correcting its opening balance.
- Viewing the derived current balance and newest-first transaction history.
- Creating, editing, and deleting direct saving expenses.
- Mapping existing leaf categories to a saving from the category edit screen.
- Editing a primary transaction's category and deleting primary transactions.
- Displaying localized API errors when a change would make a saving balance negative.

Only categories without children can map to a saving. Mapping changes are not retroactive; old deposits move only when their own source transactions are edited.

Saving cards use text initials instead of asset icons:

- One word: the first two letters (`Liburan` → `LI`).
- Multiple words: the first letters of the first two words (`Dana Darurat` → `DD`).
- Empty-name fallback: `TB`.

## Getting started

### Prerequisites

- .NET 10 SDK with the `android` workload installed (`dotnet workload install android`)
- An Android emulator or physical device for testing

### Build

```bash
dotnet build FamilyBudget.Mobile.slnx
```

### Run on an emulator/device

```bash
dotnet build FamilyBudget.Mobile/FamilyBudget.Mobile.csproj -f net10.0-android -t:Run
```

This deploys to whatever device `adb` currently targets.

### Build a Release APK

```bash
dotnet publish FamilyBudget.Mobile/FamilyBudget.Mobile.csproj -f net10.0-android -c Release
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
    public const string BaseUrl = "https://family-budget-api.alannursalim.my.id";
#endif
}
```

- **Debug** builds point at `10.0.2.2:8787`, the Android emulator's alias for the host machine — matching the companion API's local dev server (`wrangler dev`) default port.
- **Release** builds point to the privately deployed Cloudflare Worker at `family-budget-api.alannursalim.my.id`.

### Backend compatibility

The savings screens require the companion API version that includes D1 migration `0004_add_savings.sql`. Apply the remote migration before installing a mobile build that uses these screens, then deploy the matching Worker code. Without it, savings and updated category requests will fail.

## Testing

There's no automated test suite for this app (a deliberate simplicity choice for a two-user app). Changes are verified by compiling both C# and source-generated XAML, then manually exercising them against a running API on an emulator or physical device.

For a faster compile-only verification that skips APK packaging:

```bash
dotnet msbuild FamilyBudget.Mobile/FamilyBudget.Mobile.csproj /t:Compile /p:TargetFramework=net10.0-android /p:Configuration=Debug /restore:false
```
