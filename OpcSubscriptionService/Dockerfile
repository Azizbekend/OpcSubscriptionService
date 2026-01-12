@@ -1,24 +0,0 @@
# 1. Stage: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем csproj и восстанавливаем зависимости
COPY *.csproj ./
RUN dotnet restore

# Копируем остальной проект
COPY . .
RUN dotnet publish -c Release -o /app/publish

# 2. Stage: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Устанавливаем URL, на котором будет слушать ваше API
ENV ASPNETCORE_URLS=http://0.0.0.0:8088

EXPOSE 8088

ENTRYPOINT ["dotnet", "OpcSubscriptionService.dll"]