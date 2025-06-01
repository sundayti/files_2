using System.Reflection;
using Application.Commands;
using Application.Interfaces;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Presentation.Services;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------
// 1. Загрузка конфигурации
// -------------------------------
builder.Services.Configure<MinioSettings>(
    builder.Configuration.GetSection("Minio")
);

var connectionString = builder.Configuration.GetConnectionString("Postgres");

// -------------------------------
// 2. Регистрация Infrastructure
// -------------------------------
builder.Services.AddDbContext<FileDbContext>(options =>
    options.UseNpgsql(connectionString)
);

builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IStorageClient, MinioStorageClient>();

// -------------------------------
// 3. Регистрация MediatR
// -------------------------------
// Раньше в более старых примерах мог использоваться RegisterServicesFromAssemblyContaining<>
// Теперь нужно передать Assembly руками:

var applicationAssembly = typeof(UploadFileCommand).GetTypeInfo().Assembly;
builder.Services.AddMediatR(applicationAssembly);

// Можно добавлять сразу несколько сборок, если требуется:
// builder.Services.AddMediatR(applicationAssembly, anotherAssembly, ...);

// -------------------------------
// 4. Регистрация gRPC
// -------------------------------
builder.Services.AddGrpc();

var app = builder.Build();

// -------------------------------
// 5. Маршрутизация (Endpoints)
// -------------------------------
app.MapGrpcService<FileStorageGrpcService>();
app.MapGet("/", () => "FileStoringService API is running.");

app.Run();