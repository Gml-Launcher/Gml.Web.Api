FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
RUN apt-get update && apt-get install -y git
USER $APP_UID

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Gml.Web.Api/Gml.Web.Api.csproj", "src/Gml.Web.Api/"]
COPY ["src/plugins/Gml.Web.Api.EndpointSDK/Gml.Web.Api.EndpointSDK.csproj", "src/plugins/Gml.Web.Api.EndpointSDK/"]
RUN dotnet restore "src/Gml.Web.Api/Gml.Web.Api.csproj"
COPY . .
WORKDIR "/src/src/Gml.Web.Api"
RUN dotnet build "./Gml.Web.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Gml.Web.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gml.Web.Api.dll"]
