using System;
using System.IO;
using System.Threading.Tasks;
using AvaloniaHello.Services;
using AvaloniaHello.ViewModels;
using AvaloniaHello.iOS.ViewModels;

namespace AvaloniaHello.iOS;

internal sealed class IosPdfPresenter : IPdfPresenter
{
    private readonly IPdfLauncher _launcher;

    public IosPdfPresenter(IPdfLauncher launcher)
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

        viewModel = new IosPdfViewModel(filePath, navigateBack);
        return true;
    }

    public Task<bool> TryPresentExternallyAsync(string filePath)
    {
        return _launcher.TryOpenAsync(filePath);
    }
}
