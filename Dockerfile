# Étape 1 : Compilation du projet
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copier les fichiers .csproj pour mettre en cache la restauration des paquets
COPY ["bothtech.Web/bothtech.Web.csproj", "bothtech.Web/"]
COPY ["bothtech.Shared/bothtech.Shared.csproj", "bothtech.Shared/"]
RUN dotnet restore "bothtech.Web/bothtech.Web.csproj"

# Copier l'intégralité du code et publier les binaires
COPY . .
WORKDIR "/src/bothtech.Web"
RUN dotnet publish "bothtech.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Étape 2 : Image d'exécution légère (ASP.NET Core Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Exposition du port web par défaut
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "bothtech.Web.dll"]