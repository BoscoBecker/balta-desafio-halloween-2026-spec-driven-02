FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY src/UrlShortener.Api/UrlShortener.Api.csproj src/UrlShortener.Api/
COPY src/UrlShortener.Application/UrlShortener.Application.csproj src/UrlShortener.Application/
COPY src/UrlShortener.Domain/UrlShortener.Domain.csproj src/UrlShortener.Domain/
COPY src/UrlShortener.Infrastructure/UrlShortener.Infrastructure.csproj src/UrlShortener.Infrastructure/

RUN dotnet restore src/UrlShortener.Api/UrlShortener.Api.csproj

COPY src/ src/
RUN dotnet publish src/UrlShortener.Api/UrlShortener.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

RUN mkdir -p /data && chown $APP_UID:$APP_UID /data
USER $APP_UID

ENTRYPOINT ["dotnet", "UrlShortener.Api.dll"]