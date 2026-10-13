FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Ripple.Migrations/Ripple.Migrations.csproj", "src/Ripple.Migrations/"]
RUN dotnet restore "src/Ripple.Migrations/Ripple.Migrations.csproj"
COPY . .
WORKDIR "/src/src/Ripple.Migrations"
RUN dotnet publish "Ripple.Migrations.csproj" -c $BUILD_CONFIGURATION -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Ripple.Migrations.dll"]
