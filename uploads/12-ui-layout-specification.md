# PART 12: UI LAYOUT SPECIFICATION

## 12.1 Sidebar Navigation (App Layout)

```html
<!-- Sidebar structure for all app pages -->
<!-- Left sidebar: 250px wide, dark background -->
<aside class="w-[250px] bg-white border-r border-gray-200 
              flex flex-col h-screen fixed left-0 top-0">
  
  <!-- Logo -->
  <div class="p-4 border-b border-gray-200">
    <a href="/{workspaceSlug}/links">
      <span class="font-bold text-xl">UTMPro</span>
    </a>
  </div>
  
  <!-- Workspace Switcher -->
  <div class="p-3 border-b border-gray-200">
    <!-- Workspace name + dropdown arrow -->
    <!-- Shows: workspace name, plan, member count -->
    <!-- Dropdown: switch workspace, create new -->
  </div>
  
  <!-- Navigation -->
  <nav class="flex-1 p-3 overflow-y-auto">
    
    <!-- SHORT LINKS section -->
    <p class="text-xs text-gray-500 px-2 mb-1">Short Links</p>
    <a href="/{slug}/links">🔗 Links</a>
    <a href="/{slug}/links/domains">🌐 Domains</a>
    
    <!-- INSIGHTS section -->
    <p class="text-xs text-gray-500 px-2 mb-1 mt-4">Insights</p>
    <a href="/{slug}/analytics">📊 Analytics</a>
    <a href="/{slug}/events">⚡ Events</a>
    <a href="/{slug}/customers">👥 Customers</a>
    
    <!-- LIBRARY section -->
    <p class="text-xs text-gray-500 px-2 mb-1 mt-4">Library</p>
    <a href="/{slug}/links/folders">📁 Folders</a>
    <a href="/{slug}/links/tags">🏷 Tags</a>
    
    <!-- USAGE section -->
    <div class="mt-4 px-2">
      <p class="text-xs text-gray-500 mb-2">Usage</p>
      <!-- Events: 0 of 1K -->
      <!-- Links: 1 of 25 -->
      <!-- Usage reset date -->
    </div>
  </nav>
  
  <!-- Bottom: Settings + Help + User -->
  <div class="border-t border-gray-200 p-3">
    <a href="/{slug}/settings">⚙ Settings</a>
    <a href="#">❓ Help</a>
    <!-- User avatar + name -->
  </div>
</aside>
```

## 12.2 Color Scheme

```css
/* UTMPro Design System */
--primary: #000000;           /* Black - primary buttons */
--primary-hover: #1f1f1f;
--secondary: #f9fafb;         /* Light gray backgrounds */
--accent-green: #22c55e;      /* Success, active states */
--accent-blue: #3b82f6;       /* Links, info */
--danger: #ef4444;            /* Delete, errors */
--border: #e5e7eb;            /* Default border */
--text-primary: #111827;
--text-secondary: #6b7280;
--text-muted: #9ca3af;
--sidebar-bg: #ffffff;
--sidebar-active: #f3f4f6;
--badge-pro: #f59e0b;         /* PRO badge color */
--badge-business: #8b5cf6;    /* BUSINESS badge */
```

---
