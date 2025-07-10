----- Build stage -----

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build WORKDIR /app

Copy .csproj trước để tận dụng cache

COPY ["LuongVinhKhang.SachOnline.csproj", "."] RUN dotnet restore "LuongVinhKhang.SachOnline.csproj" -v diag

Copy toàn bộ source code

COPY . .

Build và publish

RUN dotnet publish "LuongVinhKhang.SachOnline.csproj" -c Release -o /app/publish -v diag

----- Runtime stage -----

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime WORKDIR /app COPY --from=build /app/publish .

Cấu hình environment cho ASP.NET Core

ENV ASPNETCORE_ENVIRONMENT=Production

Run app với tên DLL đúng

ENTRYPOINT ["dotnet", "LuongVinhKhang.SachOnline.New.dll"]