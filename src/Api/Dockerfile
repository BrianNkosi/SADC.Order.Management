FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Api/SADC.Order.Management.Api.csproj", "src/Api/"]
COPY ["src/Application/SADC.Order.Management.Application.csproj", "src/Application/"]
COPY ["src/Domain/SADC.Order.Management.Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/SADC.Order.Management.Infrastructure.csproj", "src/Infrastructure/"]
RUN dotnet restore "src/Api/SADC.Order.Management.Api.csproj"
COPY . .
WORKDIR "/src/src/Api"
RUN dotnet build "SADC.Order.Management.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SADC.Order.Management.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SADC.Order.Management.Api.dll"]
