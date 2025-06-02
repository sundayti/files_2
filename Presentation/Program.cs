using System.Reflection;
using Application.Commands;
using Application.Interfaces;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using Presentation.Services;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------
// 1. Загрузка конфигурации (appsettings.json / .env) 
// ------------------------------------------------------
builder.Configuration.AddEnvironmentVariables();

// ------------------------------------------------------
// 2. Регистрация MinIO-настроек
// ------------------------------------------------------
builder.Services.Configure<MinioSettings>(
    builder.Configuration.GetSection("Minio")
);

// ------------------------------------------------------
// 3. Регистрация DbContext (PostgreSQL) + репозиторий + MinIO-клиент
// ------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("Postgres");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
}

builder.Services.AddDbContext<FileDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// Регистрируем репозиторий и хранилище
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IStorageClient, MinioStorageClient>();

// ------------------------------------------------------
// 4. Регистрация MediatR (Application layer)
// ------------------------------------------------------
var applicationAssembly = typeof(UploadFileCommand).GetTypeInfo().Assembly;
builder.Services.AddMediatR(applicationAssembly);

// ------------------------------------------------------
// 5. Регистрация gRPC
// ------------------------------------------------------
builder.Services.AddGrpc();

// ------------------------------------------------------
// 6. Регистрация REST/Swagger (Controllers + Swashbuckle)
// ------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "FileStoringService API",
            Version = "v1",
            Description = "REST API для загрузки/скачивания файлов"
        }
    );
    c.EnableAnnotations();
});

// ------------------------------------------------------
// 7. Конфигурация Kestrel: 
//    - HTTP/1.1 (+ HTTP/2) для REST на 5002 
//    - HTTP/2 для gRPC на 5001
// ------------------------------------------------------
builder.WebHost.ConfigureKestrel(options =>
{
    // REST: HTTP/1.1 + HTTP/2 plaintext на порт 5002
    options.ListenAnyIP(5002, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });

    // gRPC: HTTP/2 plaintext на порт 5001 (UseHttps не нужен — TLS делает Nginx)
    options.ListenAnyIP(5001, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

var app = builder.Build();

// ------------------------------------------------------
// 8. Автоматическое применение миграций при старте
// ------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FileDbContext>();
    // Попытаемся применить все миграции. Если таблицы отсутствуют, EF Core их создаст.
    dbContext.Database.Migrate();
}

// ------------------------------------------------------
// 9. Middleware при разработке
// ------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// ------------------------------------------------------
// 10. Swagger UI (REST-документация)
// ------------------------------------------------------
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FileStoringService API V1");
    c.RoutePrefix = "swagger";
});

// ------------------------------------------------------
// 11. Маршрутизация: gRPC + REST
// ------------------------------------------------------
app.MapGrpcService<FileStorageGrpcService>();
app.MapControllers();
app.MapGet("/", () => "FileStoringService API is running.");

// ------------------------------------------------------
// 12. Запуск
// ------------------------------------------------------
app.Run();