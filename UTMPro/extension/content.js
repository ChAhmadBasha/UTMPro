// ═══════════════════════════════════════════════════════
// UTMPro Extension — Content Script (clipboard helper)
// ═══════════════════════════════════════════════════════

chrome.runtime.onMessage.addListener((msg, sender, sendResponse) => {
  if (msg.type === 'COPY') {
    navigator.clipboard.writeText(msg.text).then(() => {
      sendResponse({ success: true });
    }).catch(() => {
      // Fallback
      const el = document.createElement('textarea');
      el.value = msg.text;
      el.style.position = 'fixed';
      el.style.left = '-9999px';
      document.body.appendChild(el);
      el.select();
      document.execCommand('copy');
      document.body.removeChild(el);
      sendResponse({ success: true });
    });
    return true;
  }
});
