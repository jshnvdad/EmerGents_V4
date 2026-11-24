# EmergencyDesk Server

This server connects the EmergencyDesk desktop application and EmergencyApp Android client. It uses ASP.NET Core 8 and MySQL.

## First run with XAMPP

1. Install the **.NET 8 SDK** and XAMPP on Windows.
2. In XAMPP Control Panel, start **MySQL**. Apache is not required.
3. Extract this folder and double-click `Run-Server.bat`.
4. On first startup, the server creates the `emergency_db` database and its tables in MySQL. When it finds an older server schema, it adds the new columns automatically.
5. Open [http://localhost:5000/swagger](http://localhost:5000/swagger) to see the API.

The included local defaults expect MySQL at `localhost:3306` with user `root` and no password, as in a typical XAMPP installation. If your MySQL root account has a password or uses a different port, edit `ConnectionStrings:Default` in `appsettings.json` before running.

The starter desktop login for a newly created database is **admin / EmergencyDesk123!**. It is created only when the Staff table is empty. If you use a database that already has staff accounts, keep using its existing account. The JWT key and starter login in this package are for local development; change them before exposing the server to a network.

## App addresses

- Desktop app on the same computer: `http://localhost:5000/`
- Android emulator: `http://10.0.2.2:5000/`

The Android app's API base URL must point to the address above. A physical Android phone needs the computer's local network IP, plus firewall access to port 5000.

## Supported routes

Android:

- `POST /api/auth/register`, `POST /api/auth/login`
- `POST /api/emergencies` with `clientId`, `type`, `description`, optional coordinates, `createdAtUtc`, `peopleCount`, and `source`. The authenticated token determines the account; body `userId` is ignored.
- `GET /api/emergencies/mine`
- `POST /api/emergencies/{serverId}/track` with live latitude, longitude, and optional accuracy.
- `POST /api/emergencies/{serverId}/cancel`

Desktop:

- `POST /api/staff/login`
- `GET /api/incidents`, `PUT /api/incidents/{id}/assign`, `PUT /api/incidents/{id}/status`, and `PUT /api/incidents/{id}/acknowledge`
- `GET/POST/PUT /api/responders` and `GET /api/users`, `PUT /api/users/{id}/active`

Desktop statuses `EnRoute` and `OnScene` map to mobile `Dispatched` and `Arrived`. The server stores the latest reported user location per incident, not a location trail.

## Client compatibility notes

The desktop archive's `IncidentType` enum does not include `Accident`, `Other`, or `General`, while Android can submit those values. That desktop client may reject incidents with those types until its enum is expanded. The server retains the values so reports are not mislabeled. The desktop model and UI also need an update to show `peopleCount`.

The Android archive omits its `ApiService`, `DatabaseService`, and `LiveTrackingService` source, so the new routes match the documented API contract but could not be checked against those omitted implementations.
