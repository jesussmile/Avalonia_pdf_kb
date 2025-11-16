using System;
using System.IO;
using System.Threading.Tasks;
using Android.Content;
using AndroidX.Core.Content;
using AvaloniaHello.Services;

namespace AvaloniaHello.Android;

internal sealed class AndroidPdfLauncher(Context context) : IPdfLauncher
{
    private readonly Context _context = context;

    public Task<bool> TryOpenAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return Task.FromResult(false);
            }

            var file = new Java.IO.File(filePath);
            var uri = FileProvider.GetUriForFile(_context, $"{_context.PackageName}.fileprovider", file);

            var intent = new Intent(Intent.ActionView);
            intent.SetDataAndType(uri, "application/pdf");
            intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.GrantReadUriPermission);

            var chooser = Intent.CreateChooser(intent, "Open PDF");
            chooser.AddFlags(ActivityFlags.NewTask | ActivityFlags.GrantReadUriPermission);

            _context.StartActivity(chooser);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Android.Util.Log.Warn("AndroidPdfLauncher", $"Failed to launch PDF: {ex.Message}");
            return Task.FromResult(false);
        }
    }
}
