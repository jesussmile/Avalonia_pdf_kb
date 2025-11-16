using System;
using System.Threading.Tasks;
using AvaloniaHello.ViewModels;

namespace AvaloniaHello.Services;

public interface IPdfPresenter
{
    bool TryCreateEmbeddedViewModel(string filePath, Action? navigateBack, out ViewModelBase viewModel);

    Task<bool> TryPresentExternallyAsync(string filePath);
}

public static class PdfPresenter
{
    private static IPdfPresenter? _platformPresenter;
    private static readonly IPdfPresenter _defaultPresenter = new DesktopPdfPresenter();

    public static void Configure(IPdfPresenter presenter)
    {
        _platformPresenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
    }

    public static IPdfPresenter Current => _platformPresenter ?? _defaultPresenter;
}
