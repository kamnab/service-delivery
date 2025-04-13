const CACHE_VERSION = 'v1';
const CACHE_NAME = `sdc-cache-${CACHE_VERSION}`;

self.addEventListener('install', event => {
    console.log('[SW] Install');
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    console.log('[SW] Activate');
    event.waitUntil(
        caches.keys().then(keys => {
            return Promise.all(
                keys.filter(k => k !== CACHE_NAME).map(k => caches.delete(k))
            );
        })
    );
    return self.clients.claim();
});

self.addEventListener('fetch', event => {
    if (event.request.method !== 'GET') return;

    event.respondWith(
        caches.match(event.request).then(cachedResponse => {
            return cachedResponse || fetch(event.request)
                .then(response => {
                    return caches.open(CACHE_NAME).then(cache => {
                        cache.put(event.request, response.clone());
                        return response;
                    });
                });
        }).catch(() => {
            // Optional: offline fallback page or image
            return caches.match('/offline.html');
        })
    );
});
