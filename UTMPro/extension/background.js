// ═══════════════════════════════════════════════════════
// UTMPro Browser Extension — Background Service Worker
// ═══════════════════════════════════════════════════════

// Context menu: right-click any link → "Shorten with UTMPro"
chrome.runtime.onInstalled.addListener(() => {
  chrome.contextMenus.create({
    id: 'utmpro-shorten-link',
    title: 'Shorten with UTMPro',
    contexts: ['link']
  });

  chrome.contextMenus.create({
    id: 'utmpro-shorten-page',
    title: 'Shorten this page with UTMPro',
    contexts: ['page']
  });

  chrome.contextMenus.create({
    id: 'utmpro-shorten-selection',
    title: 'Shorten selected URL with UTMPro',
    contexts: ['selection']
  });
});

chrome.contextMenus.onClicked.addListener(async (info, tab) => {
  let url = '';

  if (info.menuItemId === 'utmpro-shorten-link') {
    url = info.linkUrl || '';
  } else if (info.menuItemId === 'utmpro-shorten-page') {
    url = tab.url || '';
  } else if (info.menuItemId === 'utmpro-shorten-selection') {
    url = info.selectionText || '';
  }

  if (!url) return;

  // Store URL and open popup
  await chrome.storage.local.set({ pendingUrl: url, pendingTitle: tab.title || '' });

  // Can't programmatically open popup in MV3, so show notification
  // The popup will check for pendingUrl on open
  chrome.action.setBadgeText({ text: '1' });
  chrome.action.setBadgeBackgroundColor({ color: '#000000' });
});

// Listen for messages from popup
chrome.runtime.onMessage.addListener((msg, sender, sendResponse) => {
  if (msg.type === 'GET_CURRENT_TAB') {
    chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
      sendResponse({ url: tabs[0]?.url, title: tabs[0]?.title });
    });
    return true; // async response
  }

  if (msg.type === 'COPY_TO_CLIPBOARD') {
    // Write to clipboard via offscreen or content script
    chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
      if (tabs[0]) {
        chrome.tabs.sendMessage(tabs[0].id, { type: 'COPY', text: msg.text });
      }
    });
    sendResponse({ success: true });
    return true;
  }

  if (msg.type === 'CLEAR_BADGE') {
    chrome.action.setBadgeText({ text: '' });
    chrome.storage.local.remove(['pendingUrl', 'pendingTitle']);
    sendResponse({ success: true });
  }
});
