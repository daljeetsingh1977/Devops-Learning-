# Citizen Services complaint app

An ASP.NET Core (Razor Pages, .NET 8) web app that lets citizens report local council issues –
potholes, garbage on the road, broken roads, missing street lights and similar civic problems.
Each report captures photographs and the citizen's geo location, is emailed to the council that
covers that location, and is given a service request number that the citizen can use to track
progress. The customer service team has its own portal to work on the requests.

```
Kpmg.Web.sln
├── src/Kpmg.Web          # web application
└── tests/Kpmg.Web.Tests  # xUnit tests
```

## Running the app

```bash
dotnet run --project src/Kpmg.Web
```

Then open the URL printed in the console (for example `https://localhost:7xxx`).

The app stores its data under `src/Kpmg.Web/App_Data` (created on first run):

| Folder                   | Contents                                                     |
|--------------------------|--------------------------------------------------------------|
| `App_Data/complaints.db` | SQLite database with complaints, photos, status and history   |
| `App_Data/photos`        | Uploaded photographs                                          |
| `App_Data/outbox`        | Outgoing emails written as `.eml` files (SMTP pickup folder)  |

## Citizen journey

1. **Report an issue** (`/`) – choose the issue type, describe it, attach up to five photographs
   (JPEG/PNG/WEBP/HEIC, 5 MB each) and share your location. The browser's geolocation API fills in
   the latitude/longitude automatically; the fields can also be typed in manually if the browser
   refuses to share a location.
2. **Confirmation** (`/Confirmation`) – shows the generated service request number
   (`CSR-yyyyMMdd-XXXXXX`) and the council the report was routed to.
3. **Track a request** (`/Track`) – enter a service request number to see its current status.

On submission the app:

* resolves the responsible council from the geo location,
* emails the council with the report details, a map link and the photographs attached, and
* emails the citizen an acknowledgement containing the service request number.

If the mail transport fails the complaint is still stored (and flagged as *email pending* in the
staff portal) so nothing is lost.

## Customer service portal

`/Staff` lists every request with filtering by status; `/Staff/Details/{id}` shows the full report,
the reporter's contact details, a map link, the photographs, and lets staff change the status
(Submitted → Acknowledged → In progress → Resolved/Rejected) and record case notes.

Staff sign in at `/StaffLogin` with a shared team access code. Configure it with the
`StaffPortal__AccessCode` environment variable (or user secrets):

```bash
StaffPortal__AccessCode="a-strong-shared-code" dotnet run --project src/Kpmg.Web
```

If no access code is configured, a random one is generated at start-up and written to the
application log. Photographs are only served to signed-in staff.

## Geo-location routing configuration

Councils are configured in `appsettings.json` as latitude/longitude bounding boxes. The smallest
(most specific) box that contains the reported point wins; if no box matches, the fallback mailbox
is used.

```json
"CouncilRouting": {
  "FallbackCouncilName": "National Civic Support Desk",
  "FallbackCouncilEmail": "civic-support@example.gov",
  "Councils": [
    {
      "Name": "Westminster City Council",
      "Email": "street-faults@westminster.example.gov",
      "MinLatitude": 51.48, "MaxLatitude": 51.54,
      "MinLongitude": -0.19, "MaxLongitude": -0.11
    }
  ]
}
```

Other configurable sections are `PhotoStorage` (upload limits, allowed content types, storage
path) and `Email` (sender address and pickup directory).

> The sample writes emails to a pickup directory so it runs without SMTP credentials. For a real
> deployment, relay that folder with an SMTP server or replace `IEmailSender` with an SMTP/API
> based implementation.

## Tests

```bash
dotnet test
```

The tests cover the geo-location → council routing rules, service request number generation, and
the complaint submission/lifecycle behaviour (persistence, photo validation, email routing, status
updates).
