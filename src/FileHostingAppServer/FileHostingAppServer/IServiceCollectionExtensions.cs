using AutoMapper;
using FileHostingAppServer.BLL.Interfaces;
using FileHostingAppServer.BLL.MappingProfiles;
using FileHostingAppServer.BLL.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileHostingAppServer
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
