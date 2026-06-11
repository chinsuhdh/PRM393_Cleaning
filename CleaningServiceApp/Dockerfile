# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# SỬA LẠI ĐƯỜNG DẪN Ở ĐÂY CHO ĐÚNG VỚI TÊN THƯ MỤC VẬT LÝ
COPY ["CleaningService.API/CleaningService.API.csproj", "CleaningService.API/"]
COPY ["BLL/Cleaning.BLL.csproj", "BLL/"]
COPY ["DAL/Cleaning.DAL.csproj", "DAL/"]

# Chạy lệnh restore trên project chính (API)
RUN dotnet restore "CleaningService.API/CleaningService.API.csproj"

# Copy toàn bộ source code còn lại vào container
COPY . .

# Build và Publish
WORKDIR "/src/CleaningService.API"
RUN dotnet build "CleaningService.API.csproj" -c Release -o /app/build
RUN dotnet publish "CleaningService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CleaningService.API.dll"]