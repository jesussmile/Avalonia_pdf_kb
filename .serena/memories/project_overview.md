# AvaloniaHello overview
- Purpose: showcase a single Avalonia 11 UI rendered across desktop, Android, iOS, and browser hosts while sharing view models and XAML from `AvaloniaHello/`.
- Tech stack: .NET 8 SDK, Avalonia UI 11, CommunityToolkit.Mvvm for reactive view-models, multi-target head projects per platform.
- Layout: shared app in `AvaloniaHello/` (App.axaml, Views, ViewModels, Assets), platform heads in `AvaloniaHello.Desktop/`, `.Android/`, `.iOS/`, `.Browser/`, each with its own `Program.cs`/entry manifest.
- Packaging: `Directory.Packages.props` pins package versions; solution root `AvaloniaHello.sln` ties projects together.
- Notable behavior: Main view binds `Greeting`, `UserMessage`, and `OpenPdfCommand`; PDF hook provided via injected callback.
