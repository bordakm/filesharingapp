using AutoMapper;
using Azure.Storage.Blobs.Models;
using FileHostingApp.BLL.DTOs.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileHostingApp.BLL.MappingProfiles
{
    public class FileMappingProfile : Profile
    {
        public FileMappingProfile()
        {
            CreateMap<BlobItem, FileMetadataViewModel>()
                .ForMember(d => d.RelativePathWithFilename, o => o.MapFrom(s => s.Name))
                .ForMember(d => d.Hash, o => o.MapFrom(s => s.Tags.FirstOrDefault(x => x.Key == "hash").Value))
                .ForMember(d => d.LastModified, o => o.MapFrom(s => DateTime.Parse(s.Tags.FirstOrDefault(x => x.Key == "lastModified").Value)));
        }
    }
}
