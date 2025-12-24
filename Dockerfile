# Estágio de Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/API/API.csproj", "src/API/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Core/Core.csproj", "src/Core/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]

RUN dotnet restore "src/API/API.csproj"

COPY . .

WORKDIR "/src/src/API"
RUN dotnet publish "API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

RUN mkdir -p /app/data

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "API.dll"]