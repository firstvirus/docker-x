using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using DockerX.Core.Model;

namespace DockerX.Core.Interfaces;

public interface IContainerRepository
{
    // Основные операции
    Task<List<Container>> GetAllContainersAsync();
    Task<Container> GetContainerByIdAsync(string id);
    Task<bool> StartContainerAsync(string id);
    Task<bool> StopContainerAsync(string id);
    Task<bool> RestartContainerAsync(string id);
    Task RenameContainerAsync(string id, ContainerRenameParameters parameters, CancellationToken ct);
    Task PauseContainerAsync(string id, CancellationToken ct = default);
    Task UnpauseContainerAsync(string id, CancellationToken ct = default);
    Task RemoveContainerAsync(string id, ContainerRemoveParameters parameters, CancellationToken ct = default);
    Task<ContainerInspectResponse> InspectContainerAsync(string id, CancellationToken ct = default);
    
    // Потоковые операции
    Task<MultiplexedStream> AttachContainerAsync(string id, bool tty, ContainerAttachParameters parameters, CancellationToken ct = default);
    Task GetContainerLogsAsync(string id, ContainerLogsParameters parameters, CancellationToken ct, IProgress<string> progress);
    Task GetContainerStatsAsync(string id, ContainerStatsParameters parameters, IProgress<ContainerStatsResponse> progress, CancellationToken ct = default);
    IAsyncEnumerable<string> StreamContainerLogsAsync(
        string containerId, 
        bool includeStdout = true,
        bool includeStderr = true,
        bool includeTimestamps = false,
        CancellationToken cancellationToken = default);
    
    // Работа с файлами
    Task<GetArchiveFromContainerResponse> GetArchiveFromContainerAsync(string id, GetArchiveFromContainerParameters parameters, bool statOnly, CancellationToken ct = default);
    Task ExtractArchiveToContainerAsync(string id, ContainerPathStatParameters parameters, Stream stream, CancellationToken ct = default);
    Task<Stream> ExportContainerAsync(string id, CancellationToken ct = default);
    
    // Системные операции
    Task<ContainersPruneResponse> PruneContainersAsync(ContainersPruneParameters? parameters = null, CancellationToken ct = default);
    Task<ContainerUpdateResponse> UpdateContainerAsync(string id, ContainerUpdateParameters parameters, CancellationToken ct = default);
}