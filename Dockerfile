# ----- Build stage -----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy .csproj trước để tận dụng cache layer Docker
COPY ["LuongVinhKhang.SachOnline.csproj", "./"]
RUN dotnet restore "LuongVinhKhang.SachOnline.csproj" -v diag

# Copy toàn bộ source code vào container
COPY . .

# Build và publish ra thư mục publish
RUN dotnet publish "LuongVinhKhang.SachOnline.csproj" -c Release -o /app/publish -v diag

# ----- Runtime stage -----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Thiết lập environment
ENV ASPNETCORE_ENVIRONMENT=Production

# Chạy ứng dụng - nhớ đúng tên file .dll
ENTRYPOINT ["dotnet", "LuongVinhKhang.SachOnline.dll"]
