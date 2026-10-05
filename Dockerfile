FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY backend/InSync.Api/InSync.Api.csproj backend/InSync.Api/
RUN dotnet restore backend/InSync.Api/InSync.Api.csproj
COPY backend/InSync.Api/ backend/InSync.Api/
WORKDIR /src/backend/InSync.Api
RUN dotnet publish -c Release -o /out --no-restore

FROM runtime AS final
WORKDIR /app
COPY --from=build /out .
USER $APP_UID
ENTRYPOINT ["dotnet","InSync.Api.dll"]
