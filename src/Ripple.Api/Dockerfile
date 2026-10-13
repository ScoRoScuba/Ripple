FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Ripple.Api/Ripple.Api.csproj",           "src/Ripple.Api/"]
COPY ["src/Ripple.Application/Ripple.Application.csproj", "src/Ripple.Application/"]
COPY ["src/Ripple.Infrastructure/Ripple.Infrastructure.csproj", "src/Ripple.Infrastructure/"]
COPY ["src/Ripple.Domain/Ripple.Domain.csproj",     "src/Ripple.Domain/"]
RUN dotnet restore "src/Ripple.Api/Ripple.Api.csproj"
COPY . .
WORKDIR "/src/src/Ripple.Api"
RUN dotnet publish "Ripple.Api.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ripple.Api.dll"]
