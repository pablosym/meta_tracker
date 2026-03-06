# Etapa runtime (ASP.NET Core 9)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
# La app escucha en 8080 dentro del contenedor
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Etapa build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiamos solo el csproj para cachear restore
COPY ["Tracker.csproj", "./"]
RUN dotnet restore "Tracker.csproj"

# Copiamos el resto
COPY . .
RUN dotnet build "Tracker.csproj" -c Release -o /app/build

# Etapa publish
FROM build AS publish
RUN dotnet publish "Tracker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tracker.dll"]
