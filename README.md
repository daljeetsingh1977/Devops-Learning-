# Devops-Learning-

A small ASP.NET Core web application for the KPMG organization, complete with
automated tests and a Docker image that runs the app on port **8087**.

## Layout

| Path                     | Description                                     |
| ------------------------ | ----------------------------------------------- |
| `src/Kpmg.Web`           | ASP.NET Core web app with a KPMG landing page   |
| `tests/Kpmg.Web.Tests`   | xUnit integration tests for the web app         |
| `Dockerfile`             | Multi-stage build/run image (port 8087)         |

## Endpoints

| Route        | Description                                   |
| ------------ | --------------------------------------------- |
| `/`          | KPMG-branded landing page                     |
| `/health`    | JSON health status                            |
| `/api/about` | JSON metadata about the running application   |

## Build and test

```bash
dotnet build Kpmg.Web.sln
dotnet test Kpmg.Web.sln
```

## Run locally

```bash
dotnet run --project src/Kpmg.Web
```

The application listens on <http://localhost:8087>.

## Run with Docker

```bash
docker build -t kpmg-web .
docker run --rm -p 8087:8087 kpmg-web
```

Then browse to <http://localhost:8087>.

The listening address is configured in `src/Kpmg.Web/appsettings.json`; it can be
overridden at runtime, for example with
`-e Kestrel__Endpoints__Http__Url=http://0.0.0.0:9000`.
