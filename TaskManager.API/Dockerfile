# ============ Stage 1: Build ============
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем .csproj и восстанавливаем зависимости
COPY ["TaskManager.API/TaskManager.API.csproj", "TaskManager.API/"]
COPY ["TaskManager.Application/TaskManager.Application.csproj", "TaskManager.Application/"]
COPY ["TaskManager.Domain/TaskManager.Domain.csproj", "TaskManager.Domain/"]
COPY ["TaskManager.Infrastructure/TaskManager.Infrastructure.csproj", "TaskManager.Infrastructure/"]

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
RUN dotnet restore "TaskManager.API/TaskManager.API.csproj"

# Копируем весь код и собираем
COPY . .
WORKDIR "/src/TaskManager.API"
RUN dotnet build "TaskManager.API.csproj" -c Release -o /app/build

# ============ Stage 2: Publish ============
FROM build AS publish
RUN dotnet publish "TaskManager.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ============ Stage 3: Runtime ============
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Создаём непривилегированного пользователя
RUN useradd -m appuser && chown -R appuser:appuser /app

# Устанавливаем curl для healthcheck
USER root
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Возвращаемся к непривилегированному пользователю
USER appuser

# Копируем опубликованное приложение
COPY --from=publish /app/publish .

# Открываем порт
EXPOSE 8080

# Запускаем приложение
ENTRYPOINT ["dotnet", "TaskManager.API.dll"]
