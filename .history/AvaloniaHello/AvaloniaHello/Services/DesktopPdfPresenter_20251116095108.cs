using System;
using System.Threading.Tasks;
using AvaloniaHello.ViewModels;

namespace AvaloniaHello.Services;

internal sealed class DesktopPdfPresenter : IPdfPresenter
{
    public bool TryCreateEmbeddedViewModel(string filePath, Action? navigateBack, out ViewModelBase viewModel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        viewModel = new PdfViewerViewModel(filePath, navigateBack);
        return true;
    }

    public Task<bool> TryPresentExternallyAsync(string filePath)
    {
        return Task.FromResult(false);
    }
}
