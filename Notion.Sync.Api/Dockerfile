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
ARG ENV
ARG DB_HOST
ARG DB_NAME
ARG DB_PORT
ARG LAMBDA_KEY
ARG NOTION_ACTIVE_USER
ARG NOTION_TOKEN_V2
ARG SECRET_ID
ARG AWS_REGION

USER root
WORKDIR /app
RUN mkdir -p ./node-app && \
    echo "ENV=${ENV}" >> ./node-app/.env && \
    echo "DB_HOST=${DB_HOST}" >> ./node-app/.env && \
    echo "DB_NAME=${DB_NAME}" >> ./node-app/.env && \   
    echo "DB_PORT=${DB_PORT}" >> ./node-app/.env && \
    echo "LAMBDA_KEY=${LAMBDA_KEY}" >> ./node-app/.env && \
    echo "NOTION_ACTIVE_USER=${NOTION_ACTIVE_USER}" >> ./node-app/.env && \
    echo "NOTION_TOKEN_V2=${NOTION_TOKEN_V2}" >> ./node-app/.env && \
    echo "SECRET_ID=${SECRET_ID}" >> ./node-app/.env && \
    echo "AWS_REGION=${AWS_REGION}" >> ./node-app/.env

WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=node-build /src/node-app ./node-app
RUN chown -R $APP_UID:$APP_UID /app

USER $APP_UID
ENTRYPOINT ["bash", "-c", "dotnet Notion.Sync.Api.dll & cd ./node-app && node --env-file=.env index.js & wait -n"]