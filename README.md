![Frame 39260](https://github.com/user-attachments/assets/5c0a5f4d-85cb-449c-9c09-76edaa5620fd)

# Gml.Web.Api

Gml.Web.Api is a RESTful web service within the Gml microservices ecosystem, providing APIs for server-side operations, managing game accounts, and profiles for Minecraft (supporting Forge, NeoForge, Fabric, and LiteLoader). This README explains how to set up and run the service, which uses SQLite as its database and relies on environment variables for configuration.

## Prerequisites

Ensure the following are installed:

- **.NET 8.0 SDK**: Install from [Microsoft's official website](https://dotnet.microsoft.com/download/dotnet/8.0).
- **Git**: Install from [Git website](https://git-scm.com/) or via your package manager.
- **Docker** (optional, for containerized deployment): Install Docker Desktop from [Docker's official website](https://www.docker.com/products/docker-desktop/).
- A code editor like Visual Studio or Visual Studio Code (optional, for development).

## Installation and Setup

Follow these steps to clone, configure, and run the Gml.Web.Api project.

### 1. Clone the Repository

Clone the repository using Git:

```bash
git clone https://github.com/Gml-Launcher/Gml.Web.Api.git
cd Gml.Web.Api
```

### 2. Restore Dependencies

Restore .NET project dependencies:

```bash
dotnet restore
```

### 3. Configure Environment Variables

The service is configured using environment variables, which control the SQLite database, security settings, project details, and service endpoints. Set these variables before running the application. You can set them in your shell, a `.env` file (if supported by your setup), or pass them to Docker.

Required environment variables:

- `ASPNETCORE_ENVIRONMENT`: Environment mode (e.g., `Development` for local testing, `Production` for deployment).
- `SECURITY_KEY`: A secure key for authentication/encryption (e.g., `jkuhbsfgvuk4gfikhn8i7wa34rkbqw23`). Use a strong, unique key.
- `PROJECT_NAME`: Name of the project (e.g., `GmlServer`).
- `PROJECT_DESCRIPTION`: Description of the project (e.g., `GmlServer Description`).
- `PROJECT_POLICYNAME`: Policy name for authorization (e.g., `GmlPolicy`).
- `PROJECT_PATH`: Base path for the project (leave empty `""` unless specified otherwise).
- `SERVICE_TEXTURE_ENDPOINT`: URL for the texture service endpoint (e.g., `http://localhost:5086`).
- `AllowedHosts`: Allowed hosts for the API (e.g., `*` to allow all).

Example (Linux/macOS):

```bash
ASPNETCORE_ENVIRONMENT="Development"
SECURITY_KEY="jkuhbsfgvuk4gfikhn8i7wa34rkbqw23"
PROJECT_NAME="GmlServer"
PROJECT_DESCRIPTION="GmlServer Description"
PROJECT_POLICYNAME="GmlPolicy"
PROJECT_PATH=""
SERVICE_TEXTURE_ENDPOINT="http://localhost:5086"
AllowedHosts="*"
```

On Windows, use `set` in Command Prompt:

```cmd
set ASPNETCORE_ENVIRONMENT=Development
set SECURITY_KEY=jkuhbsfgvuk4gfikhn8i7wa34rkbqw23
set PROJECT_NAME=GmlServer
set PROJECT_DESCRIPTION=GmlServer Description
set PROJECT_POLICYNAME=GmlPolicy
set PROJECT_PATH=
set SERVICE_TEXTURE_ENDPOINT=http://localhost:5086
set AllowedHosts=*
```

Note: The SQLite database path is managed internally by the application. Ensure the directory where the database file will be created is writable.

### 4. Set Up the SQLite Database

The SQLite database is created automatically on first run if the database file does not exist. To apply database migrations:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Install Entity Framework Core CLI tools if needed:

```bash
dotnet tool install --global dotnet-ef
```

### 5. Build the Project

Build the project to check for errors:

```bash
dotnet build
```

### 6. Run the Application

Run the service locally:

```bash
dotnet run
```

The API will be available at `https://localhost:5001` or `http://localhost:5000` (check terminal output for the port). Test the API with tools like Postman or curl:

```bash
curl http://localhost:5000/api/profiles
```

### 7. (Optional) Run with Docker

To run the service in a Docker container:

1. Build the Docker image:

```bash
docker build -t gml-web-api .
```

2. Run the container, passing environment variables:

```bash
docker run -d -p 5000:80 --name gml-web-api \
  -e "ASPNETCORE_ENVIRONMENT=Development" \
  -e "SECURITY_KEY=jkuhbsfgvuk4gfikhn8i7wa34rkbqw23" \
  -e "PROJECT_NAME=GmlServer" \
  -e "PROJECT_DESCRIPTION=GmlServer Description" \
  -e "PROJECT_POLICYNAME=GmlPolicy" \
  -e "PROJECT_PATH=" \
  -e "SERVICE_TEXTURE_ENDPOINT=http://localhost:5086" \
  -e "AllowedHosts=*" \
  gml-web-api
```

The API will be accessible at `http://localhost:5000`. The SQLite database file will be created inside the container. To persist the database, mount a volume:

```bash
docker run -d -p 5000:80 --name gml-web-api \
  -v /path/to/local/db:/app \
  -e "ASPNETCORE_ENVIRONMENT=Development" \
  -e "SECURITY_KEY=jkuhbsfgvuk4gfikhn8i7wa34rkbqw23" \
  -e "PROJECT_NAME=GmlServer" \
  -e "PROJECT_DESCRIPTION=GmlServer Description" \
  -e "PROJECT_POLICYNAME=GmlPolicy" \
  -e "PROJECT_PATH=" \
  -e "SERVICE_TEXTURE_ENDPOINT=http://localhost:5086" \
  -e "AllowedHosts=*" \
  gml-web-api
```

Replace `/path/to/local/db` with a local directory to store the SQLite database file.

## Troubleshooting

- **Database Access Issues**: Ensure the SQLite database file directory is writable.
- **Port Conflicts**: If port 5000/5001 is in use, set `ASPNETCORE_URLS` (e.g., `ASPNETCORE_URLS=http://localhost:8080`).
- **Dependency Errors**: Confirm .NET 8.0 SDK is installed and run `dotnet restore`.
- **Docker Issues**: Check container logs with `docker logs gml-web-api` to diagnose errors. Ensure the SQLite database path is accessible.

## Additional Notes

- For production, configure HTTPS and use a reverse proxy (e.g., Nginx).
- Monitor the API using the Gml.Web.Client dashboard from the [Gml.Backend repository](https://github.com/Gml-Launcher/Gml.Backend).
- Visit the [Gml-Launcher organization](https://github.com/Gml-Launcher) or open an issue in this repository for support.
