# NextGallery Desktop

[![Build](https://github.com/mcp-tool-shop-org/NextGallery-Desktop/actions/workflows/ci.yml/badge.svg)](https://github.com/mcp-tool-shop-org/NextGallery-Desktop/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Windows](https://img.shields.io/badge/Windows-10%2B-blue.svg)](https://www.microsoft.com/windows)

**NextGallery Desktop** is a standalone Windows desktop application for browsing and managing AI-generated images and videos. Built with .NET MAUI and WinUI 3 for native Windows performance.

> **Note:** This is the standalone desktop version. For the CodeComfy VS Code extension integration, see [next-gallery](https://github.com/mcp-tool-shop-org/next-gallery).

## Features

### Core Features
- **High Performance** - Virtualized grid with lazy loading for thousands of images
- **Instant Previews** - Quick hover previews without opening files
- **Smart Organization** - Filter by date, prompt, model, or custom tags
- **Metadata Display** - View generation parameters, prompts, and settings
- **Video Support** - Preview and organize AI-generated videos
- **Standalone Mode** - Browse any folder of images without external dependencies

### Compare Mode
- **Side-by-Side View** - Compare two images visually
- **Parameter Diff** - See exactly what changed between generations
- **Multiple View Modes** - SideBySide, Overlay, or DiffOnly

### Search & Filter
- **Text Search** - Find images by filename or embedded metadata
- **Date Filtering** - Filter by creation or modification date
- **Favorites** - Mark and filter your best images
- **Combined Filters** - All filters work together (AND logic)

## Download

- **[Latest Release](https://github.com/mcp-tool-shop-org/NextGallery-Desktop/releases/latest)** - Download MSIX installer
- **Microsoft Store** - Coming soon

## Installation

### Option 1: MSIX Package (Recommended)

1. Download the `.msix` file from [Releases](https://github.com/mcp-tool-shop-org/NextGallery-Desktop/releases)
2. Double-click to install
3. Launch from Start Menu

### Option 2: Build from Source

```powershell
git clone https://github.com/mcp-tool-shop-org/NextGallery-Desktop
cd NextGallery-Desktop
dotnet build Gallery.App --framework net9.0-windows10.0.19041.0
```

## Usage

1. Launch NextGallery Desktop
2. Click "Add Folder" to select an image folder
3. Browse, search, and organize your images

### Supported Formats

**Images:** PNG, JPG, JPEG, WebP, BMP, GIF
**Videos:** MP4, WebM, MOV (requires FFmpeg for thumbnails)

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

## Requirements

- Windows 10 version 1809 or later
- .NET 9.0 Runtime (included with MSIX installer)

## Documentation

- [Release Notes](RELEASE_NOTES.md)
- [Contributing](CONTRIBUTING.md)
- [Support](docs/SUPPORT.md)
- [Privacy Policy](docs/PRIVACY.md)

## Related Projects

- [next-gallery](https://github.com/mcp-tool-shop-org/next-gallery) - CodeComfy-integrated version
- [codecomfy-vscode](https://github.com/mcp-tool-shop-org/codecomfy-vscode) - VS Code extension for ComfyUI

## Privacy

NextGallery Desktop works entirely offline. Your images and data never leave your device. See our [Privacy Policy](docs/PRIVACY.md) for details.

## License

MIT License - see [LICENSE](LICENSE) for details.

---

Copyright (c) 2025-2026 MCP Tool Shop
