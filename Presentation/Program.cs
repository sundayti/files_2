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
using Microsoft.OpenApi.Models;
using Presentation.Services;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------
// 1. Настройки MinIO (из .env или appsettings.json)
// -------------------------------
builder.Services.Configure<MinioSettings>(
    builder.Configuration.GetSection("Minio")
);

// -------------------------------
// 2. DbContext (PostgreSQL) + Репозиторий + MinIO-клиент
// -------------------------------
var connectionString = builder.Configuration.GetConnectionString("Postgres");
builder.Services.AddDbContext<FileDbContext>(options =>
    options.UseNpgsql(connectionString)
);

builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IStorageClient, MinioStorageClient>();

// -------------------------------
// 3. MediatR (Application)
// -------------------------------
var applicationAssembly = typeof(UploadFileCommand).GetTypeInfo().Assembly;
builder.Services.AddMediatR(applicationAssembly);

// -------------------------------
// 4. gRPC
// -------------------------------
builder.Services.AddGrpc();

// -------------------------------
// 5. REST/Swagger (Controllers + Swashbuckle)
// -------------------------------
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
            Description = "REST-часть API для загрузки/скачивания файлов"
        }
    );
    c.EnableAnnotations();
});

// -------------------------------
// 6. Kestrel: HTTP/1.1 (+ HTTP/2 plaintext) для REST (5002) и HTTP/2 plaintext для gRPC (5001)
// -------------------------------
builder.WebHost.ConfigureKestrel(options =>
{
    // 6.1. HTTP/1.1 (+ HTTP/2 plaintext) для REST-контроллеров на порту 5002
    options.ListenAnyIP(5002, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });

    // 6.2. HTTP/2 plaintext (без TLS) для gRPC на порту 5001
    options.ListenAnyIP(5001, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
        // НЕ вызываем listenOptions.UseHttps() — т.к. TLS завершает Nginx
    });
});

var app = builder.Build();

// -------------------------------
// 7. Мидлвары
// -------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// 7.1. Swagger (REST-докация)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FileStoringService API V1");
    c.RoutePrefix = "swagger";
});

// 7.2. Маршрутизация
app.UseRouting();

// -------------------------------
// 8. Эндпоинты
// -------------------------------
// 8.1. gRPC-сервис
app.MapGrpcService<FileStorageGrpcService>();

// 8.2. REST-контроллеры (FilesController)
app.MapControllers();

// 8.3. Простой «ping»
app.MapGet("/", () => "FileStoringService API is running.");

// -------------------------------
// 9. Запуск
// -------------------------------
app.Run();