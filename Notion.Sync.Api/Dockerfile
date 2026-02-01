FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get install -y curl && \
    curl -fsSL https://deb.nodesource.com/setup_22.x | bash - && \
    apt-get install -y nodejs && \
    rm -rf /var/lib/apt/lists/*

USER $APP_UID
WORKDIR /app
EXPOSE 7031
EXPOSE 7032
VOLUME ["/var/log/notionapi"]
ENV ASPNETCORE_ENVIRONMENT=Production
ENV TZ=Asia/Tokyo

FROM node:22-slim AS node-build
WORKDIR /src/node-app
COPY ["notion.sync.api.node/package*.json", "./"]
RUN npm install
COPY notion.sync.api.node/ .

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Notion.Sync.Api/Notion.Sync.Api.csproj", "Notion.Sync.Api/"]
RUN dotnet restore "Notion.Sync.Api/Notion.Sync.Api.csproj"
COPY . .
WORKDIR "/src/Notion.Sync.Api"
RUN dotnet build "./Notion.Sync.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Notion.Sync.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=node-build /src/node-app ./node-app
ENTRYPOINT ["bash", "-c", "dotnet Notion.Sync.Api.dll & cd ./node-app && node --env-file=.env index.js & wait -n"]