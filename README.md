# fast_pivot

Fast Pivot is a C# + Angular prototype for displaying planned test schedules as a weekly pivot table.

## Data shape

Schedules are expected to use JSON items like:

```json
{
  "Project": "Project_1",
  "Title": "Project_1_Regular",
  "crontab": "0 30 17 ? * *",
  "Description": "Bug_987565",
  "Product": "Product_2026",
  "Version": "1.0.0.1",
  "Machine": "Host-160",
  "Image": "160_Win10_22H2",
  "TestSuite": "Stability",
  "Test": "Stability_Win10_160"
}
```

The backend reads `backend/FastPivot.Api/Data/generated_300_json_items.json` by default. To point it at another generated file, set:

```bash
Schedules__JsonPath=/path/to/generated_300_json_items.json
```

## Backend

The ASP.NET Core API exposes:

- `GET /api/schedules` - raw schedule data.
- `GET /api/pivot/current-week?timezone=UTC` - current week pivot.
- `GET /api/pivot?weekStart=2026-07-06&timezone=UTC` - selected week pivot.

All `/api` endpoints are protected by a Windows/Negotiate authorization policy. The default configuration allows everyone:

```json
"WindowsAuthorization": {
  "AllowedGroups": [
    "Everyone"
  ]
}
```

To restrict access, replace `Everyone` with Windows domain or local group names:

```json
"WindowsAuthorization": {
  "AllowedGroups": [
    "DOMAIN\\QA Engineers",
    "DOMAIN\\Release Managers"
  ]
}
```

The special values `Everyone` and `*` allow unrestricted API access. Any other value requires an authenticated Windows user that belongs to one of the configured groups.

Pivot rows are grouped by:

- Project
- Product
- Version

Pivot columns are grouped by:

- Day of week
- TestSuite

Cell keys use:

```text
yyyy-MM-dd|TestSuite
```

Each cell contains the matching scheduled test items with title, description, machine, image, test, suite, and scheduled time.

Run:

```bash
cd backend/FastPivot.Api
dotnet run --urls http://localhost:5000
```

## Frontend

The Angular app renders the pivot table with:

- Dynamic day and TestSuite columns.
- Project/Product/Version row grouping.
- Project/Product/Version/TestSuite filters.
- Previous/current/next week loading.
- Multiple test items per pivot cell.

Run:

```bash
cd frontend
npm install
npm start
```

## Deployment

### Prerequisites

- .NET 8 SDK/runtime for the backend.
- Node.js compatible with Angular 20, plus npm, for building the frontend.
- A hosting target for the ASP.NET Core API, such as Linux VM, container host, Azure App Service, IIS, or another .NET-capable platform.
- A static file host for the Angular app, such as Nginx, Azure Static Web Apps, S3/CloudFront, or the same host that serves the API.

### Build the frontend

```bash
cd frontend
npm ci
npm run build
```

The production bundle is generated in:

```text
frontend/dist/fast-pivot-frontend
```

Deploy this folder to the static web host.

### Publish the backend

```bash
cd backend/FastPivot.Api
dotnet restore
dotnet publish -c Release -o ./publish
```

Deploy the contents of `backend/FastPivot.Api/publish` to the API host and start the application with:

```bash
dotnet FastPivot.Api.dll
```

### Configure schedule data

The default deployed data file is:

```text
Data/generated_300_json_items.json
```

To use another file, set the environment variable before starting the API:

```bash
Schedules__JsonPath=/absolute/path/to/generated_300_json_items.json
```

### Connect frontend to backend

For local development, Angular uses `frontend/proxy.conf.json` to forward `/api` requests to `http://localhost:5000`.

For deployment, configure the static host or reverse proxy so browser requests to `/api/*` are forwarded to the ASP.NET Core API. Example Nginx location:

```nginx
location /api/ {
    proxy_pass http://127.0.0.1:5000/api/;
    proxy_set_header Host $host;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
}
```

If the frontend and API are hosted on different domains, update the backend CORS configuration in `Program.cs` to allow the deployed frontend origin.

### Configure Windows authorization

By default, API access is open because `WindowsAuthorization:AllowedGroups` contains `Everyone`.

To restrict access in deployment, configure allowed Windows groups with environment variables:

```bash
WindowsAuthorization__AllowedGroups__0='DOMAIN\QA Engineers'
WindowsAuthorization__AllowedGroups__1='DOMAIN\Release Managers'
```

When hosted behind IIS, enable Windows Authentication for the application. When hosted directly with Kestrel, the API uses ASP.NET Core Negotiate authentication; Linux hosts require Kerberos/domain configuration for real Windows group resolution.
