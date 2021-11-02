using AutoMapper;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FileHostingApp.BLL.DTOs.ViewModels;
using FileHostingApp.BLL.Interfaces;
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

        public AzureBlobStorageService(
            BlobServiceClient blobServiceClient,
            IHashingService hashingService,
            IMapper mapper)
        {
            _blobServiceClient = blobServiceClient;
            _hashingService = hashingService;
            _mapper = mapper;
        }

        public async Task SaveOrOverwriteFileAsync(string fileName, Stream file, CancellationToken cancellationToken)
        {
            var container = GetBlobContainer("");
            var blob = container.GetBlobClient(fileName);
            var hash = _hashingService.MD5FromStream(file);
            await blob.UploadAsync(file, true, cancellationToken);
            await blob.SetTagsAsync(new Dictionary<string, string>() {
                { "hash", hash },
                { "lastModified", DateTime.Now.ToString() }
            });
        }

        public async Task DeleteFileAsync(string fileName, CancellationToken cancellationToken)
        {
            var container = GetBlobContainer(string.Join('/', fileName.Split("/").SkipLast(1)));
            var blob = container.GetBlobClient(fileName.Split("/").Last());
            await blob.DeleteAsync(cancellationToken: cancellationToken);
        }

        public async Task<Stream> DownloadFileAsync(string filePath, CancellationToken cancellationToken)
        {
            var container = GetBlobContainer(string.Join('/', filePath.Split("/").SkipLast(1)) + "/");
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
                //fileNames.Add(asyncEnumerator.Current.Name);
                fileDatas.Add(_mapper.Map<FileMetadataViewModel>(asyncEnumerator.Current));
            }
            return fileDatas;
        }

        private BlobContainerClient GetBlobContainer(string folderPath)
        {
            // TODO: auth alapján container választás?
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient("my-container/" + folderPath);
            return containerClient;
        }
    }
}
