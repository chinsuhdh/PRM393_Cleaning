FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["CleaningService.API/CleaningService.API.csproj", "CleaningService.API/"]
COPY ["BLL/Cleaning.BLL.csproj", "BLL/"]
COPY ["DAL/Cleaning.DAL.csproj", "DAL/"]

RUN dotnet restore "CleaningService.API/CleaningService.API.csproj"

COPY . .

WORKDIR "/src/CleaningService.API"
RUN dotnet build "CleaningService.API.csproj" -c Release -o /app/build
RUN dotnet publish "CleaningService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CleaningService.API.dll"]