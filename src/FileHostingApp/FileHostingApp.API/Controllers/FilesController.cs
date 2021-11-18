using FileHostingApp.BLL.DTOs.ViewModels;
using FileHostingApp.BLL.Interfaces;
using FileHostingApp.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileHostingApp.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly IStorageService _storageService;

        public FilesController(IStorageService storageService)
        {
            _storageService = storageService;
        }

        [HttpGet("download")]
        public async Task<Stream> DownloadFile([FromQuery] string filePath, CancellationToken cancellationToken)
        {
            var file = await _storageService.DownloadFileAsync(filePath, cancellationToken);
            return file;
        }

        [HttpDelete]
        public async Task<ActionResult> DeleteFile([FromQuery] string filePath, CancellationToken cancellationToken)
        {
            await _storageService.DeleteFileAsync(filePath, cancellationToken);
            return Ok();
        }

        [HttpPost("uploadmultiple")]
        public async Task<ActionResult> UploadFile([FromForm] IEnumerable<IFormFile> formFiles, CancellationToken cancellationToken)
        {
            foreach (var formFile in formFiles)
            {
                await _storageService.SaveOrOverwriteFileAsync(formFile.FileName, formFile.OpenReadStream(), cancellationToken);
            }
            return Ok();
        }

        [HttpPost("upload")]
        public async Task<ActionResult> UploadMultipleFiles([FromForm] IFormFile formFile, CancellationToken cancellationToken)
        {
            await _storageService.SaveOrOverwriteFileAsync(formFile.FileName, formFile.OpenReadStream(), cancellationToken);

            return Ok();
        }

        [HttpGet("list")]
        public async Task<IEnumerable<FileMetadataViewModel>> ListFiles(CancellationToken cancellationToken)
        {
            return await _storageService.ListFilesAsync(cancellationToken);
        }

        [HttpGet("deletehistory")]
        public async Task<IEnumerable<FileHistoryEntry>> GetFileHistory([FromQuery] string path, CancellationToken cancellationToken)
        {
            return await _storageService.GetHistoryAsync(path, DAL.Enums.FileAction.Delete, cancellationToken);
        }

    }
}
