using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ConsoleApp
{
    public class LocalStackContainer : IAsyncDisposable
    {
        private readonly DockerClient _dockerClient;
        private const string _imageName = "localstack/localstack:0.11.0";
        private string _containerId;
        private const string _containerName = "localstack-container";

        public LocalStackContainer() => _dockerClient = new DockerClientConfiguration(new Uri(GetDockerUri())).CreateClient();

        public async Task RunContainer()
        {
            try
            {
                await CleanUpContainer();

                await GetImage();

                await StartContainer();
            }
            catch (Exception ex)
            {
                await CleanUpContainer();
                throw new Exception(ex.Message);
            }
        }

        private async Task GetImage()
        {
            await _dockerClient.Images
                .CreateImageAsync(new ImagesCreateParameters
                {
                    FromImage = _imageName
                },
                new AuthConfig(),
                new Progress<JSONMessage>());
        }

        private async Task StartContainer()
        {
            var response = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Name = _containerName,
                Image = _imageName,
                ExposedPorts = new Dictionary<string, EmptyStruct>
                {
                    { "4572", default},
                    { "8081", default},
                },

                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        {"4572", new List<PortBinding> {new PortBinding { HostPort = "4572" } }},
                        {"8081", new List<PortBinding> {new PortBinding { HostPort = "8081" } }}
                    }
                },
                Env = new List<string> { "SERVICES=s3:4572", "PORT_WEB_UI=8081" }
            });

            _containerId = response.ID;
            Console.WriteLine("Starting up the localstack container");
            await _dockerClient.Containers.StartContainerAsync(_containerId, null);
            Console.WriteLine("LocalStack is running");
        }

        private async Task CleanUpContainer()
        {
            var containertToDelete = (await _dockerClient.Containers
                                        .ListContainersAsync(new ContainersListParameters { All = true }))
                                        .Where(x => x.Names.Any(n => n.Contains(_containerName)));

            if (containertToDelete.Any())
            {
                foreach (var item in containertToDelete)
                {
                    if (item.State.Equals("running"))
                        await _dockerClient.Containers.KillContainerAsync(item.ID, new ContainerKillParameters());
                    else
                        await _dockerClient.Containers.StopContainerAsync(item.ID, new ContainerStopParameters());

                    await _dockerClient.Containers.RemoveContainerAsync(item.ID, new ContainerRemoveParameters { Force = true });
                }
            }
        }

        private string GetDockerUri()
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            if (isWindows)
            {
                return "npipe://./pipe/docker_engine";
            }

            var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            if (isLinux)
            {
                return "unix:/var/run/docker.sock";
            }

            throw new Exception("Unable to determine what OS this is running on");
        }

        public async ValueTask DisposeAsync()
        {
            if (_containerId != null)
            {
                Console.WriteLine("Shutdown localstack container");
                await CleanUpContainer();
            }
        }
    }
}