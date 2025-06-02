using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Application.Commands;
using Application.Interfaces;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Infrastructure.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Presentation.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Конфигурация Minio и Postgres (как раньше) ---
builder.Services.Configure<MinioSettings>(
    builder.Configuration.GetSection("Minio"));
var connectionString = builder.Configuration.GetConnectionString("Postgres");

builder.Services.AddDbContext<FileDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IStorageClient, MinioStorageClient>();

// --- MediatR (Application) ---
var appAssembly = typeof(UploadFileCommand).GetTypeInfo().Assembly;
builder.Services.AddMediatR(appAssembly);

// --- Регистрируем gRPC ---
builder.Services.AddGrpc();

// --- Регистрируем MVC/Controllers для REST ---
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        // например, стриминговая сериализация, если нужно; но по умолчанию хватит
    });

var app = builder.Build();

// Если нужно слушать HTTP/2 без TLS (опционально), но для REST достаточно HTTP/1.1      
// app.Services … (оставляем стандартные Kestrel-настройки, которые слушают 5001 с HTTPS и 5000 без HTTPS)

// --- Маршруты gRPC ---
app.MapGrpcService<FileStorageGrpcService>();

// --- Маршруты REST ---
app.MapControllers();

// --- Проверочный ping ---
app.MapGet("/", () => "FileStoringService API is running.");

builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP/1.1 (REST)
    options.ListenAnyIP(5002, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });

    // HTTPS/HTTP2 (gRPC)
    options.ListenAnyIP(5001, listenOptions =>
    {
        listenOptions.UseHttps();             // самоподписанный или настоящий, если есть
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});
app.Run();