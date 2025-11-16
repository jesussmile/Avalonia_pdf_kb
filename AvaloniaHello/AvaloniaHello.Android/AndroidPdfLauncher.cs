using System;
using System.IO;
using System.Threading.Tasks;
using Android.Content;
using Android.Content.PM;
using AndroidX.Core.Content;
using Android.Util;
using AvaloniaHello.Services;

namespace AvaloniaHello.Android;

internal sealed class AndroidPdfLauncher : IPdfLauncher
{
    private readonly Context _context;

    public AndroidPdfLauncher(Context context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<bool> TryOpenAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return Task.FromResult(false);
            }

            var file = new Java.IO.File(filePath);
            var packageName = _context.PackageName ?? throw new InvalidOperationException("Missing package name");
            var uri = FileProvider.GetUriForFile(_context, $"{packageName}.fileprovider", file);

            var intent = new Intent(Intent.ActionView);
            intent.SetDataAndType(uri, "application/pdf");
            intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.GrantReadUriPermission);

            var chooser = Intent.CreateChooser(intent, "Open PDF")
                          ?? throw new InvalidOperationException("Unable to create chooser intent");
            chooser.AddFlags(ActivityFlags.NewTask | ActivityFlags.GrantReadUriPermission);

            _context.StartActivity(chooser);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Log.Warn("AndroidPdfLauncher", $"Failed to launch PDF: {ex.Message}");
            return Task.FromResult(false);
        }
    }
}
