using System.Threading.Tasks;
using DockerX.Application.Extensions;
using DockerX.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DockerX.Tests;

public class DockerContainerRepositoryTests
{
    private readonly IContainerRepository _repository;

    public DockerContainerRepositoryTests()
    {
        ServiceCollection services = new();
        services.AddInfrastructure();
        ServiceProvider provider = services.BuildServiceProvider();
        _repository = provider.GetRequiredService<IContainerRepository>();
    }

    [Fact]
    public async Task GetAllContainersAsync_ReturnsAllContainers()
    {
        var containers = await _repository.GetAllContainersAsync();
        Assert.NotNull(containers);
        Assert.NotEmpty(containers);
    }
}