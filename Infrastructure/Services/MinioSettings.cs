namespace Infrastructure.Services;

/// <summary>
/// Настройки для подключения к MinIO (или S3-совместимому хранилищу).
/// </summary>
public class MinioSettings
{
    /// <summary>
    /// Endpoint MinIO (например, "localhost:9000" или "minio.cloud.provider:9000").
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Ключ доступа (Access Key) для MinIO.
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Секретный ключ (Secret Key) для MinIO.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Имя бакета (bucket) в MinIO, в который будут сохраняться файлы.
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// Нужно ли использовать SSL (true/false). 
    /// Например, если у вас MinIO работает по http, ставьте false.
    /// </summary>
    public bool UseSsl { get; set; } = true;
}