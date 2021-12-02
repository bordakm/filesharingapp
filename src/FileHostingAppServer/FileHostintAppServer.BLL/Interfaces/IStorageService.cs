using FileHostingAppServer.BLL.DTOs.ViewModels;
using FileHostingAppServer.DAL.Entities;
using FileHostingAppServer.DAL.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileHostingAppServer.BLL.Interfaces
{
    public interface IStorageService
    {
        Task DeleteFileAsync(string fileName, CancellationToken cancellationToken);
        Task<Stream> DownloadFileAsync(string filePath, CancellationToken cancellationToken);
        Task<IEnumerable<FileHistoryEntry>> GetHistoryAsync(string filterFilename, FileAction? filterAction, CancellationToken cancellationToken);
        Task<IEnumerable<FileMetadataViewModel>> ListFilesAsync(CancellationToken cancellationToken);
        Task SaveOrOverwriteFileAsync(string fileName, Stream file, CancellationToken cancellationToken);
    }
}
