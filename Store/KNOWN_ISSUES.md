# Known Issues - Pre-Store Submission

This document tracks issues that must be resolved before Microsoft Store submission.

## Critical Issues (Must Fix)

### 1. Button Click Crashes
**Severity:** Critical
**Status:** Open
**Description:** App crashes when clicking "Refresh" or "Open Outputs Folder" buttons on the empty state view.

**Steps to Reproduce:**
1. Launch app without a valid CodeComfy workspace
2. Click either "Refresh" or "Open Outputs Folder" button
3. App crashes silently

**Likely Cause:** Null reference exception - buttons attempt to operate on workspace paths that don't exist or aren't properly initialized.

**Fix Required:** Add null checks and graceful error handling in button command handlers.

---

### 2. No Standalone Image Browsing
**Severity:** Medium
**Status:** By Design (but limits appeal)
**Description:** App only works with CodeComfy's `index.json` format. Cannot browse arbitrary image folders.

**Impact:** Reduces potential user base. Most users want a general-purpose gallery.

**Recommendation:** Consider adding a "simple mode" that can browse any folder of images without requiring CodeComfy metadata.

---

### 3. Missing Error Dialogs
**Severity:** High
**Status:** Open
**Description:** Errors fail silently instead of showing user-friendly error messages.

**Fix Required:** Add try-catch blocks with MessageDialog or similar UI feedback.

---

## Store Submission Blockers

| Issue | Severity | Status |
|-------|----------|--------|
| Button crashes | Critical | Open |
| Silent failures | High | Open |
| No error dialogs | High | Open |
| Missing PNG tile assets | Medium | Open |
| Screenshots needed | Medium | Open |

## Recommended Pre-Submission Testing

1. [ ] Test all button interactions
2. [ ] Test with valid CodeComfy workspace
3. [ ] Test with invalid/missing workspace
4. [ ] Test window resize and minimize/restore
5. [ ] Run Windows App Certification Kit (WACK)
6. [ ] Test on clean Windows 10 install
7. [ ] Test on Windows 11

## Files Ready for Store

These assets are complete and ready:
- ✅ Privacy Policy (`docs/PRIVACY.md`)
- ✅ Terms of Service (`docs/TERMS.md`)
- ✅ Support Documentation (`docs/SUPPORT.md`)
- ✅ Store Listing Content (`Store/STORE_LISTING.md`)
- ✅ GitHub Issue Templates
- ✅ GitHub Pages workflow
- ✅ App Icon SVG (needs PNG export)
- ✅ CI/CD Pipeline

## Next Steps

1. Fix button crash bugs
2. Add error handling throughout
3. Generate PNG tile assets from SVG
4. Capture screenshots with working app
5. Run WACK and fix any failures
6. Submit to Partner Center
