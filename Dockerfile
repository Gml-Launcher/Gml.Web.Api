FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG TARGETARCH
WORKDIR /src
COPY ["src/Gml.Web.Api/Gml.Web.Api.csproj", "src/Gml.Web.Api/"]
COPY ["src/Gml.Core/src/Gml.Common/Gml.Common/Gml.Common.csproj", "src/Gml.Core/src/Gml.Common/Gml.Common/"]
COPY ["src/Gml.Core/src/Gml.Core/Gml.Core.csproj", "src/Gml.Core/src/Gml.Core/"]
COPY ["src/Gml.Core/src/CmlLib.Core.Installer.Forge/CmlLib.Core.Installer.Forge/CmlLib.Core.Installer.Forge.csproj", "src/Gml.Core/src/CmlLib.Core.Installer.Forge/CmlLib.Core.Installer.Forge/"]
COPY ["src/Gml.Core/src/CmlLib.ExtendedCore/src/CmlLib.Core.csproj", "src/Gml.Core/src/CmlLib.ExtendedCore/src/"]
COPY ["src/Gml.Core/src/CmlLib.Core.Installer.NeoForge/CmlLib.Core.Installer.NeoForge/CmlLib.Core.Installer.NeoForge.csproj", "src/Gml.Core/src/CmlLib.Core.Installer.NeoForge/CmlLib.Core.Installer.NeoForge/"]
COPY ["src/Gml.Core/src/Modrinth.Api/src/Modrinth.Api/Modrinth.Api.csproj", "src/Gml.Core/src/Modrinth.Api/src/Modrinth.Api/"]
COPY ["src/Gml.Core/src/Pingo/Pingo/Pingo.csproj", "src/Gml.Core/src/Pingo/Pingo/"]
COPY ["src/plugins/Gml.Web.Api.EndpointSDK/Gml.Web.Api.EndpointSDK.csproj", "src/plugins/Gml.Web.Api.EndpointSDK/"]
RUN if [ "$TARGETARCH" = "amd64" ]; then RID=linux-x64; elif [ "$TARGETARCH" = "arm64" ]; then RID=linux-arm64; else RID=linux-x64; fi && \
    dotnet restore "src/Gml.Web.Api/Gml.Web.Api.csproj" -r $RID
COPY . .
WORKDIR "/src/src/Gml.Web.Api"
RUN if [ "$TARGETARCH" = "amd64" ]; then RID=linux-x64; elif [ "$TARGETARCH" = "arm64" ]; then RID=linux-arm64; else RID=linux-x64; fi && \
    dotnet build "./Gml.Web.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build -r $RID

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG TARGETARCH
RUN if [ "$TARGETARCH" = "amd64" ]; then RID=linux-x64; elif [ "$TARGETARCH" = "arm64" ]; then RID=linux-arm64; else RID=linux-x64; fi && \
    dotnet publish "./Gml.Web.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish -r $RID --no-self-contained /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gml.Web.Api.dll"]
