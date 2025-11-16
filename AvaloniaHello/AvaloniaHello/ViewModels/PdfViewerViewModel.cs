using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuPDFCore;
using MuPDFCore.MuPDFRenderer;

namespace AvaloniaHello.ViewModels;

public partial class PdfViewerViewModel : ViewModelBase, IDisposable, IPdfDocumentViewModel
{
    private readonly string _documentPath;
    private readonly Action? _navigateBack;
    private readonly object _documentLock = new();
    private MuPDFContext? _context;
    private MuPDFDocument? _document;
    private PDFRenderer? _renderer;
    private bool _disposed;

    public PdfViewerViewModel(string documentPath, Action? navigateBack)
    {
      Console.WriteLine($"=== PdfViewerViewModel Constructor ===");
        Console.WriteLine($"Document Path: {documentPath}");
  Console.WriteLine($"File Exists: {System.IO.File.Exists(documentPath)}");
        
  _documentPath = documentPath;
      _navigateBack = navigateBack;
   BackCommand = new RelayCommand(() => _navigateBack?.Invoke());
    }

    public ObservableCollection<PageThumbnail> Thumbnails { get; } = new();

    [ObservableProperty]
 private bool _isBusy;

    [ObservableProperty]
    private int _activePageIndex;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
  private bool _hasError;

    public bool IsInitialized { get; private set; }

    public IRelayCommand BackCommand { get; }

    public int PageCount => _document?.Pages.Length ?? 0;

  public void Activate()
    {
    Console.WriteLine($"=== PdfViewerViewModel.Activate() called ===");
        EnsureDocumentLoaded();
    }

    private void EnsureDocumentLoaded()
    {
   Console.WriteLine($"=== EnsureDocumentLoaded() ===");
        Console.WriteLine($"IsInitialized: {IsInitialized}");
        Console.WriteLine($"Document Path: {_documentPath}");
        Console.WriteLine($"File Exists: {System.IO.File.Exists(_documentPath)}");
    
        if (IsInitialized || !System.IO.File.Exists(_documentPath))
        {
    if (!IsInitialized)
 {
            var msg = $"ERROR: Document path missing at {_documentPath}";
    Console.WriteLine(msg);
       ErrorMessage = msg;
    HasError = true;
    }
else
    {
         Console.WriteLine("Document already initialized");
       }
   return;
    }

        try
        {
   Console.WriteLine("Creating MuPDFContext...");
       _context = new MuPDFContext();
      Console.WriteLine("MuPDFContext created successfully");
        
      Console.WriteLine($"Loading document from: {_documentPath}");
    _document = new MuPDFDocument(_context, _documentPath);
    Console.WriteLine($"Document loaded successfully with {_document.Pages.Length} pages");
    
 IsInitialized = true;
            HasError = false;
        ErrorMessage = null;

  OnPropertyChanged(nameof(PageCount));

  var loadTask = GenerateThumbnailsAsync();
     _ = loadTask.ContinueWith(t =>
       {
  if (t.Exception is not null)
       {
    Console.WriteLine($"ERROR: Thumbnail generation failed: {t.Exception.GetBaseException().Message}");
      Console.WriteLine($"Stack trace: {t.Exception.GetBaseException().StackTrace}");
       }
     }, TaskScheduler.Default);
     }
 catch (Exception ex)
        {
        var msg = $"CRITICAL ERROR in EnsureDocumentLoaded: {ex.GetType().Name}: {ex.Message}";
     Console.WriteLine(msg);
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
      ErrorMessage = msg;
 HasError = true;
     throw;
   }
    }

 public void AttachRenderer(PDFRenderer renderer)
  {
      Console.WriteLine($"=== AttachRenderer() called ===");
 Console.WriteLine($"Renderer: {renderer?.GetType().Name ?? "null"}");
        Console.WriteLine($"IsInitialized: {IsInitialized}");
        Console.WriteLine($"Document: {(_document != null ? "Loaded" : "null")}");
      
        _renderer = renderer;

        if (!IsInitialized || _document is null)
        {
   var msg = "WARNING: Cannot attach renderer - document not initialized";
   Console.WriteLine(msg);
            ErrorMessage = msg;
            HasError = true;
      return;
   }

  try
        {
  var threadCount = Math.Max(1, Environment.ProcessorCount - 1);
       Console.WriteLine($"Initializing renderer with {threadCount} threads, starting at page {ActivePageIndex}");
      
      renderer.Initialize(_document, threadCount, ActivePageIndex, 1.0, includeAnnotations: true, ocrLanguage: null);
   Console.WriteLine("Renderer.Initialize() completed");
 
      renderer.Cover();
       Console.WriteLine("Renderer.Cover() completed - PDF should now be visible");
  HasError = false;
            ErrorMessage = null;
  }
        catch (Exception ex)
        {
    var msg = $"ERROR in AttachRenderer: {ex.GetType().Name}: {ex.Message}";
            Console.WriteLine(msg);
          Console.WriteLine($"Stack trace: {ex.StackTrace}");
         ErrorMessage = msg;
       HasError = true;
   throw;
  }
 }

