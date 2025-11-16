using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace AvaloniaHello.Services;

public interface IPdfLauncher
{
    Task<bool> TryOpenAsync(string filePath);
}

public static class PdfLauncher
{
    private static IPdfLauncher? _platformLauncher;

    private static readonly IPdfLauncher _defaultLauncher = new DefaultPdfLauncher();

    public static void Configure(IPdfLauncher launcher)
    {
        _platformLauncher = launcher;
    }

    public static IPdfLauncher Current => _platformLauncher ?? _defaultLauncher;

    private sealed class DefaultPdfLauncher : IPdfLauncher
    {
        public Task<bool> TryOpenAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return Task.FromResult(false);
                }

                if (OperatingSystem.IsWindows())
                {
                    Process.Start(new ProcessStartInfo("cmd", $"/c start \"\" \"{filePath}\"")
                    {
                        CreateNoWindow = true
                    });
                }
                else if (OperatingSystem.IsMacOS())
                {
                    Process.Start("open", filePath);
                }
                else if (OperatingSystem.IsLinux())
                {
                    Process.Start("xdg-open", filePath);
                }
                else
                {
                    return Task.FromResult(false);
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to launch PDF: {ex.Message}");
                return Task.FromResult(false);
            }
        }
    }
}
