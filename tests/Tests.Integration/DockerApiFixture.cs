using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;
using DotNet.Testcontainers.Configurations;
using Testcontainers.PostgreSql;
using Xunit;

namespace Tests.Integration;

public class DockerApiFixture : IAsyncLifetime
{
    private INetwork _network = null!;
    private PostgreSqlContainer _dbContainer = null!;
    private IContainer _apiContainer = null!;
    private IFutureDockerImage _apiImage = null!;

    public string BaseUrl { get; private set; } = string.Empty;
    public INetwork Network => _network;

    public async Task InitializeAsync()
    {
        _network = new NetworkBuilder()
            .WithName($"garden-network-{Guid.NewGuid():N}")
            .Build();
        await _network.CreateAsync();

        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("garden_planner")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithNetwork(_network)
            .WithNetworkAliases("db")
            .Build();
        await _dbContainer.StartAsync();

        var apiDbConnectionString = "Host=db;Port=5432;Database=garden_planner;Username=postgres;Password=postgres";

        // Find root directory by looking for the .sln file
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDir != null && !currentDir.GetFiles("*.sln").Any())
        {
            currentDir = currentDir.Parent;
        }
        var rootDir = currentDir?.FullName ?? throw new Exception("Could not find solution root directory");
        
        _apiImage = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(Path.Combine(rootDir, "src"))
            .WithDockerfile("Api/Dockerfile")
            .WithName("garden-api-test:latest")
            .WithCleanUp(true)
            .Build();
        await _apiImage.CreateAsync();

        _apiContainer = new ContainerBuilder()
            .WithImage(_apiImage.FullName)
            .WithNetwork(_network)
            .WithNetworkAliases("api")
            .WithPortBinding(8080, true)
            .DependsOn(_dbContainer)
            .WithEnvironment("ConnectionStrings__DefaultConnection", apiDbConnectionString)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("ASPNETCORE_URLS", "http://+:8080")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilMessageIsLogged("Now listening on: http://.*:8080"))
            .Build();
        await _apiContainer.StartAsync();

        var host = _apiContainer.Hostname;
        var port = _apiContainer.GetMappedPublicPort(8080);
        BaseUrl = $"http://{host}:{port}";
    }

    public async Task DisposeAsync()
    {
        if (_apiContainer != null)
            await _apiContainer.DisposeAsync();

        if (_dbContainer != null)
            await _dbContainer.DisposeAsync();

        if (_network != null)
            await _network.DeleteAsync();
    }
}
