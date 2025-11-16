using System;
using System.IO;
using System.Threading.Tasks;
using AvaloniaHello.Services;
using AvaloniaHello.ViewModels;

namespace AvaloniaHello.Android;

internal sealed class AndroidPdfPresenter : IPdfPresenter
{
    private readonly IPdfLauncher _launcher;

    public AndroidPdfPresenter(IPdfLauncher launcher)
    {
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
    }

    public bool TryCreateEmbeddedViewModel(string filePath, Action? navigateBack, out ViewModelBase viewModel)
    {
        if (!File.Exists(filePath))
        {
            viewModel = null!;
            return false;
        }

        viewModel = new AndroidPdfViewModel(filePath, navigateBack);
        return true;
    }

    public Task<bool> TryPresentExternallyAsync(string filePath)
    {
        return _launcher.TryOpenAsync(filePath);
    }
}
