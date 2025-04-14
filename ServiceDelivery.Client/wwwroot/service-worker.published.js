self.importScripts('./service-worker-assets.js');

const CACHE_NAME = 'sdc-cache-v1.3';
const OFFLINE_FALLBACK_PAGE = 'offline.html';
const SPA_FALLBACK_PAGE = 'index.html';
const OFFLINE_URLS = [OFFLINE_FALLBACK_PAGE, SPA_FALLBACK_PAGE];

const ASSETS = (self.assetsManifest?.assets?.map(a => a.url) || [])
    .filter(url => url) // Ensure no nulls
    .concat(OFFLINE_URLS);

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
        (async () => {
            const cacheNames = await caches.keys();
            await Promise.all(
                cacheNames
                    .filter(name => name !== CACHE_NAME)
                    .map(name => caches.delete(name))
            );
            await self.clients.claim();

            const clients = await self.clients.matchAll({ includeUncontrolled: true });
            for (const client of clients) {
                client.postMessage({ type: 'app-update-available' });
            }
        })()
    );
});

self.addEventListener('fetch', event => {
    if (event.request.method !== 'GET') return;

    const { request } = event;

    // Handle API requests with stale-while-revalidate strategy
    if (request.url.includes('/api/') && request.method === 'GET') {
        event.respondWith(
            caches.open(CACHE_NAME).then(async cache => {
                const cachedResponse = await cache.match(request);

                const networkFetch = fetch(request)
                    .then(networkResponse => {
                        // Cache the network response for future requests
                        cache.put(request, networkResponse.clone());

                        // Notify all app clients that new updated data is available
                        self.clients.matchAll({ includeUncontrolled: true }).then(clients => {
                            for (const client of clients) {
                                client.postMessage({
                                    type: 'data-update-available',
                                    url: request.url
                                });
                            }
                        });

                        return networkResponse;
                    })
                    .catch(() => {
                        // Return cached response if network fails
                        return cachedResponse;
                    });

                // Return cached data immediately, while updating in the background
                return cachedResponse || networkFetch;
            })
        );
        return; // Stop further execution, we've handled it above.
    }

    // Handle other GET requests (e.g. static assets, pages) with stale-while-revalidate
    event.respondWith(
        caches.open(CACHE_NAME).then(async cache => {
            const cachedResponse = await cache.match(request);

            const networkFetch = fetch(request)
                .then(networkResponse => {
                    // Cache the network response for future requests
                    cache.put(request, networkResponse.clone());

                    // Notify all app clients that new updated data is available
                    self.clients.matchAll({ includeUncontrolled: true }).then(clients => {
                        for (const client of clients) {
                            client.postMessage({
                                type: 'data-update-available',
                                url: request.url
                            });
                        }
                    });

                    return networkResponse;
                })
                .catch(() => {
                    // Fallback to cached response if network fails
                    if (request.mode === 'navigate') {
                        return cache.match(SPA_FALLBACK_PAGE);
                    }
                    if (request.destination === 'document') {
                        return cache.match(OFFLINE_FALLBACK_PAGE);
                    }
                    return cachedResponse || null;
                });

            // Return cached content immediately, but revalidate in the background
            return cachedResponse || networkFetch;
        })
    );
});

