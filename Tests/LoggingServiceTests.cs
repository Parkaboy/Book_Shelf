using System;
using System.IO;
using Serilog;
using Xunit;

namespace Book_Shelf.Tests;

public sealed class SerilogTests : IDisposable
{
    private readonly string testRoot = Path.Combine(Path.GetTempPath(), "BookShelfLoggingTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Serilog_WritesContextAndExceptionToErrorFile()
    {
        var errorFilePath = Path.Combine(testRoot, "error.log");
        Directory.CreateDirectory(testRoot);
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Error()
            .WriteTo.File(errorFilePath)
            .CreateLogger();

        try
        {
            Log.Error(new InvalidOperationException("Something failed"), "Test operation");
            Log.CloseAndFlush();

            var log = File.ReadAllText(errorFilePath);
            Assert.Contains("Test operation", log);
            Assert.Contains("InvalidOperationException", log);
            Assert.Contains("Something failed", log);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }
    }
}
