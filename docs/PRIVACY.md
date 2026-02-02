# Privacy Policy

**NextGallery**
**Effective Date:** February 2, 2026
**Last Updated:** February 2, 2026

## Overview

NextGallery is a desktop application for browsing and managing AI-generated images and videos. This privacy policy explains how we handle your data.

## Data Collection

### What We Do NOT Collect

NextGallery does **not** collect, transmit, or store any personal information. Specifically:

- **No Analytics:** We do not collect usage statistics or telemetry
- **No Account Required:** The app works entirely offline without sign-up
- **No Network Transmission:** Your images, metadata, and browsing history never leave your device
- **No Cloud Storage:** All data remains on your local machine

### What the App Accesses Locally

NextGallery accesses the following data **only on your local device**:

| Data Type | Purpose | Storage |
|-----------|---------|---------|
| Image/Video Files | Display in gallery | Read-only access to folders you select |
| EXIF Metadata | Show generation parameters | Extracted and cached locally |
| Thumbnails | Performance optimization | Cached in app's local data folder |
| Workspace Settings | Remember your preferences | Stored in app's local settings |

## Data Storage

All application data is stored locally in:

```
%LOCALAPPDATA%\NextGallery\
```

This includes:
- Thumbnail cache
- SQLite database (media index)
- User preferences

You can delete this folder at any time to remove all app data.

## Third-Party Services

NextGallery does **not** integrate with any third-party services, APIs, or analytics platforms.

### FFmpeg

NextGallery may optionally use FFmpeg for video thumbnail generation. FFmpeg runs entirely locally and does not transmit any data. FFmpeg is licensed under LGPL/GPL - see the included license files.

## Permissions

NextGallery requests the following Windows capabilities:

| Permission | Reason |
|------------|--------|
| `runFullTrust` | Required for .NET MAUI desktop apps to access the file system |

The app only accesses folders you explicitly select or configure as library paths.

## Children's Privacy

NextGallery does not collect any personal information from anyone, including children under 13. The app is suitable for all ages.

## Changes to This Policy

We may update this privacy policy occasionally. Changes will be noted with a new "Last Updated" date. Continued use of the app after changes constitutes acceptance.

## Contact

For privacy questions or concerns:

- **GitHub Issues:** https://github.com/mcp-tool-shop-org/next-gallery/issues
- **Email:** 64996768+mcp-tool-shop@users.noreply.github.com

## Your Rights

Since we don't collect personal data, there is no personal data to access, correct, or delete. Your local app data can be deleted by uninstalling the app or removing the `%LOCALAPPDATA%\NextGallery\` folder.

---

**Summary:** NextGallery is a privacy-respecting application that works entirely offline. Your images and data never leave your device.
