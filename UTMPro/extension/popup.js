// ═══════════════════════════════════════════════════════
// UTMPro Browser Extension — Popup Logic
// ═══════════════════════════════════════════════════════

let config = { apiBaseUrl: '', apiKey: '', workspaceId: 0 };
let createdShortUrl = '';

// ─── Init ────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', async () => {
  const stored = await chrome.storage.sync.get(['apiBaseUrl', 'apiKey', 'workspaceId']);
  
  if (stored.apiKey && stored.workspaceId) {
    config = stored;
    showMain();
  } else {
    showLogin();
  }
});

// ─── Screens ─────────────────────────────────────────
function showLogin() {
  document.getElementById('loginScreen').classList.remove('hidden');
  document.getElementById('mainScreen').classList.add('hidden');
}

async function showMain() {
  document.getElementById('loginScreen').classList.add('hidden');
  document.getElementById('mainScreen').classList.remove('hidden');

  // Load current tab info
  chrome.runtime.sendMessage({ type: 'GET_CURRENT_TAB' }, (resp) => {
    if (resp) {
      document.getElementById('pageTitle').textContent = resp.title || 'Current Page';
      document.getElementById('pageUrl').textContent = resp.url || '';
      document.getElementById('destUrl').value = resp.url || '';
      
      // Extract favicon
      try {
        const u = new URL(resp.url);
        document.getElementById('pageFavicon').innerHTML = 
          `<img src="https://www.google.com/s2/favicons?domain=${u.hostname}&sz=32" style="width:20px;height:20px;border-radius:4px;" onerror="this.parentElement.textContent='🌐'">`;
      } catch(e) {}
    }
  });

  // Check for pending URL from context menu
  const pending = await chrome.storage.local.get(['pendingUrl', 'pendingTitle']);
  if (pending.pendingUrl) {
    document.getElementById('destUrl').value = pending.pendingUrl;
    document.getElementById('pageTitle').textContent = pending.pendingTitle || 'Context Menu Link';
    chrome.runtime.sendMessage({ type: 'CLEAR_BADGE' });
  }

  // Load domains
  loadDomains();
  // Load recent links
  loadRecent();
}

// ─── Settings ────────────────────────────────────────
async function saveSettings() {
  const baseUrl = document.getElementById('apiBaseUrl').value.replace(/\/$/, '');
  const key = document.getElementById('apiKey').value.trim();
  const wsId = parseInt(document.getElementById('workspaceId').value) || 0;

  if (!key || !wsId) {
    alert('API Key and Workspace ID are required');
    return;
  }

  config = { apiBaseUrl: baseUrl || 'https://app.utmpro.link', apiKey: key, workspaceId: wsId };
  await chrome.storage.sync.set(config);
  showMain();
}

function showSettings() {
  // Toggle back to login/settings screen
  document.getElementById('apiBaseUrl').value = config.apiBaseUrl;
  document.getElementById('apiKey').value = config.apiKey;
  document.getElementById('workspaceId').value = config.workspaceId;
  showLogin();
}

function openDashboard() {
  chrome.tabs.create({ url: config.apiBaseUrl });
}

// ─── API Helper ──────────────────────────────────────
async function api(method, endpoint, body = null) {
  const opts = {
    method,
    headers: {
      'Authorization': `Bearer ${config.apiKey}`,
      'Content-Type': 'application/json'
    }
  };
  if (body) opts.body = JSON.stringify(body);

  const url = `${config.apiBaseUrl}${endpoint}${endpoint.includes('?') ? '&' : '?'}workspaceId=${config.workspaceId}`;
  const res = await fetch(url, opts);
  return res.json();
}

// ─── Load Domains ────────────────────────────────────
async function loadDomains() {
  try {
    const data = await api('GET', '/api/v1/domains');
    const select = document.getElementById('domainSelect');
    select.innerHTML = '';
    (data.data || []).forEach(d => {
      const opt = document.createElement('option');
      opt.value = d.id;
      opt.textContent = d.domainName;
      select.appendChild(opt);
    });
  } catch (e) {
    console.error('Failed to load domains:', e);
  }
}

