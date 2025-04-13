self.importScripts('service-worker-assets.js');

const CACHE_NAME = 'sdc-cache-v1';
const ASSETS = self.assetsManifest?.assets?.map(asset => asset.url) || [];

self.addEventListener('install', event => {
    console.log('[SW] Installing...');
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => cache.addAll(ASSETS))
            .then(() => self.skipWaiting())
    );
});

self.addEventListener('activate', event => {
    console.log('[SW] Activating...');
    event.waitUntil(
        caches.keys().then(keys =>
            Promise.all(keys
                .filter(key => key !== CACHE_NAME)
                .map(key => caches.delete(key))
            )
        )
    );
    event.waitUntil(self.clients.claim());

    // Notify client(s) of new version
    self.clients.matchAll({ includeUncontrolled: true }).then(clients => {
        for (const client of clients) {
            client.postMessage({ type: 'app-update-available' });
        }
    });
});

self.addEventListener('fetch', event => {
    if (event.request.method !== 'GET') return;

    event.respondWith(
        caches.match(event.request)
            .then(cached => cached || fetch(event.request))
    );
});
