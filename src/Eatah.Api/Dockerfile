# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY global.json ./
COPY src/Eatah.Api/Eatah.Api.csproj src/Eatah.Api/
COPY src/Eatah.Domain/Eatah.Domain.csproj src/Eatah.Domain/
COPY src/Eatah.Infrastructure/Eatah.Infrastructure.csproj src/Eatah.Infrastructure/
RUN dotnet restore src/Eatah.Api/Eatah.Api.csproj

COPY src/Eatah.Api/ src/Eatah.Api/
COPY src/Eatah.Domain/ src/Eatah.Domain/
COPY src/Eatah.Infrastructure/ src/Eatah.Infrastructure/

RUN dotnet publish src/Eatah.Api/Eatah.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "Eatah.Api.dll"]
