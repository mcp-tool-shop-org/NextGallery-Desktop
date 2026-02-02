# Contributing to NextGallery

Thank you for your interest in contributing to NextGallery!

## Development Setup

### Prerequisites
- .NET 9.0 SDK
- Windows 10 version 1809+
- Windows App SDK 1.4+
- Visual Studio 2022 or VS Code with C# Dev Kit

### Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/mcp-tool-shop-org/next-gallery.git
   cd next-gallery
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Build:
   ```bash
   dotnet build
   ```

4. Run tests:
   ```bash
   dotnet test
   ```

## Architecture

NextGallery follows Clean Architecture:
- `Gallery.Domain` - Core entities and interfaces
- `Gallery.Application` - Use cases and business logic
- `Gallery.Infrastructure` - External dependencies (SQLite, ImageSharp)
- `Gallery.App` - WinUI 3 desktop application
- `Gallery.Tests` - Test suite (115+ tests)

## Code Style

- C# with nullable reference types enabled
- Follow .NET naming conventions
- Use CommunityToolkit.Mvvm for MVVM patterns

## Pull Request Process

1. Fork the repository
2. Create a feature branch (`git checkout -b feat/amazing-feature`)
3. Make your changes
4. Ensure all tests pass (`dotnet test`)
5. Commit with conventional commits (`feat:`, `fix:`, `docs:`, etc.)
6. Push to your fork
7. Open a Pull Request

## Reporting Issues

- Use GitHub Issues for bug reports and feature requests
- Include Windows version and .NET runtime version
- Provide reproduction steps for bugs

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
