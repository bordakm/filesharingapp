using FileHostingAppDesktopClient.Domain;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace FileHostingAppDesktopClient.Services
{
    class SyncService
    {
        HttpClient _httpClient;
        //private static string cloudBaseAddress = "http://testwebapp1147546458495166.azurewebsites.net/";
        private static string cloudBaseAddress = "http://localhost:30001/";
        private static string localRootPath = @"C:/Users/Mate/Desktop/tmp/filehosting/";
        //private static string localRootPath = "./filehostingfiles";
        HashingService _hashingService;
        public SyncService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(cloudBaseAddress);
            _hashingService = new HashingService();
        }

        public async Task SyncAsync()
        {
            var listResponse = await _httpClient.GetAsync("files/list");
            var str = await listResponse.Content.ReadAsStringAsync();
            var remoteFileDatas = JsonConvert.DeserializeObject<IEnumerable<FileMetadata>>(str);
            Debug.WriteLine("res: " + str);
            var localFileDatas = new List<FileMetadata>();
            DirectoryInfo di = new DirectoryInfo(localRootPath);
            var localFiles = Directory.GetFiles(localRootPath, "*", new EnumerationOptions { RecurseSubdirectories = true }).Select(x => x.Replace("\\", "/"));

            foreach (var localFilePath in localFiles)
            {
                var file = File.OpenRead(localFilePath);
                var hash = _hashingService.MD5FromStream(file);
                var relativeFilePath = localFilePath.Substring(localRootPath.Length);
                var fileName = relativeFilePath.Split("/").Last();
                localFileDatas.Add(new FileMetadata
                {
                    RelativeFileLocation = relativeFilePath.Replace(fileName, ""),
                    FileName = fileName,
                    Hash = hash,
                    LastModified = System.IO.File.GetLastWriteTimeUtc(localFilePath)
                });
            }


            var filesToUpload = new List<FileMetadata>();
            var filesToDownload = new List<FileMetadata>();
            var filesToDeleteLocally = new List<FileMetadata>();
            var filesToDeleteRemotely = new List<FileMetadata>();

            filesToUpload = localFileDatas.Where(x => !remoteFileDatas.Any(y => y.FileName == x.FileName && x.Hash == y.Hash)).ToList();
            filesToDownload = remoteFileDatas.Where(x => !localFileDatas.Any(y => y.FileName == x.FileName && x.Hash == y.Hash)).ToList();

            foreach (var fileMetadata in filesToUpload)
            {
                var form = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(localRootPath + fileMetadata.RelativePathWithFilename));
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                form.Add(fileContent, "formFile", fileMetadata.RelativePathWithFilename);

                var response = await _httpClient.PostAsync("files/upload", form);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
            }

            foreach (var fileMetadata in filesToDownload)
            {
                UriBuilder builder = new UriBuilder(_httpClient.BaseAddress + "files/download");
                builder.Query = "filePath=" + fileMetadata.RelativePathWithFilename;
                var response = await _httpClient.GetAsync(builder.Uri);


                Directory.CreateDirectory(localRootPath + fileMetadata.RelativeFileLocation);


                using (var fs = new FileStream(localRootPath + fileMetadata.RelativePathWithFilename, FileMode.Create))
                {
                    await response.Content.CopyToAsync(fs);
                }

                //using (var fs = new FileStream(localRootPath + "/" + fileMetadata.FileName, FileMode.CreateNew))
                //{
                //    await response.Content.CopyToAsync(fs);
                //}
            }
        }



        private byte[] StreamToByteArray(Stream stream)
        {
            int length = Convert.ToInt32(stream.Length);
            byte[] data = new byte[length];
            stream.Read(data, 0, length);
            stream.Close();
            return data;
        }
    }
}
