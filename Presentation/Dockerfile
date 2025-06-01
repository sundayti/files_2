# -----------------------------
# 1. Сборка (build) .NET SDK
# -----------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 1.1. Копируем только файлы .csproj для каждого проекта, чтобы ускорить кэширование слоёв
COPY ["Presentation/Presentation.csproj", "Presentation/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Domain/Domain.csproj", "Domain/"]

# 1.2. Восстанавливаем зависимости (restore) для Presentation.csproj
RUN dotnet restore "Presentation/Presentation.csproj"

# 1.3. Копируем все остальные файлы
COPY . .

# Переходим в папку Presentation для сборки
WORKDIR "/src/Presentation"

# 1.4. Сборка и публикация в папку /app/publish
RUN dotnet publish -c Release -o /app/publish

# -----------------------------
# 2. Runtime-образ
# -----------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# 2.1. Копируем опубликованные файлы из слоя "build"
COPY --from=build /app/publish .

# 2.2. Открываем порт для gRPC (5001 по умолчанию для HTTPS/HTTP2)
EXPOSE 5001

# 2.3. Запускаем
ENTRYPOINT ["dotnet", "Presentation.dll"]