const CACHE_NAME = 'smartfollow-shell-v1';
const SHELL_FILES = [
  '/login.html',
  '/doctor-dashboard.html',
  '/patient-dashboard.html',
  '/admin-dashboard.html',
  '/case-details.html',
  '/prescriptions.html',
  '/reports.html',
  '/alerts.html',
  '/manifest.json',
  '/icons/icon-192.png',
  '/icons/icon-512.png'
];

// Cache the app shell on install
self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME).then((cache) => cache.addAll(SHELL_FILES)).catch(() => {})
  );
  self.skipWaiting();
});

// Clean up old caches on activate
self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((keys) =>
      Promise.all(keys.filter((k) => k !== CACHE_NAME).map((k) => caches.delete(k)))
    )
  );
  self.clients.claim();
});

// Network-first for our own pages (so users always get the latest version when online),
// falling back to the cached shell when offline. API calls (different origin) are left
// untouched — they always need a live network connection, caching them would be unsafe
// for medical data.
self.addEventListener('fetch', (event) => {
  const url = new URL(event.request.url);
  const isSameOrigin = url.origin === self.location.origin;
  const isNavigationOrShellFile = event.request.mode === 'navigate' || SHELL_FILES.includes(url.pathname);

  if (!isSameOrigin || !isNavigationOrShellFile) return; // let API/data requests pass through normally

  event.respondWith(
    fetch(event.request)
      .then((response) => {
        const clone = response.clone();
        caches.open(CACHE_NAME).then((cache) => cache.put(event.request, clone));
        return response;
      })
      .catch(() => caches.match(event.request).then((cached) => cached || caches.match('/login.html')))
  );
});
