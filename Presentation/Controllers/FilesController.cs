using System.Net.Mime;
using Application.Commands;
using Application.DTOs;
using Application.Queries;
using MediatR;

using MediatR;
using Microsoft.AspNetCore.Mvc;
namespace Presentation.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly IMediator _mediator;

    public FilesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// REST-концевик для загрузки файла.
    /// Получает multipart/form-data: поле "file".
    /// Возвращает JSON { "fileId": "GUID" }.
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(100_000_000)] // до 100 МБ, настроить по необходимости
    public async Task<IActionResult> Upload([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "File is required." });

        // Считываем содержимое в память (MemoryStream)
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        byte[] content = ms.ToArray();

        // Формируем команду для MediatR
        var command = new UploadFileCommand(new MemoryStream(content), file.FileName);
        UploadFileResult result = await _mediator.Send(command);

        return Ok(new { fileId = result.FileId });
    }

    /// <summary>
    /// REST-концевик для скачивания файла.
    /// GET /api/files/{id}
    /// Возвращает File(byte[], contentType, fileName).
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> Download(string id)
    {
        if (!Guid.TryParse(id, out var fileGuid))
            return BadRequest(new { error = "Invalid GUID format." });

        try
        {
            // Запрашиваем через MediatR
            FileDto fileDto = await _mediator.Send(new GetFileQuery(fileGuid));

            // Возвращаем двоичный файл с правильными заголовками
            return File(
                fileDto.Content,
                string.IsNullOrWhiteSpace(fileDto.ContentType) 
                    ? MediaTypeNames.Application.Octet : fileDto.ContentType,
                fileDto.FileName
            );
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = $"File with ID = {id} not found." });
        }
    }
}