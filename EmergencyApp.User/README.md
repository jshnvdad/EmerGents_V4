# EmergencyApp.User (.NET 10 MAUI, Android)

Target framework: .NET 10 MAUI for Android. Use Visual Studio 2026 with the .NET MAUI workload (or the .NET 10 SDK plus the Android workload), pick an Android emulator, and run `EmergencyApp.User.csproj`.

## Architecture
MVVM (CommunityToolkit.Mvvm) · Shell navigation · sqlite-net-pcl (local cache, offline-first) · HttpClient + JWT bearer.
- `Services/DatabaseService` local SQLite (user, contacts, requests)
- `Services/ApiService` REST client; base URL stored in Preferences (default `http://10.0.2.2:5000/` = host PC from emulator)
- `Services/SyncService` pushes unsent SOS requests, pulls status updates
- SOS is always saved locally first, then sent; unsent requests retry on History refresh

## Server API contract (ASP.NET Core) the app expects (JSON, camelCase)
| Method | Route | Body | Returns |
|---|---|---|---|
| POST | `/api/auth/register` | `{fullName, phone, email, password}` | `{userId, fullName, phone, email, token}` |
| POST | `/api/auth/login` | `{email, password}` | same as above |
| POST | `/api/emergencies` (Bearer) | `{clientId, userId, type, description, latitude, longitude, createdAtUtc}` | `{serverId, clientId, status, responderName}` |
| GET | `/api/emergencies/mine` (Bearer) | – | `[{serverId, clientId, status, responderName}]` |

Status values: `Received`, `Dispatched`, `Arrived`, `Resolved`, `Cancelled` (other words are mapped in `RequestStatus.Normalize`). The server should treat `clientId` as an idempotency key so retries don't create duplicates.

## Dev notes
- `usesCleartextTraffic="true"` in the manifest is for http dev only; use HTTPS in production.
- DTOs live in `Models/Models.cs`; mirror them in your shared/server project.

## Live tracking endpoints (new)
| Method | Route | Body | Returns |
|---|---|---|---|
| POST | `/api/emergencies/{serverId}/track` (Bearer) | `{latitude, longitude, accuracyMeters}` (user's live position, every ~5 s) | `LiveEmergencyDto` |
| POST | `/api/emergencies/{serverId}/cancel` (Bearer) | `{}` | `LiveEmergencyDto` (status `Cancelled`) |

`LiveEmergencyDto` (camelCase JSON, everything except serverId/clientId/status is optional):
`serverId, clientId, status, responderName, responderUnit, responderPhone, responderAddress, responderLatitude, responderLongitude, distanceKm, etaMinutes, receivedAtUtc, dispatchedAtUtc, arrivedAtUtc, resolvedAtUtc`.
`GET /api/emergencies/mine` may return the same object per item. If `/track` is missing the app falls back to `/mine` for status only.

## SOS payload additions
`POST /api/emergencies` now also carries `peopleCount` (1, 2, 3 or 4 = "4 or more") and `source` (`Hold`, `Tap`, `Shake`, `Volume button`). Add them to your server DTO / MySQL table; older servers simply ignore them.
Incident `type` values: `Medical`, `Fire`, `Police`, `Accident`, `Other`, `General` (nothing selected).
