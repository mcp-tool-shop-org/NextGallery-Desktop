# Microsoft Store Assets

This folder contains assets required for Microsoft Store submission.

## Required Assets

### Screenshots (Required)
Place screenshots in the `Screenshots/` folder:
- **Minimum:** 1 screenshot
- **Recommended:** 3-5 screenshots
- **Resolution:** 1920x1080 (preferred) or 1366x768
- **Format:** PNG

Suggested screenshots:
1. `01-gallery-overview.png` - Main gallery view with images
2. `02-compare-mode.png` - Side-by-side comparison feature
3. `03-metadata-panel.png` - Metadata and generation parameters
4. `04-search-filter.png` - Search and filter functionality
5. `05-video-support.png` - Video playback/preview

### Store Logo (Required)
- **File:** `StoreLogo.png`
- **Size:** 300x300 pixels
- **Format:** PNG with transparency

### Tile Assets (Required for Windows)
Located in `Gallery.App/Platforms/Windows/Images/`:
- `StoreLogo.png` - 50x50 (Store tile)
- `Square44x44Logo.png` - 44x44 (taskbar)
- `Square71x71Logo.png` - 71x71 (small tile)
- `Square150x150Logo.png` - 150x150 (medium tile)
- `Wide310x150Logo.png` - 310x150 (wide tile)
- `Square310x310Logo.png` - 310x310 (large tile)
- `SplashScreen.png` - 620x300 (splash)

## Store Listing Content

### App Name
NextGallery

### Short Description (100 chars)
High-performance gallery for AI-generated images and videos with instant previews and smart filtering.

### Description (up to 10,000 chars)
```
NextGallery is a powerful Windows desktop application designed specifically for managing AI-generated images and videos. Built with modern .NET MAUI and WinUI 3 technology, it delivers a native Windows experience with exceptional performance.

KEY FEATURES:

Gallery & Organization
• Virtualized grid with lazy loading for thousands of images
• Smart organization by date, prompt, model, or custom tags
• Instant hover previews without opening files
• Support for both images and videos

Compare Mode
• Side-by-side visual comparison of generations
• Parameter diff highlighting what changed between images
• Multiple view modes: SideBySide, Overlay, DiffOnly

Workflow Search & Filter
• Search by prompt text (case-insensitive)
• Find images by exact seed number
• Filter by preset/model
• Date range filtering
• All filters work together with AND logic

Job Management
• Delete jobs with optional file removal
• Open outputs directly in File Explorer
• One-click copy prompt to clipboard
• Export metadata as JSON or readable format

ComfyUI Integration
• Works seamlessly with CodeComfy VS Code extension
• Automatically detects new outputs
• Reads generation metadata from image files

REQUIREMENTS:
• Windows 10 version 1809 or later
• Works best with AI image generation workflows

NextGallery is free, open-source software under the MIT License.
```

### Category
Photo & video > Photo viewers

### Keywords
gallery, AI images, image viewer, ComfyUI, Stable Diffusion, image manager, photo organizer, metadata viewer, AI art

### Age Rating
Everyone (no mature content)

## Submission Checklist

- [ ] Screenshots captured and placed in Screenshots/
- [ ] All tile PNGs generated from SVG icons
- [ ] Store listing text reviewed
- [ ] Privacy Policy URL configured in Partner Center
- [ ] Support URL configured in Partner Center
- [ ] EULA/Terms URL configured in Partner Center
- [ ] WACK tests passed locally
- [ ] App tested on clean Windows install
