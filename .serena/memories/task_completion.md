# Task completion checklist
1. Run `dotnet build` (or the specific head project) to ensure the shared code compiles across targets; fix any warnings you introduce.
2. If you touched platform-specific code, run that head (`dotnet run --project AvaloniaHello.Desktop`, Android/iOS `-t:Run`, or browser publish) to verify UI behavior.
3. Update `README.md` or project notes if you changed prerequisites, commands, or user-facing functionality.
4. Add/adjust assets under `Assets/docs` when bundling new PDFs or resources; keep references in view models/views in sync.
5. Before handing off, run `git status` to confirm only intentional files changed and stage/commit with meaningful messages.
