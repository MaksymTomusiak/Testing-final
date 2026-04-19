using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using Tests.Integration;
using Xunit;

namespace Tests.K6;

public class PerformanceTests : IClassFixture<DockerApiFixture>
{
    private readonly DockerApiFixture _fixture;

    public PerformanceTests(DockerApiFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<string> RunK6Test(string scriptName)
    {
        // Find root directory by looking for the .sln file
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDir != null && !currentDir.GetFiles("*.sln").Any())
        {
            currentDir = currentDir.Parent;
        }
        var rootDir = currentDir?.FullName ?? throw new Exception("Could not find solution root directory");
        var scriptDir = Path.Combine(rootDir, "tests", "Tests.K6");
        
        var k6Container = new ContainerBuilder()
            .WithImage("grafana/k6:latest")
            .WithNetwork(_fixture.Network)
            .WithBindMount(scriptDir, "/scripts", AccessMode.ReadWrite)
            .WithEnvironment("BASE_URL", "http://api:8080")
            .WithCommand("run", $"/scripts/{scriptName}")
            .Build();

        try 
        {
            await k6Container.StartAsync();
            
            // Poll for completion (k6 prints http_req_duration at the very end)
            string stdout, stderr;
            var timeout = DateTime.UtcNow.AddMinutes(10);
            
            while (true)
            {
                var logs = await k6Container.GetLogsAsync();
                stdout = logs.Stdout;
                stderr = logs.Stderr;
                
                if (stdout.Contains("http_req_duration") || stderr.Contains("http_req_duration"))
                    break;
                
                if (DateTime.UtcNow > timeout)
                    throw new TimeoutException("k6 test timed out after 10 minutes");
                    
                await Task.Delay(5000);
            }
            
            Console.WriteLine($"k6 STDOUT: {stdout}");
            Console.WriteLine($"k6 STDERR: {stderr}");

            return stdout + stderr;
        }
        finally
        {
            await k6Container.DisposeAsync();
        }
    }

    [Fact]
    public async Task SmokeTest()
    {
        var output = await RunK6Test("smoke.js");
        Assert.Contains("http_req_duration", output);
    }

    [Fact]
    public async Task LoadTest()
    {
        var output = await RunK6Test("load_test.js");
        Assert.Contains("http_req_duration", output);
    }

    [Fact]
    public async Task StressTest()
    {
        var output = await RunK6Test("stress.js");
        Assert.Contains("http_req_duration", output);
    }
}
