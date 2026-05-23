// Web Push API helpers called from Blazor via JS interop.
window.PushNotifications = {
    isSupported: function () {
        return typeof Notification !== 'undefined' &&
            'serviceWorker' in navigator &&
            'PushManager' in window;
    },

    getPermission: function () {
        return Notification.permission; // 'default' | 'granted' | 'denied'
    },

    requestPermissionAndSubscribe: async function (vapidPublicKey) {
        const permission = await Notification.requestPermission();
        if (permission !== 'granted') return null;
        return await PushNotifications._subscribe(vapidPublicKey);
    },

    subscribeWithExistingPermission: async function (vapidPublicKey) {
        if (Notification.permission !== 'granted') return null;
        return await PushNotifications._subscribe(vapidPublicKey);
    },

    getExistingSubscription: async function () {
        if (!PushNotifications.isSupported()) return null;
        const reg = await navigator.serviceWorker.ready;
        const sub = await reg.pushManager.getSubscription();
        return sub ? PushNotifications._serializeSub(sub) : null;
    },

    unsubscribe: async function () {
        if (!PushNotifications.isSupported()) return;
        const reg = await navigator.serviceWorker.ready;
        const sub = await reg.pushManager.getSubscription();
        if (sub) await sub.unsubscribe();
    },

    _subscribe: async function (vapidPublicKey) {
        const reg = await navigator.serviceWorker.ready;
        const sub = await reg.pushManager.subscribe({
            userVisibleOnly: true,
            applicationServerKey: PushNotifications._urlBase64ToUint8Array(vapidPublicKey)
        });
        return PushNotifications._serializeSub(sub);
    },

    _serializeSub: function (sub) {
        const json = sub.toJSON();
        return JSON.stringify({
            endpoint: json.endpoint,
            p256dh: json.keys.p256dh,
            auth: json.keys.auth
        });
    },

    _urlBase64ToUint8Array: function (base64String) {
        const padding = '='.repeat((4 - base64String.length % 4) % 4);
        const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
        const rawData = window.atob(base64);
        const output = new Uint8Array(rawData.length);
        for (let i = 0; i < rawData.length; i++) {
            output[i] = rawData.charCodeAt(i);
        }
        return output;
    }
};
