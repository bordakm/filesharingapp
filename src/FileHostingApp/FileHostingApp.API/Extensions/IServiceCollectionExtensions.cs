using AutoMapper;
using FileHostingApp.BLL.Interfaces;
using FileHostingApp.BLL.MappingProfiles;
using FileHostingApp.BLL.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileHostingApp.API.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddDefaultServices(this IServiceCollection serviceCollection)
        {
            return serviceCollection
                .AddScoped<IStorageService, AzureBlobStorageService>()
                .AddScoped<IIdentityService, IdentityService>()
                .AddTransient<IHashingService, HashingService>();
        }
        public static IServiceCollection AddAutoMapperProfiles(this IServiceCollection serviceCollection)
        {
            var mappingConfig = new MapperConfiguration(mc =>
            {
                mc.AddProfile(new FileMappingProfile());
            });

            IMapper mapper = mappingConfig.CreateMapper();
            return serviceCollection.AddSingleton(mapper);
        }
    }
}