    private async Task GenerateThumbnailsAsync()
    {
     Console.WriteLine($"=== GenerateThumbnailsAsync() started ===");
 
        if (_document is null)
     {
            Console.WriteLine("ERROR: GenerateThumbnailsAsync called before document load");
        return;
   }

  IsBusy = true;

     var pageCount = PageCount;
        Console.WriteLine($"Generating thumbnails for {pageCount} pages");

        Dispatcher.UIThread.Post(() => Thumbnails.Clear());

     try
        {
 await Task.Run(() =>
     {
   for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
       {
         try
      {
    Console.WriteLine($"Creating thumbnail for page {pageIndex + 1}/{pageCount}");
var thumbnail = CreateThumbnail(pageIndex);
          Dispatcher.UIThread.Post(() => Thumbnails.Add(thumbnail));
      Console.WriteLine($"Thumbnail {pageIndex + 1}/{pageCount} added successfully");
        }
   catch (Exception ex)
   {
             Console.WriteLine($"ERROR: Failed to render thumbnail {pageIndex}: {ex.Message}");
       Console.WriteLine($"Stack trace: {ex.StackTrace}");
     throw;
        }
           }
            });

      if (pageCount > 0)
          {
       Console.WriteLine("Setting ActivePageIndex to 0");
     ActivePageIndex = 0;
      }
        }
        finally
        {
         IsBusy = false;
        Console.WriteLine($"Thumbnail generation finished. Count={Thumbnails.Count}");
   }
    }

    private PageThumbnail CreateThumbnail(int pageNumber)
    {
  lock (_documentLock)
     {
          if (_document is null)
 {
                throw new InvalidOperationException("Document not loaded");
}

      var bounds = _document.Pages[pageNumber].Bounds;
          const double targetWidth = 180d;
     var zoom = Math.Clamp(targetWidth / bounds.Width, 0.08d, 0.6d);
         var rounded = bounds.Round(zoom);

 // Try BGR pixel format to fix blue tint (Red/Blue channel swap issue)
            Console.WriteLine($"Rendering thumbnail {pageNumber} with BGR format...");
          var pixelData = _document.Render(pageNumber, bounds, zoom, MuPDFCore.PixelFormats.BGR);
      Console.WriteLine($"Rendered thumbnail for page {pageNumber}: {rounded.Width}x{rounded.Height}, {pixelData.Length} bytes");

       // Use Bgra8888 to match BGR pixel data
       var bitmap = new WriteableBitmap(new PixelSize(rounded.Width, rounded.Height), new Vector(72, 72), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
         using (var fb = bitmap.Lock())
    {
      // Copy BGR data - need to add alpha channel
     var stride = rounded.Width * 4; // 4 bytes per pixel (BGRA)
       var bgraData = new byte[rounded.Width * rounded.Height * 4];
         
      // Convert BGR to BGRA by adding 255 alpha
  for (int i = 0, j = 0; i < pixelData.Length; i += 3, j += 4)
         {
 bgraData[j] = pixelData[i];     // B
          bgraData[j + 1] = pixelData[i + 1]; // G
        bgraData[j + 2] = pixelData[i + 2]; // R
 bgraData[j + 3] = 255; // A (fully opaque)
      }
      
            Marshal.Copy(bgraData, 0, fb.Address, bgraData.Length);
          }

            return new PageThumbnail(pageNumber, bitmap);
        }
}

    partial void OnActivePageIndexChanged(int value)
    {
        Console.WriteLine($"=== ActivePageIndex changed to {value} ===");
     
        if (_renderer is null || _document is null)
        {
  var rendererStatus = _renderer != null ? "OK" : "null";
            var documentStatus = _document != null ? "OK" : "null";
       Console.WriteLine($"WARNING: Cannot change page - Renderer: {rendererStatus}, Document: {documentStatus}");
    return;
     }

        try
        {
     var targetPage = Math.Clamp(value, 0, Math.Max(0, PageCount - 1));
  Console.WriteLine($"Switching to page {targetPage}");
        
      var threadCount = Math.Max(1, Environment.ProcessorCount - 1);
       _renderer.Initialize(_document, threadCount, targetPage, 1.0, includeAnnotations: true, ocrLanguage: null);
  _renderer.Cover();
Console.WriteLine($"Successfully switched to page {targetPage}");
        }
        catch (Exception ex)
      {
            Console.WriteLine($"ERROR in OnActivePageIndexChanged: {ex.Message}");
Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

  public void Dispose()
    {
        if (_disposed)
 {
            return;
        }

  Console.WriteLine("=== PdfViewerViewModel.Dispose() ===");
        _disposed = true;
      _renderer?.ReleaseResources();
        _renderer = null;
        _document?.Dispose();
        _context?.Dispose();
  Console.WriteLine("PdfViewerViewModel: Disposed resources");
    }

    public record PageThumbnail(int PageNumber, Bitmap Image)
    {
 public string Title => $"Page {PageNumber + 1}";
    }
}