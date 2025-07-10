# ----- Build stage -----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy toàn bộ source code
COPY . .

# Restore NuGet packages
RUN dotnet restore

# Build và publish
RUN dotnet publish -c Release -o /app/publish

# ----- Runtime stage -----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy output từ build stage qua
COPY --from=build /app/publish .

# Run app
ENTRYPOINT ["dotnet", "LuongVinhKhang.SachOnline.dll"]
