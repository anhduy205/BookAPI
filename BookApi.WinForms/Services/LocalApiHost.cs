using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;

namespace BookApi.WinForms.Services;

public sealed class LocalApiHost : IDisposable
{
    private static readonly Uri HealthUri = new("http://localhost:9999/health");
    private readonly HttpClient _httpClient;
    private readonly StringBuilder _outputBuffer = new();
    private Process? _launchedProcess;

    public LocalApiHost()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2)
        };
    }

    public async Task EnsureAvailableAsync(CancellationToken cancellationToken = default)
    {
        if (await IsHealthyAsync(cancellationToken))
        {
            return;
        }

        if (_launchedProcess is null || _launchedProcess.HasExited)
        {
            _launchedProcess = StartApiProcess();
        }

        var started = await WaitForHealthyAsync(TimeSpan.FromSeconds(20), cancellationToken);
        if (started)
        {
            return;
        }

        throw new InvalidOperationException(
            "Book API did not start in time. Check SQL Server and try again."
            + Environment.NewLine
            + GetOutputSnapshot());
    }

    public void Dispose()
    {
        _httpClient.Dispose();

        if (_launchedProcess is null || _launchedProcess.HasExited)
        {
            return;
        }

        try
        {
            _launchedProcess.Kill(entireProcessTree: true);
            _launchedProcess.WaitForExit(3000);
        }
        catch
        {
            // Ignore cleanup failures when the form closes.
        }
        finally
        {
            _launchedProcess.Dispose();
            _launchedProcess = null;
        }
    }

    private async Task<bool> WaitForHealthyAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var startedAt = DateTime.UtcNow;

        while (DateTime.UtcNow - startedAt < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_launchedProcess is not null && _launchedProcess.HasExited)
            {
                return false;
            }

            if (await IsHealthyAsync(cancellationToken))
            {
                return true;
            }

            await Task.Delay(500, cancellationToken);
        }

        return false;
    }

    private async Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<HealthResponse>(HealthUri, cancellationToken);
            return string.Equals(response?.Status, "Healthy", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private Process StartApiProcess()
    {
        var startInfo = BuildStartInfo();
        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (_, args) => AppendOutput(args.Data);
        process.ErrorDataReceived += (_, args) => AppendOutput(args.Data);

        if (!process.Start())
        {
            throw new InvalidOperationException("Unable to start the Book API process.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private ProcessStartInfo BuildStartInfo()
    {
        var currentConfiguration = GetCurrentConfigurationName();
        var apiProjectDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "BookApi.Api"));
        var apiOutputDirectory = Path.Combine(apiProjectDirectory, "bin", currentConfiguration, "net8.0");
        var apiExecutablePath = Path.Combine(apiOutputDirectory, "BookApi.Api.exe");

        if (File.Exists(apiExecutablePath))
        {
            return new ProcessStartInfo
            {
                FileName = apiExecutablePath,
                WorkingDirectory = apiOutputDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
        }

        var apiProjectPath = Path.Combine(apiProjectDirectory, "BookApi.Api.csproj");
        if (File.Exists(apiProjectPath))
        {
            return new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{apiProjectPath}\"",
                WorkingDirectory = apiProjectDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
        }

        throw new InvalidOperationException("Cannot locate BookApi.Api to start it automatically.");
    }

    private string GetOutputSnapshot()
    {
        lock (_outputBuffer)
        {
            return _outputBuffer.Length == 0
                ? "No startup output was captured."
                : _outputBuffer.ToString();
        }
    }

    private void AppendOutput(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        lock (_outputBuffer)
        {
            if (_outputBuffer.Length > 4000)
            {
                _outputBuffer.Remove(0, _outputBuffer.Length - 4000);
            }

            _outputBuffer.AppendLine(line);
        }
    }

    private static string GetCurrentConfigurationName()
    {
        var baseDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        return baseDirectory.Parent?.Name ?? "Debug";
    }

    private sealed class HealthResponse
    {
        public string? Status { get; init; }
    }
}
