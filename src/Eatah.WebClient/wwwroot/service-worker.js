// Service worker used during `dotnet run` / development. Performs no caching so
// that local changes are always served fresh. The production build uses
// `service-worker.published.js` (renamed to service-worker.js by the Blazor SDK).
self.addEventListener('install', () => self.skipWaiting());
self.addEventListener('activate', () => self.clients.claim());
self.addEventListener('fetch', () => { });
