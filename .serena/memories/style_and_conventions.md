# Style & conventions
- C# 11 with file-scoped namespaces; nullable reference types enabled; prefer implicit usings.
- MVVM via CommunityToolkit.Mvvm: `partial` view-model classes derive from `ViewModelBase`, use `[ObservableProperty]` fields and generated properties, commands via `RelayCommand`/`IRelayCommand`.
- XAML uses Avalonia markup with `x:DataType` for compile-time binding checks, `Design.DataContext` for previewer, and resource brushes defined inline.
- Keep comments minimal; only add explanatory remarks for preview-only data contexts or non-obvious behaviors.
- Project favors dependency injection through constructor callbacks rather than service locators; keep properties immutable when possible.
