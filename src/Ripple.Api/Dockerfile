FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Ripple.Api/Ripple.Api.csproj",             "src/Ripple.Api/"]
COPY ["src/Ripple.Interfaces/Ripple.Interfaces.csproj", "src/Ripple.Interfaces/"]
COPY ["src/Ripple.Services/Ripple.Services.csproj",   "src/Ripple.Services/"]
COPY ["src/Ripple.DataStore/Ripple.DataStore.csproj", "src/Ripple.DataStore/"]
COPY ["src/Ripple.Domain/Ripple.Domain.csproj",       "src/Ripple.Domain/"]
RUN dotnet restore "src/Ripple.Api/Ripple.Api.csproj"
COPY . .
WORKDIR "/src/src/Ripple.Api"
RUN dotnet publish "Ripple.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Ripple.Api.dll"]