// ─── Create Link ─────────────────────────────────────
async function createLink() {
  const destUrl = document.getElementById('destUrl').value.trim();
  if (!destUrl) { showError('Please enter a destination URL'); return; }

  const btn = document.getElementById('createBtn');
  const btnText = document.getElementById('createBtnText');
  const spinner = document.getElementById('createSpinner');

  btn.disabled = true;
  btnText.textContent = 'Creating...';
  spinner.classList.remove('hidden');
  hideResult(); hideError();

  try {
    const body = {
      primaryUrl: destUrl,
      domainId: parseInt(document.getElementById('domainSelect').value) || 0,
      customSlug: document.getElementById('slugInput').value.trim() || null,
      utmSource: document.getElementById('utmSource').value.trim() || null,
      utmMedium: document.getElementById('utmMedium').value.trim() || null,
      utmCampaign: document.getElementById('utmCampaign').value.trim() || null,
      destinations: [],
      tagIds: [],
      targetingRules: []
    };

    const data = await api('POST', '/api/v1/links', body);

    if (data.success && data.link) {
      const domain = data.link.domain || 'go.utmpro.link';
      const slug = data.link.slug;
      createdShortUrl = `https://${domain}/${slug}`;

      document.getElementById('resultUrl').textContent = createdShortUrl;
      document.getElementById('resultCard').classList.remove('hidden');

      // Auto-copy
      await copyToClipboard(createdShortUrl);

      // Save to recent
      saveToRecent({ shortUrl: createdShortUrl, destUrl, slug, domain, createdAt: new Date().toISOString() });
      loadRecent();
    } else {
      showError(data.error || 'Failed to create link');
    }
  } catch (e) {
    showError('Network error: ' + e.message);
  } finally {
    btn.disabled = false;
    btnText.textContent = 'Create Short Link';
    spinner.classList.add('hidden');
  }
}

// ─── Copy Link ───────────────────────────────────────
async function copyLink() {
  await copyToClipboard(createdShortUrl);
  const fb = document.getElementById('copyFeedback');
  fb.classList.remove('hidden');
  setTimeout(() => fb.classList.add('hidden'), 2000);
}

async function copyToClipboard(text) {
  try {
    await navigator.clipboard.writeText(text);
  } catch (e) {
    chrome.runtime.sendMessage({ type: 'COPY_TO_CLIPBOARD', text });
  }
}

function openLink() {
  if (createdShortUrl) chrome.tabs.create({ url: createdShortUrl });
}

// ─── QR Code ─────────────────────────────────────────
function showQR() {
  const container = document.getElementById('qrContainer');
  container.classList.toggle('hidden');
  if (!container.classList.contains('hidden')) {
    const canvas = document.getElementById('qrCanvas');
    if (typeof QRCode !== 'undefined') {
      QRCode.toCanvas(canvas, createdShortUrl, { width: 180, margin: 2, color: { dark: '#000', light: '#fff' } });
    }
  }
}

function downloadQR() {
  const canvas = document.getElementById('qrCanvas');
  const link = document.createElement('a');
  link.download = `qr-${createdShortUrl.split('/').pop()}.png`;
  link.href = canvas.toDataURL();
  link.click();
}

// ─── Recent Links ────────────────────────────────────
async function loadRecent() {
  const container = document.getElementById('recentLinks');
  
  // Load from local storage
  const stored = await chrome.storage.local.get('recentLinks');
  const recent = stored.recentLinks || [];

  if (recent.length === 0) {
    container.innerHTML = '<p class="text-muted text-center py-3" style="font-size:12px;">No links created yet.<br>Shorten your first link above!</p>';
    return;
  }

  container.innerHTML = recent.slice(0, 8).map(link => `
    <div class="recent-item" onclick="copyRecent('${link.shortUrl}')">
      <div class="recent-icon">🔗</div>
      <div class="recent-details">
        <div class="recent-short">${link.shortUrl.replace('https://', '')}</div>
        <div class="recent-dest">${link.destUrl}</div>
      </div>
      <div class="recent-clicks" title="Click to copy">📋</div>
    </div>
  `).join('');
}

async function copyRecent(url) {
  await copyToClipboard(url);
  // Brief visual feedback
  const items = document.querySelectorAll('.recent-item');
  items.forEach(item => {
    if (item.querySelector('.recent-short')?.textContent === url.replace('https://', '')) {
      item.style.borderColor = '#22c55e';
      setTimeout(() => { item.style.borderColor = ''; }, 800);
    }
  });
}

async function saveToRecent(link) {
  const stored = await chrome.storage.local.get('recentLinks');
  const recent = stored.recentLinks || [];
  recent.unshift(link);
  if (recent.length > 20) recent.length = 20;
  await chrome.storage.local.set({ recentLinks: recent });
}

// ─── Error/Result Helpers ────────────────────────────
function showError(msg) {
  document.getElementById('errorMsg').textContent = msg;
  document.getElementById('errorCard').classList.remove('hidden');
}
function hideError() { document.getElementById('errorCard').classList.add('hidden'); }
function hideResult() { 
  document.getElementById('resultCard').classList.add('hidden');
  document.getElementById('qrContainer').classList.add('hidden');
}
