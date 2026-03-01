FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Worker/SADC.Order.Management.Worker.csproj", "src/Worker/"]
COPY ["src/Application/SADC.Order.Management.Application.csproj", "src/Application/"]
COPY ["src/Domain/SADC.Order.Management.Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/SADC.Order.Management.Infrastructure.csproj", "src/Infrastructure/"]
RUN dotnet restore "src/Worker/SADC.Order.Management.Worker.csproj"
COPY . .
WORKDIR "/src/src/Worker"
RUN dotnet build "SADC.Order.Management.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SADC.Order.Management.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SADC.Order.Management.Worker.dll"]
