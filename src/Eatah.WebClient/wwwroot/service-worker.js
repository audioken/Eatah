// Service worker used during `dotnet run` / development. Performs no caching so
// that local changes are always served fresh. The production build uses
// `service-worker.published.js` (renamed to service-worker.js by the Blazor SDK).
self.addEventListener('install', () => self.skipWaiting());
self.addEventListener('activate', () => self.clients.claim());
self.addEventListener('fetch', () => { });

self.addEventListener('push', event => {
    if (!event.data) return;
    const data = event.data.json();
    event.waitUntil(
        self.registration.showNotification(data.title || 'Eatah', {
            body: data.body || '',
            icon: data.icon || '/icons/app-icon.svg',
            badge: data.badge || '/icons/app-icon.svg',
            data: data.data
        })
    );
});

self.addEventListener('notificationclick', event => {
    event.notification.close();
    event.waitUntil(clients.openWindow('/'));
});
