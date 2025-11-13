# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY .. .
RUN dotnet restore ./DriveSolution.ApiService/DriveSolution.ApiService.csproj
RUN dotnet publish ./DriveSolution.ApiService/DriveSolution.ApiService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8090
EXPOSE 8080
ENTRYPOINT ["dotnet", "DriveSolution.ApiService.dll"]
