# Use an official dotnet runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Update packages and install openssl
RUN apt-get update -y && \
    apt-get install -y openssl

# Specify a user to prevent running the container as root
USER $APP_UID

# Set the working directory for any subsequent ADD, COPY, CMD, ENTRYPOINT, or RUN instructions that follow this instruction in the Dockerfile.
WORKDIR /app

# Use an official dotnet SDK as a build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files and run dotnet restore
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Gml.Web.Api/Gml.Web.Api.csproj", "src/Gml.Web.Api/"]
COPY ["src/Gml.Common/Gml.Common/Gml.Common.csproj", "src/Gml.Common/Gml.Common/"]
COPY ["src/Gml.Core/src/CmlLib.Core.Installer.Forge/CmlLib.Core.Installer.Forge/CmlLib.Core.Installer.Forge.csproj", "src/Gml.Core/src/CmlLib.Core.Installer.Forge/CmlLib.Core.Installer.Forge/"]
COPY ["src/Gml.Core/src/CmlLib.ExtendedCore/CmlLib/CmlLib.csproj", "src/Gml.Core/src/CmlLib.ExtendedCore/CmlLib/"]
COPY ["src/Gml.Web.Api.Domains/Gml.Web.Api.Domains.csproj", "src/Gml.Web.Api.Domains/"]
COPY ["src/Gml.Web.Api.Dto/Gml.Web.Api.Dto.csproj", "src/Gml.Web.Api.Dto/"]
COPY ["src/Gml.Core/src/Gml.Core/GmlCore.csproj", "src/Gml.Core/src/GmlCore/"]
COPY ["src/Gml.Core/src/Gml.Core.Interfaces/Gml.Core.Interfaces.csproj", "src/Gml.Core/src/Gml.Core.Interfaces/"]
RUN dotnet restore "src/Gml.Web.Api/Gml.Web.Api.csproj"

# Copy the rest of the files and build the project
COPY . .
WORKDIR "/src/src/Gml.Web.Api"
RUN dotnet build "Gml.Web.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Gml.Web.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Java installation stage
FROM adoptopenjdk:11-jdk AS java


# Copy the .NET runtime, and your app and java from the separate stages.
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=java /opt/java/openjdk .

# Set environment variable for Java.
ENV PATH="/opt/java/openjdk/bin:${PATH}"

ENTRYPOINT ["dotnet", "Gml.Web.Api.dll"]
