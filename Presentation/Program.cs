using System.Reflection;
using Application.Commands;
using Application.Interfaces;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
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
    // Если используете аннотации [SwaggerOperation], [SwaggerResponse] и т. д.:
    c.EnableAnnotations();
});

// -------------------------------
// 6. Kestrel: HTTP/1.1 для REST (5002) и HTTPS/HTTP2 для gRPC (5001)
// -------------------------------
builder.WebHost.ConfigureKestrel(options =>
{
    // 6.1. HTTP/1.1 (+ опционально HTTP/2) для REST-контроллеров
    options.ListenAnyIP(5002, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });

    // 6.2. HTTPS + HTTP/2 для gRPC
    options.ListenAnyIP(5001, listenOptions =>
    {
        listenOptions.UseHttps();                // самоподписанный сертификат в dev
        listenOptions.Protocols = HttpProtocols.Http2;
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
    c.RoutePrefix = "swagger";  // Swagger UI будет доступен по /swagger
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

// 8.3. Простой «ping» (можно убрать)
app.MapGet("/", () => "FileStoringService API is running.");

// -------------------------------
// 9. Запуск
// -------------------------------
app.Run();