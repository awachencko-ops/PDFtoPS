using System.Diagnostics;
using System.Text;

namespace PDFtoPS;

internal enum GhostscriptErrorCode
{
    None,
    ExecutableNotFound,
    ProcessStartFailed,
    Timeout,
    NonZeroExitCode,
    OutputFileMissing,
    Cancelled,
    Unknown
}

internal sealed class GhostscriptRunResult
{
    public bool Success { get; init; }
    public GhostscriptErrorCode ErrorCode { get; init; } = GhostscriptErrorCode.None;
    public string Message { get; init; } = string.Empty;
    public string StdOut { get; init; } = string.Empty;
    public string StdErr { get; init; } = string.Empty;
    public int? ExitCode { get; init; }
}

internal sealed class GhostscriptRunner
{
    private readonly TimeSpan timeout;
    private readonly int retryCount;
    private readonly int retryDelayMs;
    private readonly AppLogger logger;

    public GhostscriptRunner(TimeSpan timeout, int retryCount, int retryDelayMs, AppLogger logger)
    {
        this.timeout = timeout;
        this.retryCount = retryCount;
        this.retryDelayMs = retryDelayMs;
        this.logger = logger;
    }

    public string ResolveGhostscriptPath()
    {
        string? envPath = Environment.GetEnvironmentVariable("GHOSTSCRIPT_PATH");
        if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath)) return envPath;

        foreach (string candidate in BuildCandidates())
        {
            if (File.Exists(candidate)) return candidate;
        }

        return string.Empty;
    }

    public GhostscriptRunResult RunWithRetry(string gsPath, string arguments, string operation, string? expectedOutputPath = null, CancellationToken cancellationToken = default)
    {
        GhostscriptRunResult lastResult = new() { Success = false, ErrorCode = GhostscriptErrorCode.Unknown };

        for (int attempt = 1; attempt <= retryCount; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lastResult = Run(gsPath, arguments, operation, attempt, expectedOutputPath, cancellationToken);
            if (lastResult.Success)
            {
                return lastResult;
            }

            if (lastResult.ErrorCode == GhostscriptErrorCode.Cancelled)
            {
                return lastResult;
            }

            logger.Warning($"Ghostscript attempt failed: {operation}",
                ("attempt", attempt),
                ("errorCode", lastResult.ErrorCode.ToString()),
                ("message", lastResult.Message));

            if (attempt < retryCount)
            {
                cancellationToken.WaitHandle.WaitOne(retryDelayMs);
            }
        }

        return lastResult;
    }

    private GhostscriptRunResult Run(string gsPath, string arguments, string operation, int attempt, string? expectedOutputPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(gsPath) || !File.Exists(gsPath))
        {
            return new GhostscriptRunResult
            {
                Success = false,
                ErrorCode = GhostscriptErrorCode.ExecutableNotFound,
                Message = "Ghostscript executable not found."
            };
        }

        ProcessStartInfo psi = new()
        {
            FileName = gsPath,
            Arguments = BuildArguments(arguments),
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using Process process = new() { StartInfo = psi };
        bool processStarted = false;
        StringBuilder stdOut = new();
        StringBuilder stdErr = new();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is { Length: > 0 }) stdOut.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is { Length: > 0 }) stdErr.AppendLine(e.Data);
        };

        try
        {
            logger.Info("Ghostscript started",
                ("operation", operation),
                ("attempt", attempt),
                ("command", $"\"{gsPath}\" {psi.Arguments}"));

            if (!process.Start())
            {
                return new GhostscriptRunResult
                {
                    Success = false,
                    ErrorCode = GhostscriptErrorCode.ProcessStartFailed,
                    Message = "Failed to start Ghostscript process."
                };
            }

            processStarted = true;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            DateTime deadline = DateTime.UtcNow + timeout;
            while (!process.WaitForExit(200))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    try { process.Kill(true); } catch { }
                    return new GhostscriptRunResult
                    {
                        Success = false,
                        ErrorCode = GhostscriptErrorCode.Cancelled,
                        Message = "Ghostscript execution cancelled by user.",
                        StdOut = stdOut.ToString(),
                        StdErr = stdErr.ToString()
                    };
                }

                if (DateTime.UtcNow > deadline)
                {
                    try { process.Kill(true); } catch { }
                    return new GhostscriptRunResult
                    {
                        Success = false,
                        ErrorCode = GhostscriptErrorCode.Timeout,
                        Message = $"Ghostscript timed out after {timeout.TotalMinutes:0} min.",
                        StdOut = stdOut.ToString(),
                        StdErr = stdErr.ToString()
                    };
                }
            }

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                return new GhostscriptRunResult
                {
                    Success = false,
                    ErrorCode = GhostscriptErrorCode.NonZeroExitCode,
                    Message = $"Ghostscript exited with code {process.ExitCode}.",
                    StdOut = stdOut.ToString(),
                    StdErr = stdErr.ToString(),
                    ExitCode = process.ExitCode
                };
            }

            if (!string.IsNullOrWhiteSpace(expectedOutputPath) && !File.Exists(expectedOutputPath))
            {
                return new GhostscriptRunResult
                {
                    Success = false,
                    ErrorCode = GhostscriptErrorCode.OutputFileMissing,
                    Message = $"Output file was not created: {expectedOutputPath}",
                    StdOut = stdOut.ToString(),
                    StdErr = stdErr.ToString(),
                    ExitCode = process.ExitCode
                };
            }

            return new GhostscriptRunResult
            {
                Success = true,
                ErrorCode = GhostscriptErrorCode.None,
                Message = "Ghostscript run completed successfully.",
                StdOut = stdOut.ToString(),
                StdErr = stdErr.ToString(),
                ExitCode = process.ExitCode
            };
        }
        catch (Exception ex)
        {
            return new GhostscriptRunResult
            {
                Success = false,
                ErrorCode = GhostscriptErrorCode.Unknown,
                Message = ex.Message,
                StdOut = stdOut.ToString(),
                StdErr = stdErr.ToString()
            };
        }
        finally
        {
            bool hasExited = processStarted && process.HasExited;
            bool isSuccess = hasExited && process.ExitCode == 0;
            int exitCode = hasExited ? process.ExitCode : -1;

            logger.Info("Ghostscript finished",
                ("operation", operation),
                ("attempt", attempt),
                ("started", processStarted),
                ("success", isSuccess),
                ("exitCode", exitCode));
        }
    }

    private static string BuildArguments(string args)
    {
        return $"-dSAFER -dNOPROMPT -dQUIET {args}";
    }

    private static IEnumerable<string> BuildCandidates()
    {
        string baseDir = AppContext.BaseDirectory;
        yield return Path.Combine(baseDir, "gswin64c.exe");
        yield return Path.Combine(baseDir, "gswin32c.exe");
        yield return Path.Combine(baseDir, "gs", "bin", "gswin64c.exe");
        yield return Path.Combine(baseDir, "gs", "bin", "gswin32c.exe");

        foreach (string root in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "gs"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "gs")
                 })
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) continue;

            IEnumerable<string> versionDirs = Directory.GetDirectories(root)
                .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase);

            foreach (string dir in versionDirs)
            {
                yield return Path.Combine(dir, "bin", "gswin64c.exe");
                yield return Path.Combine(dir, "bin", "gswin32c.exe");
            }
        }
    }
}
