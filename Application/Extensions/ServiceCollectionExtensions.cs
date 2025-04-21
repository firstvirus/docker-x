using Docker.DotNet;
using Microsoft.Extensions.DependencyInjection;
using DockerX.Core.Interfaces;
using DockerX.Infrastructure.Docker;
using DockerX.Infrastructure.Repositories;

namespace DockerX.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IDockerClient>(_ => DockerClientFactory.CreateDockerClient());
        services.AddTransient<IContainerRepository, DockerContainerRepository>();
        return services;
    }
}