FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Notion.Sync.Api/Notion.Sync.Api.csproj", "./"]
RUN dotnet restore
COPY Notion.Sync.Api/ ./
RUN dotnet publish -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish ./
RUN mkdir -p /var/log/notionapi && chown -R $APP_UID:$APP_UID /var/log/notionapi
USER $APP_UID
ENV ASPNETCORE_ENVIRONMENT=Production
ENV TZ=Asia/Tokyo
EXPOSE 7031
ENTRYPOINT ["dotnet", "Notion.Sync.Api.dll"]
