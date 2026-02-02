# Support

## Getting Help with NextGallery

### Quick Links

- **Report a Bug:** [GitHub Issues](https://github.com/mcp-tool-shop-org/next-gallery/issues/new?template=bug_report.md)
- **Request a Feature:** [GitHub Issues](https://github.com/mcp-tool-shop-org/next-gallery/issues/new?template=feature_request.md)
- **Discussions:** [GitHub Discussions](https://github.com/mcp-tool-shop-org/next-gallery/discussions)
- **Documentation:** [README](https://github.com/mcp-tool-shop-org/next-gallery#readme)

## Frequently Asked Questions

### Installation

**Q: What are the system requirements?**
- Windows 10 version 1809 or later
- .NET 9.0 Runtime (included with MSIX installer)
- Windows App SDK 1.4+

**Q: How do I install NextGallery?**
1. Download the `.msix` file from [Releases](https://github.com/mcp-tool-shop-org/next-gallery/releases)
2. Double-click to install
3. Launch from Start Menu

**Q: How do I uninstall?**
- Settings > Apps > Installed apps > NextGallery > Uninstall
- Or right-click the app in Start Menu > Uninstall

### Usage

**Q: How do I add a folder to my gallery?**
1. Open NextGallery
2. Click the folder icon or use File > Add Library Folder
3. Select the folder containing your images/videos

**Q: What image formats are supported?**
- Images: PNG, JPG, JPEG, WebP, BMP, GIF
- Videos: MP4, WebM, MOV (requires FFmpeg for thumbnails)

**Q: Why aren't video thumbnails showing?**
Video thumbnail generation requires FFmpeg. Either:
- Install FFmpeg and add it to your PATH
- Or download our pre-bundled version from the releases page

**Q: How do I use the Compare feature?**
1. Select a job in the gallery
2. Click "Compare" or press `C`
3. Select a second job to compare
4. Use the view mode buttons to switch between Side-by-Side, Overlay, or Diff views

**Q: How does search work?**
- **Prompt search:** Type in the search box to find images by prompt text
- **Seed search:** Enter a seed number to find exact matches
- **Filters:** Use the filter dropdown for preset, favorites, and date range

### Troubleshooting

**Q: The app won't start**
1. Ensure Windows 10 v1809+ is installed
2. Try restarting your computer
3. Reinstall the app
4. Check Windows Event Viewer for errors

**Q: Images aren't loading**
1. Verify the folder path exists
2. Check file permissions
3. Try removing and re-adding the library folder
4. Clear the thumbnail cache: delete `%LOCALAPPDATA%\NextGallery\`

**Q: The app is slow with many images**
- NextGallery uses virtualized scrolling for performance
- Initial thumbnail generation may take time
- Consider splitting very large libraries into subfolders

**Q: Metadata isn't showing**
- Metadata is read from image EXIF data
- ComfyUI images store metadata in PNG chunks
- Some image editors strip metadata when saving

## Reporting Issues

When reporting a bug, please include:

1. **Windows version** (e.g., Windows 11 23H2)
2. **NextGallery version** (Help > About)
3. **Steps to reproduce** the issue
4. **Expected behavior** vs actual behavior
5. **Screenshots** if applicable
6. **Error messages** from Event Viewer if available

### Where to Find Logs

Application logs (if enabled):
```
%LOCALAPPDATA%\NextGallery\logs\
```

Windows Event Viewer:
1. Open Event Viewer
2. Navigate to Windows Logs > Application
3. Filter by Source: "NextGallery"

## Feature Requests

We welcome feature suggestions! Please check [existing issues](https://github.com/mcp-tool-shop-org/next-gallery/issues) first to avoid duplicates.

When suggesting a feature:
1. Describe the use case
2. Explain the expected behavior
3. Include mockups or examples if possible

## Contributing

Interested in contributing code? See [CONTRIBUTING.md](https://github.com/mcp-tool-shop-org/next-gallery/blob/main/CONTRIBUTING.md)

## Contact

- **GitHub Issues:** https://github.com/mcp-tool-shop-org/next-gallery/issues
- **Email:** 64996768+mcp-tool-shop@users.noreply.github.com

---

**Response Time:** We aim to respond to issues within 48-72 hours. Critical bugs are prioritized.
