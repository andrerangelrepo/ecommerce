# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ECommerce.sln ./
COPY src/ECommerce.Domain/ECommerce.Domain.csproj src/ECommerce.Domain/
COPY src/ECommerce.Application/ECommerce.Application.csproj src/ECommerce.Application/
COPY src/ECommerce.Infrastructure/ECommerce.Infrastructure.csproj src/ECommerce.Infrastructure/
COPY src/ECommerce.API/ECommerce.API.csproj src/ECommerce.API/
RUN dotnet restore src/ECommerce.API/ECommerce.API.csproj

COPY src/ src/
RUN dotnet publish src/ECommerce.API/ECommerce.API.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

RUN mkdir -p /app/data && chown -R $APP_UID:$APP_UID /app/data

ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__Database="Data Source=/app/data/orders.db"
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "ECommerce.API.dll"]
