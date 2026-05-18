window.eatahChat = {
    scrollToBottom: function (el) {
        if (el) {
            el.scrollTop = el.scrollHeight;
        }
    },

    _visibilityHandler: null,
    _focusHandler: null,
    _onlineHandler: null,

    // Registers handlers that invoke the .NET callback whenever the app/tab returns
    // to the foreground or regains network. Critical for iOS Safari (PWA) which
    // kills WebSocket connections when the app is backgrounded.
    registerVisibility: function (dotNetRef) {
        this.unregisterVisibility();

        var notify = function (reason) {
            try { dotNetRef.invokeMethodAsync('OnAppResumed', reason); } catch (e) { }
        };

        this._visibilityHandler = function () {
            if (document.visibilityState === 'visible') notify('visibility');
        };
        this._focusHandler = function () { notify('focus'); };
        this._onlineHandler = function () { notify('online'); };

        document.addEventListener('visibilitychange', this._visibilityHandler);
        window.addEventListener('focus', this._focusHandler);
        window.addEventListener('online', this._onlineHandler);

        // Fire once at registration in case the app is already visible.
        if (document.visibilityState === 'visible') notify('initial');
    },

    unregisterVisibility: function () {
        if (this._visibilityHandler) {
            document.removeEventListener('visibilitychange', this._visibilityHandler);
            this._visibilityHandler = null;
        }
        if (this._focusHandler) {
            window.removeEventListener('focus', this._focusHandler);
            this._focusHandler = null;
        }
        if (this._onlineHandler) {
            window.removeEventListener('online', this._onlineHandler);
            this._onlineHandler = null;
        }
    }
};
