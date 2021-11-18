using AutoMapper;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FileHostingApp.BLL.DTOs.ViewModels;
using FileHostingApp.BLL.Interfaces;
using FileHostingApp.DAL.DbContexts;
using FileHostingApp.DAL.Entities;
using FileHostingApp.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileHostingApp.BLL.Services
{
    public class AzureBlobStorageService : IStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly IHashingService _hashingService;
        private readonly IMapper _mapper;
        private readonly FileHostingDbContext _dbContext;
        private readonly IIdentityService _identityService;

        public AzureBlobStorageService(
            BlobServiceClient blobServiceClient,
            IHashingService hashingService,
            IMapper mapper,
            FileHostingDbContext dbContext,
            IIdentityService identityService)
        {
            _blobServiceClient = blobServiceClient;
            _hashingService = hashingService;
            _mapper = mapper;
            _dbContext = dbContext;
            _identityService = identityService;
        }

        public async Task SaveOrOverwriteFileAsync(string fileName, Stream file, CancellationToken cancellationToken)
        {
            var container = GetBlobContainer("");
            var blob = container.GetBlobClient(fileName);
            var hash = _hashingService.MD5FromStream(file);

            await _dbContext.History.AddAsync(new DAL.Entities.FileHistoryEntry
            {
                Action = DAL.Enums.FileAction.Edit,
                Hash = hash,
                Path = fileName,
                Timestamp = DateTime.UtcNow
            }, cancellationToken);

            await blob.UploadAsync(file, true, cancellationToken);
            await blob.SetTagsAsync(new Dictionary<string, string>() {
                { "hash", hash },
                { "lastModified", DateTime.UtcNow.ToString() }
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteFileAsync(string fileName, CancellationToken cancellationToken)
        {
            await _dbContext.History.AddAsync(new DAL.Entities.FileHistoryEntry
            {
                Action = DAL.Enums.FileAction.Delete,
                Path = fileName,
                Timestamp = DateTime.UtcNow
            }, cancellationToken);

            var container = GetBlobContainer("");
            var blob = container.GetBlobClient(fileName);
            await blob.DeleteAsync(cancellationToken: cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<Stream> DownloadFileAsync(string filePath, CancellationToken cancellationToken)
        {
            //var container = GetBlobContainer(string.Join('/', filePath.Split("/").SkipLast(1)) + "/");
            var container = GetBlobContainer("");
            var blob = container.GetBlobClient(filePath);
            return (await blob.DownloadContentAsync(cancellationToken)).Value.Content.ToStream();
        }

        public async Task<IEnumerable<FileMetadataViewModel>> ListFilesAsync(CancellationToken cancellationToken)
        {
            var container = GetBlobContainer("");
            ICollection<FileMetadataViewModel> fileDatas = new List<FileMetadataViewModel>();
            var asyncEnumerator = container.GetBlobsAsync(BlobTraits.Tags, cancellationToken: cancellationToken).GetAsyncEnumerator(cancellationToken);
            while (await asyncEnumerator.MoveNextAsync())
            {
                fileDatas.Add(_mapper.Map<FileMetadataViewModel>(asyncEnumerator.Current));
            }
            return fileDatas;
        }

        public async Task<IEnumerable<FileHistoryEntry>> GetHistoryAsync(string filterFilename, FileAction? filterAction, CancellationToken cancellationToken)
        {
            return await _dbContext.History
                .Where(x => filterFilename == null ? true : x.Path == filterFilename)
                .Where(x => filterAction == null ? true : x.Action == filterAction)
                .ToListAsync(cancellationToken);
        }

        private BlobContainerClient GetBlobContainer(string folderPath)
        {
            string userId = _identityService.GetCurrentUserId();
            if (userId == null) throw new Exception("User not found!");
            var containerPath = userId + "/" + folderPath;
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerPath);
            bool isExist = containerClient.Exists();
            if (!isExist)
            {
                containerClient.Create();
            }
            return containerClient;
        }
    }
}
