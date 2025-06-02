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
        /// Загружает файл через multipart/form-data.
        /// Возвращает JSON: { "fileId": "GUID" }.
        /// </summary>
        [HttpPost("upload")]
        [RequestSizeLimit(100_000_000)] // 100 МБ лимит тела запроса (настраивается)
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "File is required." });

            // Считываем файл в память
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var memoryStream = new MemoryStream(ms.ToArray());

            // Внутри используется тот же UploadFileCommand, что и в gRPC-хендлере
            var command = new UploadFileCommand(memoryStream, file.FileName);
            var result = await _mediator.Send(command);

            return Ok(new { fileId = result.FileId });
        }

        /// <summary>
        /// Скачивает файл по его GUID.
        /// Возвращает двоичный контент файла.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> Download(string id)
        {
            if (!Guid.TryParse(id, out var fileGuid))
                return BadRequest(new { error = "Invalid GUID format." });

            try
            {
                var dto = await _mediator.Send(new GetFileQuery(fileGuid));
                return File(
                    dto.Content,
                    string.IsNullOrWhiteSpace(dto.ContentType) 
                        ? MediaTypeNames.Application.Octet 
                        : dto.ContentType,
                    dto.FileName
                );
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = $"File with ID = {id} not found." });
            }
        }
    }