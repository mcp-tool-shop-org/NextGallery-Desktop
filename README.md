# NextGallery

**NextGallery** is a high-performance Windows desktop application for browsing and managing AI-generated images and videos. Built with .NET MAUI and WinUI 3, it provides a native Windows experience with smooth scrolling, instant previews, and powerful organization tools.

## Features

- **High Performance** - Virtualized grid with lazy loading for thousands of images
- **Instant Previews** - Quick hover previews without opening files
- **Smart Organization** - Filter by date, prompt, model, or custom tags
- **Metadata Display** - View generation parameters, prompts, and settings
- **ComfyUI Integration** - Works seamlessly with [CodeComfy VS Code](https://github.com/mcp-tool-shop-org/codecomfy-vscode)
- **Video Support** - Preview and organize AI-generated videos

## Installation

### Option 1: MSIX Package (Recommended)

1. Download the `.msix` file from [Releases](https://github.com/mcp-tool-shop-org/next-gallery/releases)
2. Double-click to install
3. Launch from Start Menu

### Option 2: Build from Source

```powershell
git clone https://github.com/mcp-tool-shop-org/next-gallery
cd next-gallery
dotnet build Gallery.App
```

## Architecture

NextGallery follows Clean Architecture principles:

```
NextGallery.sln
├── Gallery.App/           # WinUI 3 desktop application
├── Gallery.Application/   # Use cases and business logic
├── Gallery.Domain/        # Core entities and interfaces
├── Gallery.Infrastructure/# File system, metadata parsing
├── Gallery.Tests/         # Unit and integration tests
└── Contracts/             # Shared contracts and DTOs
```

## Integration with CodeComfy

NextGallery is designed to work with the [CodeComfy VS Code extension](https://github.com/mcp-tool-shop-org/codecomfy-vscode) for a seamless ComfyUI workflow:

1. Generate images/videos in VS Code with CodeComfy
2. NextGallery automatically detects new outputs
3. Browse, organize, and manage your generations

## Requirements

- Windows 10 version 1809 or later
- .NET 8.0 Runtime
- Windows App SDK 1.4+

## License

MIT License - see [LICENSE](LICENSE) for details.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.
