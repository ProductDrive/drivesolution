# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY .. .
RUN dotnet restore ./EmailApi/EmailApi.csproj
RUN dotnet publish ./EmailApi/EmailApi.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 3588
ENTRYPOINT ["dotnet", "EmailApi.dll"]