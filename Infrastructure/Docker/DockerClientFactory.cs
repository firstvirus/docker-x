using System;
using Docker.DotNet;

namespace DockerX.Infrastructure.Docker;

public static class DockerClientFactory
{
    public static DockerClient CreateDockerClient()
    {
        Uri dockerUri = OperatingSystem.IsWindows()
            ? new ("npipe://./pipe/docker_engine")
            : new ("unix:///var/run/docker.run/");
        
        return new DockerClientConfiguration(dockerUri).CreateClient();
    }
}