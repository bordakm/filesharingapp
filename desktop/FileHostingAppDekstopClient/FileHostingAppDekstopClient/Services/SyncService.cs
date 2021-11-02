using FileHostingAppDekstopClient.Domain;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FileHostingAppDekstopClient.Services
{
    class SyncService
    {
        HttpClient _httpClient;
        private static string cloudBaseAddress = "http://testwebapp1147546458495166.azurewebsites.net/";
        //private static string cloudBaseAddress = "http://localhost:1155/";
        private static string localRootPath = @"C:\Users\Mate\Desktop\tmp\filehosting\";
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
            var localFiles = Directory.GetFiles(localRootPath);

            foreach (var filePath in localFiles)
            {
                var file = File.OpenRead(filePath);

                var hash = _hashingService.MD5FromStream(file);
                var fileName = filePath.Split("/").Last();
                localFileDatas.Add(new FileMetadata
                {
                    FullFilePath = fileName,
                    FileName = fileName.Replace("\\", "/").Replace("filehostingfiles/", ""),
                    Hash = hash,
                    LastModified = System.IO.File.GetLastWriteTimeUtc(filePath)
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
                var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(fileMetadata.FullFilePath));
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                form.Add(fileContent, "formFile", fileMetadata.FileName);

                var response = await _httpClient.PostAsync("files/upload", form);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
            }

            Windows.Storage.StorageFolder storageFolder =
                Windows.Storage.ApplicationData.Current.LocalFolder;


            foreach (var fileMetadata in filesToDownload)
            {
                UriBuilder builder = new UriBuilder(_httpClient.BaseAddress + "files/download");
                builder.Query = "filePath=" + fileMetadata.FileName;
                var response = await _httpClient.GetAsync(builder.Uri);

                var splitted = fileMetadata.FileName.Split("/");
                if (splitted.Length > 1) continue;

                var currentPath = localRootPath;
                //for (var i = 0; i < splitted.Length - 1; i++)
                //{
                //    currentPath += "/" + splitted[i];
                //    Directory.CreateDirectory(currentPath);
                //}


                using (var fs = new FileStream(storageFolder.Path+"\\"+ fileMetadata.FileName, FileMode.CreateNew))
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
