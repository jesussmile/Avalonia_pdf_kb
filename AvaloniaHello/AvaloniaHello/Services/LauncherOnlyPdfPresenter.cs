using System;
using System.Threading.Tasks;
using AvaloniaHello.ViewModels;

namespace AvaloniaHello.Services;

public sealed class LauncherOnlyPdfPresenter : IPdfPresenter
{
    private readonly IPdfLauncher _launcher;

    public LauncherOnlyPdfPresenter(IPdfLauncher launcher)
    {
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
    }

    public bool TryCreateEmbeddedViewModel(string filePath, Action? navigateBack, out ViewModelBase viewModel)
    {
        viewModel = null!;
        return false;
    }

    public Task<bool> TryPresentExternallyAsync(string filePath)
    {
        return _launcher.TryOpenAsync(filePath);
    }
}
