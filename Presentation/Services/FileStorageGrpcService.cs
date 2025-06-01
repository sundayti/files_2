using Application.Commands;
using Application.DTOs;
using Application.Queries;
using MediatR;
using Grpc.Core;
using Google.Protobuf;            
using Presentation.Protos;
namespace Presentation.Services;

/// <summary>
/// gRPC-сервис FileStorage (реализация методов UploadFile и DownloadFile).
/// Наследуется от автоматически сгенерированного FileStorage.FileStorageBase.
/// </summary>
public class FileStorageGrpcService : FileStorage.FileStorageBase
{
    private readonly IMediator _mediator;

    public FileStorageGrpcService(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Unary RPC: загружает файл (получает bytes + file_name), создаёт UploadFileCommand и отправляет его в MediatR.
    /// Возвращает UploadFileReply с новым FileId.
    /// </summary>
    public override async Task<UploadFileReply> UploadFile(UploadFileRequest request, ServerCallContext context)
    {
        try
        {
            // 1) Преобразуем content (ByteString) в byte[]
            byte[] fileBytes = request.Content.ToByteArray();

            // 2) Получаем имя файла; если пустое или null — выбрасываем ошибку InvalidArgument
            if (string.IsNullOrWhiteSpace(request.FileName))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "FileName must be provided."));
            }
            string fileName = request.FileName;

            // 3) Заворачиваем byte[] в MemoryStream, чтобы передать в команду
            using var ms = new MemoryStream(fileBytes);

            // 4) Формируем команду и отправляем в MediatR
            var command = new UploadFileCommand(ms, fileName);
            UploadFileResult result = await _mediator.Send(command, context.CancellationToken);

            // 5) Возвращаем gRPC-ответ с FileId (Guid в виде строки)
            return new UploadFileReply
            {
                FileId = result.FileId.ToString()
            };
        }
        catch (RpcException)
        {
            // Если мы уже бросили RpcException (например, из-за некорректного аргумента),
            // передаём его дальше без обёртки.
            throw;
        }
        catch (Exception ex)
        {
            // Любая другая неожиданная ошибка → возвращаем Internal
            throw new RpcException(new Status(StatusCode.Internal, $"Server error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Unary RPC: скачивает файл по FileId. 
    /// Формируем GetFileQuery и возвращаем DownloadFileReply (content, file_name, content_type).
    /// </summary>
    public override async Task<DownloadFileReply> DownloadFile(FileRequest request, ServerCallContext context)
    {
        try
        {
            // 1) Парсим file_id в Guid
            if (!Guid.TryParse(request.FileId, out var fileGuid))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid FileId format."));
            }

            // 2) Запрашиваем через MediatR
            var query = new GetFileQuery(fileGuid);
            FileDto fileDto = await _mediator.Send(query, context.CancellationToken);

            // 3) Формируем gRPC-ответ
            return new DownloadFileReply
            {
                Content = ByteString.CopyFrom(fileDto.Content),
                FileName = fileDto.FileName,
                ContentType = fileDto.ContentType
            };
        }
        catch (RpcException)
        {
            // Если уже это была RpcException (NotFound или InvalidArgument), отдадим как есть
            throw;
        }
        catch (KeyNotFoundException)
        {
            // Файл не найден в БД → NotFound
            throw new RpcException(new Status(StatusCode.NotFound, $"File with ID={request.FileId} not found."));
        }
        catch (Exception ex)
        {
            // Любая другая ошибка → Internal
            throw new RpcException(new Status(StatusCode.Internal, $"Server error: {ex.Message}"));
        }
    }
}