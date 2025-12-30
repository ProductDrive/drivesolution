FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the entire solution
COPY . .

# Restore only the API (this restores referenced project dependencies as well)
RUN dotnet restore ./EmailApi/EmailApi.csproj

# Publish
RUN dotnet publish ./EmailApi/EmailApi.csproj -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EmailApi.dll"]
