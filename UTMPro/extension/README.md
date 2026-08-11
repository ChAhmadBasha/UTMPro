# UTMPro Browser Extension

Create branded short links, QR codes, and track clicks — from any webpage.

## Features

- 🔗 **One-Click Shortening** — Shorten the current page URL instantly
- 📋 **Auto-Copy** — Short link is copied to clipboard automatically
- 📱 **QR Code** — Generate and download QR codes for any link
- 📊 **UTM Builder** — Add UTM parameters without opening the dashboard
- 🖱️ **Right-Click Menu** — Right-click any link → "Shorten with UTMPro"
- 📜 **Recent Links** — Quick access to your last 20 created links
- 🔒 **Secure** — API key stored in browser sync storage

## Installation

### Chrome (from source)
1. Go to `chrome://extensions/`
2. Enable "Developer mode" (top-right toggle)
3. Click "Load unpacked"
4. Select the `extension/` folder
5. Click the UTMPro icon in the toolbar → enter your API key

### Firefox (from source)
1. Go to `about:debugging#/runtime/this-firefox`
2. Click "Load Temporary Add-on"
3. Select `extension/manifest.json`
4. Click UTMPro icon → enter API key

### Chrome Web Store / Firefox Add-ons
Coming soon — submit via:
- Chrome: https://chrome.google.com/webstore/devconsole
- Firefox: https://addons.mozilla.org/developers/

## Setup

1. Open the extension popup (click UTMPro icon)
2. Enter your **API Base URL** (default: `https://app.utmpro.link`)
3. Enter your **API Key** (get it from Settings → API Keys in UTMPro)
4. Enter your **Workspace ID**
5. Click "Connect Account"

## Usage

### Shorten current page
1. Navigate to any webpage
2. Click the UTMPro extension icon
3. The URL is pre-filled → click "Create Short Link"
4. Link is created and auto-copied to clipboard!

### Right-click any link
1. Right-click any link on a page
2. Select "Shorten with UTMPro"
3. Click the extension icon to see the pre-filled URL
4. Click "Create Short Link"

### QR Codes
1. After creating a link, click the "📱 QR" button
2. QR code appears inline
3. Click "Download QR" to save as PNG

### UTM Parameters
1. Click "📊 UTM Parameters" in the creation form
2. Fill in source, medium, campaign
3. Parameters are appended to the destination URL

## File Structure

```
extension/
├── manifest.json          # Chrome MV3 + Firefox manifest
├── popup.html             # Main popup UI
├── popup.js               # Popup logic (API, QR, clipboard)
├── background.js          # Service worker (context menu)
├── content.js             # Content script (clipboard fallback)
├── options.html           # Settings/options page
├── css/
│   └── popup.css          # Popup styles
├── lib/
│   └── qrcode.min.js      # QR code renderer
├── icons/
│   ├── icon16.png
│   ├── icon32.png
│   ├── icon48.png
│   └── icon128.png
└── README.md
```

## API Endpoints Used

- `GET /api/v1/domains?workspaceId=X` — Load domains for dropdown
- `POST /api/v1/links?workspaceId=X` — Create short link

## Requirements

- UTMPro account with API key
- Chrome 88+ or Firefox 109+
