FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 7031
EXPOSE 7032

VOLUME ["/var/log/notionapi"]

ENV ASPNETCORE_ENVIRONMENT=Production \
    TZ=Asia/Tokyo

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Notion.Sync.Api.csproj", "."]
RUN dotnet restore "./Notion.Sync.Api.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./Notion.Sync.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Notion.Sync.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Notion.Sync.Api.dll"]