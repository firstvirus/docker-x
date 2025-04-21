using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using DockerX.Core.Interfaces;
using DockerX.Core.Model;

namespace DockerX.Infrastructure.Repositories;

public class DockerContainerRepository(IDockerClient dockerClient) : IContainerRepository
{
    private readonly IDockerClient _dockerClient = dockerClient;

    // Основные операции
    public async Task<List<Container>> GetAllContainersAsync()
    {
        IList<ContainerListResponse> containers = await _dockerClient.Containers.ListContainersAsync(
            new () { All = true }
        );

        return containers.Select(c => new Container()
            {
                Id = c.ID,
                Image = c.Image,
                Command = c.Command,
                Created = c.Created,
                Status = c.Status,
                Ports = c.Ports.Select(p => $"{p.PublicPort}:{p.PrivatePort}/{p.Type}").ToList()
            })
            .ToList();
    }

    public async Task<Container> GetContainerByIdAsync(string id)
    {
        ContainerInspectResponse container = await _dockerClient.Containers.InspectContainerAsync(id);

        return new Container()
        {
            Id = container.ID,
            Image = container.Config.Image,
            Command = string.Join("\n", container.Config.Cmd),
            Created = container.Created,
            Status = container.State.Status,
            Ports = container.NetworkSettings.Ports.Select(p => $"{p.Key}").ToList()
        };
    }

    public async Task<bool> StartContainerAsync(string id)
    {
        bool result = await _dockerClient.Containers.StartContainerAsync(
            id, new ContainerStartParameters());
        return result;
    }

    public async Task<bool> StopContainerAsync(string id)
    {
        bool result = await _dockerClient.Containers.StopContainerAsync(
            id, new ContainerStopParameters { WaitBeforeKillSeconds = 10 });
        return result;
    }
    
    public async Task<bool> RestartContainerAsync(string id)
    {
        await _dockerClient.Containers.RestartContainerAsync(
            id, new ContainerRestartParameters { WaitBeforeKillSeconds = 10 });
        return true;
    }

    public Task RenameContainerAsync(string id, ContainerRenameParameters parameters, CancellationToken ct) 
        => _dockerClient.Containers.RenameContainerAsync(id, parameters, ct);

    public Task PauseContainerAsync(string id, CancellationToken ct) 
        => _dockerClient.Containers.PauseContainerAsync(id, ct);

    public Task UnpauseContainerAsync(string id, CancellationToken ct) 
        => _dockerClient.Containers.UnpauseContainerAsync(id, ct);

    public Task RemoveContainerAsync(string id, ContainerRemoveParameters parameters, CancellationToken ct) 
        => _dockerClient.Containers.RemoveContainerAsync(id, parameters, ct);

    public Task<ContainerInspectResponse> InspectContainerAsync(string id, CancellationToken ct) 
        => _dockerClient.Containers.InspectContainerAsync(id, ct);

    // Потоковые операции
    public async IAsyncEnumerable<string> StreamContainerLogsAsync(
        string containerId,
        bool includeStdout = true,
        bool includeStderr = true,
        bool includeTimestamps = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ContainerLogsParameters parameters = new()
        {
            ShowStdout = includeStdout,
            ShowStderr = includeStderr,
            Timestamps = includeTimestamps,
            Follow = true // Важно для потокового вывода!
        };

        using MultiplexedStream stream = await _dockerClient.Containers.GetContainerLogsAsync(
            containerId,
            false,
            parameters,
            cancellationToken);

        // Буфер для чтения заголовка (8 байт)
        byte[] headerBuffer = new byte[8];

        while (!cancellationToken.IsCancellationRequested)
        {
            // Читаем заголовок
            MultiplexedStream.ReadResult headerBytes = await stream.ReadOutputAsync(headerBuffer, 0, 8, cancellationToken);
            if (headerBytes.EOF) break; // Поток завершен

            // Разбираем заголовок:
            // - 1-й байт: тип потока (0 = stdin, 1 = stdout, 2 = stderr)
            // - 4-8 байты: размер чанка (big-endian int32)
            byte streamType = headerBuffer[0];
            int chunkSize = BitConverter.ToInt32(headerBuffer, 4);

            // Пропускаем, если тип потока не нужен
            if ((streamType == 1 && !includeStdout) || (streamType == 2 && !includeStderr))
            {
                await SkipAsync(stream, chunkSize, cancellationToken);
                continue;
            }

            // Читаем данные чанка
            byte[] chunkBuffer = new byte[chunkSize];
            MultiplexedStream.ReadResult bytes = await stream.ReadOutputAsync(chunkBuffer, 0, chunkSize, cancellationToken);
            if (bytes.EOF) break;

            // Преобразуем в строку (UTF-8)
            string chunkText = Encoding.UTF8.GetString(chunkBuffer, 0, bytes.Count);
            yield return chunkText;
        }
    }
    
    public Task<MultiplexedStream> AttachContainerAsync(string id, bool tty, ContainerAttachParameters parameters, CancellationToken ct) 
        => _dockerClient.Containers.AttachContainerAsync(id, tty, parameters, ct);

    public Task GetContainerLogsAsync(string id, ContainerLogsParameters parameters, CancellationToken ct, IProgress<string> progress)
        => _dockerClient.Containers.GetContainerLogsAsync(id, parameters, ct, progress);

    public Task GetContainerStatsAsync(string id, ContainerStatsParameters parameters, IProgress<ContainerStatsResponse> progress, CancellationToken ct) 
        => _dockerClient.Containers.GetContainerStatsAsync(id, parameters, progress, ct);

    // Работа с файлами
    public Task<GetArchiveFromContainerResponse> GetArchiveFromContainerAsync(string id, GetArchiveFromContainerParameters parameters, bool statOnly, CancellationToken ct) 
        => _dockerClient.Containers.GetArchiveFromContainerAsync(id, parameters, statOnly, ct);

    public Task ExtractArchiveToContainerAsync(string id, ContainerPathStatParameters parameters, Stream stream, CancellationToken ct) 
        => _dockerClient.Containers.ExtractArchiveToContainerAsync(id, parameters, stream, ct);

    public Task<Stream> ExportContainerAsync(string id, CancellationToken ct) 
        => _dockerClient.Containers.ExportContainerAsync(id, ct);

    // Системные операции
    public Task<ContainersPruneResponse> PruneContainersAsync(ContainersPruneParameters? parameters, CancellationToken ct) 
        => _dockerClient.Containers.PruneContainersAsync(parameters, ct);

    public Task<ContainerUpdateResponse> UpdateContainerAsync(string id, ContainerUpdateParameters parameters, CancellationToken ct) 
        => _dockerClient.Containers.UpdateContainerAsync(id, parameters, ct);
    
    // Вспомогательный метод для пропуска чтения байт в потоке
    private static async Task SkipAsync(MultiplexedStream stream, int bytesToSkip, CancellationToken ct)
    {
        byte[] buffer = new byte[4096];
        while (bytesToSkip > 0 && !ct.IsCancellationRequested)
        {
            MultiplexedStream.ReadResult bytes = await stream.ReadOutputAsync(
                buffer, 
                0, 
                Math.Min(buffer.Length, bytesToSkip), 
                ct);
        
            if (bytes.EOF) break;
            bytesToSkip -= bytes.Count;
        }
    }

}