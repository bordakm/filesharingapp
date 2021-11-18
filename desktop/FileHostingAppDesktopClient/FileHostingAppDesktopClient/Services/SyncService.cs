using DocumentFormat.OpenXml.ExtendedProperties;
using FileHostingAppDesktopClient.Context;
using FileHostingAppDesktopClient.Domain;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileHostingAppDesktopClient.Services
{
    class SyncService
    {
        HttpClient _httpClient;
        //private string cloudBaseAddress = "http://testwebapp1147546458495166.azurewebsites.net/";
        private string cloudBaseAddress;
        //private string cloudBaseAddress = "http://localhost:30001/";
        public string localRootPath;
        //public string localRootPath = @"C:/Users/Mate/Desktop/tmp/filehosting/";
        HashingService _hashingService;
        public event EventHandler<string> MessageEvent;
        public SyncService(string cloudBaseAddress, string localRootPath, string bearer)
        {
            this.cloudBaseAddress = cloudBaseAddress;
            this.localRootPath = localRootPath;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(cloudBaseAddress);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            _hashingService = new HashingService();
        }
        private void LogEvent(string message)
        {
            MessageEvent(this, message);
        }

        public async Task SyncAsync(CancellationToken cancellationToken = default)
        {
            LogEvent("Starting sync..");
            await AddFileChangesAsync(cancellationToken);

            var remoteFileDatas = await ListFilesFromApiAsync(cancellationToken);
            var remoteDeleteHistoryEntries = await ListDeleteHistoryFromApiAsync(cancellationToken);
            var localDeleteHistoryEntries = await ListLocalDeleteHistoryAsync(cancellationToken);

            var localFileDatas = new List<FileMetadata>();
            DirectoryInfo di = new DirectoryInfo(localRootPath);
            var localFiles = Directory.GetFiles(localRootPath, "*", new EnumerationOptions { RecurseSubdirectories = true }).Select(x => x.Replace("\\", "/"));

            foreach (var localFilePath in localFiles)
            {
                using (var file = File.OpenRead(localFilePath))
                {
                    var hash = _hashingService.MD5FromStream(file);
                    var relativeFilePath = localFilePath.Substring(localRootPath.Length);
                    var fileName = relativeFilePath.Split("/").Last();
                    localFileDatas.Add(new FileMetadata
                    {
                        RelativeFileLocation = relativeFilePath.Replace(fileName, ""),
                        FileName = fileName,
                        Hash = hash,
                        LastModified = new DateTime(Math.Max(File.GetCreationTimeUtc(localFilePath).Ticks, File.GetLastWriteTimeUtc(localFilePath).Ticks))
                    });
                }
            }

            //var filesToUpload = new List<FileMetadata>();
            //var filesToDownload = new List<FileMetadata>();
            //var filesToDeleteLocally = new List<FileMetadata>();
            //var filesToDeleteRemotely = new List<FileMetadata>();


            var localFilesWithSameNameDifferentHash = localFileDatas.Where(x => remoteFileDatas.Select(y => y.FileName).Contains(x.FileName) && remoteFileDatas.First(y => y.FileName == x.FileName).Hash != x.Hash);
            foreach (var localFile in localFilesWithSameNameDifferentHash)
            {
                var remoteFile = remoteFileDatas.First(x => x.FileName == localFile.FileName);
                if (remoteFile.LastModified > localFile.LastModified)
                {
                    // remote is newer, download remote
                    LogEvent($"Downloading file {remoteFile.RelativePathWithFilename}");
                    await FetchFileAsync(remoteFile, cancellationToken);
                }
                else
                {
                    LogEvent($"Uploading file {remoteFile.RelativePathWithFilename}");
                    await PushFileAsync(localFile, cancellationToken);
                }
            }

            var localFilesThatDontExistRemotely = localFileDatas.Where(x => !remoteFileDatas.Select(y => y.FileName).Contains(x.FileName)).ToList();
            foreach (var localFile in localFilesThatDontExistRemotely)
            {
                var recentlyDeletedRemoteHistoryEntry = remoteDeleteHistoryEntries.OrderByDescending(x => x.Timestamp).FirstOrDefault(x => x.Path == localFile.RelativePathWithFilename && x.Timestamp > localFile.LastModified);
                if (recentlyDeletedRemoteHistoryEntry == null)
                {
                    LogEvent($"Uploading file {localFile.RelativePathWithFilename}");
                    await PushFileAsync(localFile, cancellationToken);
                }
                else
                {
                    LogEvent($"Deleting local file {recentlyDeletedRemoteHistoryEntry.Path}");
                    DeleteLocalFile(recentlyDeletedRemoteHistoryEntry.Path);
                }
            }


            var remoteFilesThatDontExistLocally = remoteFileDatas.Where(x => !localFileDatas.Select(y => y.FileName).Contains(x.FileName));
            foreach (var remoteFile in remoteFilesThatDontExistLocally)
            {
                var recentlyDeletedLocalHistoryEntry = localDeleteHistoryEntries.OrderByDescending(x => x.Timestamp).FirstOrDefault(x => x.Path == remoteFile.RelativePathWithFilename && x.Timestamp > remoteFile.LastModified);
                if (recentlyDeletedLocalHistoryEntry == null)
                {
                    LogEvent($"Downloading file {remoteFile.RelativePathWithFilename}");
                    await FetchFileAsync(remoteFile, cancellationToken);
                }
                else
                {
                    // delete remote
                    LogEvent($"Deleting remote file {remoteFile.RelativePathWithFilename}");
                    await DeleteRemoteFileAsync(remoteFile.RelativePathWithFilename, cancellationToken);
                }
            }


            var localFilesAfterSync = Directory.GetFiles(localRootPath, "*", new EnumerationOptions { RecurseSubdirectories = true }).Select(x => x.Replace("\\", "/"));
            using (FileHostingDbContext dbContext = new FileHostingDbContext())
            {
                dbContext.Files.RemoveRange(dbContext.Files);

                foreach (var localFilePath in localFilesAfterSync)
                {
                    using (var file = File.OpenRead(localFilePath))
                    {
                        var hash = _hashingService.MD5FromStream(file);
                        var relativeFilePath = localFilePath.Substring(localRootPath.Length);

                        FileEntry fe = new FileEntry
                        {
                            Path = relativeFilePath,
                            Hash = hash,
                            LastModified = new DateTime(Math.Max(File.GetCreationTimeUtc(localFilePath).Ticks, File.GetLastWriteTimeUtc(localFilePath).Ticks))
                        };

                        await dbContext.Files.AddAsync(fe);
                    }
                }
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await AddFileChangesAsync(cancellationToken);
            LogEvent("Sync finished");
        }


        private async Task FetchFileAsync(FileMetadata fileMetadata, CancellationToken cancellationToken)
        {
            UriBuilder builder = new UriBuilder(_httpClient.BaseAddress + "files/download");
            builder.Query = "filePath=" + fileMetadata.RelativePathWithFilename;
            var response = await _httpClient.GetAsync(builder.Uri);

            Directory.CreateDirectory(localRootPath + fileMetadata.RelativeFileLocation);

            using (var fs = new FileStream(localRootPath + fileMetadata.RelativePathWithFilename, FileMode.Create))
            {
                await response.Content.CopyToAsync(fs);
            }
        }

        private async Task PushFileAsync(FileMetadata fileMetadata, CancellationToken cancellationToken)
        {
            var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(localRootPath + fileMetadata.RelativePathWithFilename));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
            form.Add(fileContent, "formFile", fileMetadata.RelativePathWithFilename);

            var response = await _httpClient.PostAsync("files/upload", form);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
        }


        private async Task DeleteRemoteFileAsync(string path, CancellationToken cancellationToken)
        {
            UriBuilder builder = new UriBuilder(_httpClient.BaseAddress + "files");
            builder.Query = "filePath=" + path;
            var response = await _httpClient.DeleteAsync(builder.Uri, cancellationToken);
        }

        private void DeleteLocalFile(string path)
        {
            string pathtodelete = localRootPath + path;
            Debug.WriteLine("Deleting:" + pathtodelete);
            File.Delete(pathtodelete);
        }

        private async Task AddFileChangesAsync(CancellationToken cancellationToken)
        {
            var localFiles = Directory.GetFiles(localRootPath, "*", new EnumerationOptions { RecurseSubdirectories = true }).Select(x => x.Replace("\\", "/"));
            using (FileHostingDbContext dbContext = new FileHostingDbContext())
            {
                foreach (var localFilePath in localFiles)
                {
                    using (var file = File.OpenRead(localFilePath))
                    {
                        var hash = _hashingService.MD5FromStream(file);
                        var relativeFilePath = localFilePath.Substring(localRootPath.Length);
                        var dbFile = await dbContext.Files.FirstOrDefaultAsync(x => x.Path == relativeFilePath, cancellationToken);

                        if (dbFile == null || dbFile.Hash != hash)
                        {
                            FileHistoryEntry fhe = new FileHistoryEntry
                            {
                                Action = Enums.FileAction.Edit,
                                Hash = hash,
                                Path = relativeFilePath,
                                Timestamp = new DateTime(Math.Max(File.GetCreationTimeUtc(localFilePath).Ticks, File.GetLastWriteTimeUtc(localFilePath).Ticks))
                            };
                            await dbContext.History.AddAsync(fhe);
                        }
                    }
                }

                var dbFiles = await dbContext.Files.ToListAsync(cancellationToken);
                var oldExistingFiles = dbFiles.Select(x => x.Path);
                var localFileRelativePaths = localFiles.Select(x => x.Substring(localRootPath.Length));
                var deletedFilePaths = oldExistingFiles.Where(x => !localFileRelativePaths.Contains(x));

                foreach (var deletedFilePath in deletedFilePaths)
                {
                    FileHistoryEntry fhe = new FileHistoryEntry
                    {
                        Action = Enums.FileAction.Delete,
                        Path = deletedFilePath,
                        Timestamp = DateTime.UtcNow
                    };
                    await dbContext.History.AddAsync(fhe);
                }

                // újraírni aktuális snapshotot?
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }


        private async Task<IEnumerable<FileHistoryEntry>> ListLocalDeleteHistoryAsync(CancellationToken cancellationToken)
        {
            using (FileHostingDbContext dbContext = new FileHostingDbContext())
            {
                return await dbContext.History.Where(x => x.Action == Enums.FileAction.Delete).ToListAsync(cancellationToken);
            }
        }


        private async Task<IEnumerable<FileMetadata>> ListFilesFromApiAsync(CancellationToken cancellationToken)
        {
            var listResponse = await _httpClient.GetAsync("files/list");
            var str = await listResponse.Content.ReadAsStringAsync();
            Debug.WriteLine("res: " + str);
            var remoteFileDatas = JsonConvert.DeserializeObject<IEnumerable<FileMetadata>>(str);
            return remoteFileDatas;
        }

        private async Task<IEnumerable<FileHistoryEntry>> ListDeleteHistoryFromApiAsync(CancellationToken cancellationToken)
        {
            var listResponse = await _httpClient.GetAsync("files/deletehistory");
            var str = await listResponse.Content.ReadAsStringAsync();
            Debug.WriteLine("res: " + str);
            var historyEntries = JsonConvert.DeserializeObject<IEnumerable<FileHistoryEntry>>(str);
            return historyEntries;
        }

        private byte[] StreamToByteArray(Stream stream)
        {
            int length = Convert.ToInt32(stream.Length);
            byte[] data = new byte[length];
            stream.Read(data, 0, length);
            stream.Close();
            return data;
        }

        public class MessageEventArgs : EventArgs
        {
            public MessageEventArgs(string message)
            {
                Message = message;
            }

            public string Message { get; set; }
        }

        public async Task WipeLocalFileHistory()
        {
            using (FileHostingDbContext dbContext = new FileHostingDbContext())
            {
                dbContext.Files.RemoveRange(dbContext.Files);
                dbContext.History.RemoveRange(dbContext.History);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
