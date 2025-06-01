using Application.Interfaces;
using Domain.ValueObjects;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace Infrastructure.Services;

 /// <summary>
    /// Реализация IStorageClient на основе MinIO .NET SDK.
    /// Позволяет сохранять и читать файлы из S3-совместимого хранилища (MinIO).
    /// </summary>
    public class MinioStorageClient : IStorageClient
    {
        private readonly IMinioClient _minioClient;
        private readonly string _bucketName;

        public MinioStorageClient(IOptions<MinioSettings> options)
        {
            var settings = options.Value 
                ?? throw new ArgumentNullException(nameof(options), "MinioSettings не сконфигурированы.");

            _bucketName = settings.BucketName;

            // Строим клиент MinIO
            _minioClient = new MinioClient()
                .WithEndpoint(settings.Endpoint)
                .WithCredentials(settings.AccessKey, settings.SecretKey)
                .WithSSL(settings.UseSsl)
                .Build();
        }

        /// <summary>
        /// Сохраняет файл (массив байт) в указанный бакет и возвращает FileLocation (ключ/имя объекта).
        /// </summary>
        public async Task<FileLocation> SaveAsync(byte[] content, string fileName, CancellationToken cancellationToken)
        {
            // Генерируем уникальное имя объекта в бакете, чтобы не было коллизий.
            // В именовании можно использовать GUID + исходное имя.
            var objectName = $"{Guid.NewGuid()}_{fileName}";

            // Убедимся, что бакет существует; если нет — создадим.
            try
            {
                bool foundBucket = await _minioClient.BucketExistsAsync(
                    new BucketExistsArgs().WithBucket(_bucketName),
                    cancellationToken);
                if (!foundBucket)
                {
                    await _minioClient.MakeBucketAsync(
                        new MakeBucketArgs().WithBucket(_bucketName),
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Не удалось проверить или создать бакет '{_bucketName}' в MinIO.", ex);
            }

            // Загружаем объект в MinIO
            try
            {
                using var ms = new MemoryStream(content);
                var putArgs = new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectName)
                    .WithStreamData(ms)
                    .WithObjectSize(ms.Length)
                    // Можно явно задать ContentType, например, по расширению:
                    // .WithContentType("application/octet-stream")
                    ;

                await _minioClient.PutObjectAsync(putArgs, cancellationToken);
                return new FileLocation(objectName);
            }
            catch (MinioException minioEx)
            {
                throw new Exception($"Ошибка при загрузке объекта в MinIO: {minioEx.Message}", minioEx);
            }
            catch (Exception ex)
            {
                throw new Exception("Неизвестная ошибка при сохранении файла в MinIO.", ex);
            }
        }

        /// <summary>
        /// Считывает файл из MinIO по ключу (FileLocation.Value) и возвращает байты.
        /// </summary>
        public async Task<byte[]> GetAsync(FileLocation location, CancellationToken cancellationToken)
        {
            var objectName = location.Value;

            try
            {
                using var ms = new MemoryStream();
                var getArgs = new GetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectName)
                    .WithCallbackStream(stream =>
                    {
                        // При успешном получении библиотека MinIO .NET SDK откроет поток с контентом.
                        stream.CopyTo(ms);
                    });

                await _minioClient.GetObjectAsync(getArgs, cancellationToken);

                return ms.ToArray();
            }
            catch (MinioException minioEx)
            {
                throw new Exception($"Не удалось получить объект '{objectName}' из MinIO: {minioEx.Message}", minioEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Неизвестная ошибка при чтении объекта '{objectName}' из MinIO.", ex);
            }
        }
    }