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

The backend reads `backend/FastPivot.Api/Data/sample-schedules.json` by default. To point it at a larger generated file, set:

```bash
Schedules__JsonPath=/path/to/generated_300_json_items.json
```

## Backend

The ASP.NET Core API exposes:

- `GET /api/schedules` - raw schedule data.
- `GET /api/pivot/current-week?timezone=UTC` - current week pivot.
- `GET /api/pivot?weekStart=2026-07-06&timezone=UTC` - selected week pivot.

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
