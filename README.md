# 🌤 Weather Microservice — NEA Singapore

A production-grade .NET 8 Weather Microservice powered entirely by **NEA Singapore via data.gov.sg**.
Every field in every response maps directly to a real NEA sensor or API endpoint.
Database using SQLite with Entity Framework Core

---

## Quick Start

```bash
# 1. Unzip and enter the project
cd WeatherService-main

# 2. Run
cd WeatherService/WeatherService.API
dotnet run

# 3. Open Swagger UI at http://localhost:5181


## Data Sources — All from NEA Singapore
--------------------------------------------------------------------------------------------------------
|                  What                |    NEA Endpoint      |               Notes                    |
|--------------------------------------|----------------------|----------------------------------------|
| Temperature (°C)                     | `/air-temperature`   | Average of all NEA stations            |
| Humidity (%)                         | `/relative-humidity` | Average of all NEA stations            |
| Rainfall (mm)                        | `/rainfall`          | Average of all NEA stations            |
| Wind speed (m/s)                     | `/wind-speed`        | Converted from km/h                    |
| PM2.5 (µg/m³)                        | `/pm25`              | National 1-hour reading                |
| PM10, CO, SO₂, NO₂, O₃, PSI          | `/psi`               | National 24-hour breakdown             |
| UV Index                             | `/uv-index`          | Available 7am–7pm SGT                  |
| 4-Day Forecast                       | `/four-day-outlook`  | Temp range, humidity range, wind, text |
--------------------------------------------------------------------------------------------------------

## API Reference

### Weather
------------------------------------------------------------------------------------------------------------------
| Method |                         Endpoint                                                |    Description      |
|--------|---------------------------------------------------------------------------------|---------------------|
| `GET`  | `/api/v1/weather/current?location=Singapore`                                    | Live NEA conditions |
| `GET`  | `/api/v1/weather/forecast?location=Singapore&days=4`                            | NEA 4-day outlook   |
| `GET`  | `/api/v1/weather/historical?location=Singapore&from="2026-03-01"&to="2026-03-04"| Records + stats     |
| `GET`  | `/api/v1/weather/export/csv?location=Singaporefrom="2026-03-01"&to="2026-03-04"`| CSV download        |
| `POST` | `/api/v1/weather/refresh?location=Singapore`                                    | Force data refresh  |
------------------------------------------------------------------------------------------------------------------

### Alerts

Subscribe to threshold-based notifications on real NEA sensor data:
--------------------------------------------------------------------------------------
| Method  |               Endpoint              |           Description              |
|-------- |-------------------------------------|------------------------------------|
| `POST`  | `/api/v1/alerts/CreateAlert`        | Subscribe alert base on thereshold |
| `GET`   | `/api/v1/alerts/GetAlertById`       | Retrieve alert by user Id          |
| `GET`   | `/api/v1/alerts/GetAlertByEmail`    | Retrieve alert by Email            |
| `PATCH` | `/api/v1/alerts/UpdateAlertContent` | Update alert contents              |
| `DELETE`| `/api/v1/alert/DeleteAlert`         | Unsubscribe alert                  |
| `POST`  | `/api/v1/alert/ProcessAlert`        | Manual send alert                  |
--------------------------------------------------------------------------------------

### Locations
-----------------------------------------------------------------------------------------------------------------------
| Method |                          Endpoint                        |                 Description                     |
|--------|----------------------------------------------------------|-------------------------------------------------|
| `GET`  | `/api/v1/locations`                                      | All tracked locations                           |
| `GET`  | `/api/v1/locations/{location}/summary`                   | Dashboard: temp, humidity, PSI, UV, alert count |
| `GET`  | `/api/v1/locations/compare?locations=Singapore,Tampines` | Side-by-side (max 5)                            |
-----------------------------------------------------------------------------------------------------------------------

### System
-------------------------------------------------
| Endpoint      | Description                   |
|---------------|-------------------------------|
| `GET /health` | EF Core DB health check       |
| `GET /`       | Swagger UI (Development only) |
-------------------------------------------------

#Backend Microservice

AlertProcessingBackgroundService - Sending alert to all subscriber when fulfill the threshold-based (Every 15 mintues)
WeatherRefreshBackgroundService - Trigger interval refresh (Every 30 minutes)


**Condition types** (all backed by NEA sensors):
------------------------------------------------
|      Type        | Unit |      NEA Source    |
|------------------|------|--------------------|
| 1 - `Temperature`| °C   | /air-temperature   |
| 2 -`Humidity`    | %    | /relative-humidity |
| 3 -`RainfallMm`  | mm   | /rainfall          |
| 4 -`WindSpeedMs` | m/s  | /wind-speed        |
| 5 -`Pm25`        | µg/m³| /pm25              |
| 6 - `Psi`        | —    | /psi               |
| 7- `UvIndex`     | —    | /uv-index          |
------------------------------------------------

**Threshold Operators:** (For Subscribe Alert)
1 - GreaterThan
2 - LessThan
3 - GreaterThanOrEqual
4 - LessThanOrEqual 


## Docker

```bash
# With Docker Compose (recommended)
docker-compose up --build

# Plain Docker
docker build -t weather-service .
docker run -p 8080:8080 weather-service
