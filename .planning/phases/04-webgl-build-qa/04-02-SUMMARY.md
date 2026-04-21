# Phase 04-02 — WebGL Build & GitHub Pages Deploy — SUMMARY

**Status:** Complete
**Wave:** 2
**Commits:** manual deployment to gh-pages branch

## What Was Built

- `.nojekyll` file present in repository root (already existed — confirmed present)
- Unity WebGL build compiled with **Disabled** compression (Gzip dropped due to GitHub Pages CDN double-decompression issue with `.framework.js.gz`)
- Build deployed to `gh-pages` branch via orphan branch workflow
- GitHub Pages configured: source = `gh-pages` branch, folder = `/(root)`

## Issue Resolved During Execution

**Gzip CDN double-decompression bug:** GitHub Pages' Fastly CDN detects `Matrimiorci_Build.framework.js.gz` by its `.js` extension and serves it with `Content-Encoding: gzip`. The browser auto-decompresses it; Unity's loader then tries to decompress again → corrupted → `Uncaught SyntaxError: Invalid or unexpected token (at Matrimiorci_Build.framework.js.gz:1:1)`.

**Fix:** Switched Unity Player Settings → Publishing Settings → Compression Format from **Gzip** to **Disabled**. Rebuilt and redeployed. No `.gz` files → no CDN interference.

**Note:** Build size increased to ~40-50MB uncompressed (was 14MB with Gzip). Accepted — developer confirmed size is not a concern for this use case.

## Developer Actions Completed (Checkpoint)

- Rebuilt Unity WebGL with Disabled compression
- Created orphan `gh-pages` branch, removed tracked files, copied build output
- Staged only `Build/`, `index.html`, `TemplateData/`, `.nojekyll` (bypassing untracked Unity runtime folders)
- Pushed `gh-pages` branch to origin with `--force`
- Configured GitHub Pages: `gh-pages` branch, root folder
- Verified both URL routes:
  - `https://{user}.github.io/{repo}/` → Home scene ✅
  - `https://{user}.github.io/{repo}/?type=invite` → Invite scene ✅

## Key Files

| File | Purpose |
|------|---------|
| `.nojekyll` | Disables Jekyll on GitHub Pages — prevents Unity asset file stripping |
| `Build/Matrimiorci_Build.data` | Unity WebGL data file (uncompressed) |
| `Build/Matrimiorci_Build.framework.js` | Unity framework JavaScript (uncompressed) |
| `Build/Matrimiorci_Build.wasm` | WebAssembly module (uncompressed) |
| `Build/Matrimiorci_Build.loader.js` | Unity WebGL loader entry point |

## Requirements Satisfied

- TECH-01: WebGL build compiled successfully ✅
- TECH-02: Build deployed and accessible (size > 12MB but accepted) ✅
- TECH-08: HTTPS hosting via GitHub Pages ✅
