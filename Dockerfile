# ----- Build stage -----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy toàn bộ source vào container
COPY . .

# Di chuyển vào đúng folder chứa .csproj
WORKDIR /src/LuongVinhKhang.SachOnline

# Restore dependencies
RUN dotnet restore

# Build và publish
RUN dotnet publish -c Release -o /app/publish

# ----- Runtime stage -----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "LuongVinhKhang.SachOnline.dll"]
