window.eatahUi = {
    getElementRect: function (el) {
        if (!el) return null;
        const r = el.getBoundingClientRect();
        return { top: r.top, bottom: r.bottom, left: r.left, right: r.right, width: r.width, height: r.height };
    },

    _notifCloseHandler: null,

    // Registers a document-level click listener that invokes Close on the .NET component
    // when the user clicks outside the notification dropdown.
    registerNotifClose: function (dotNetRef) {
        this.unregisterNotifClose();
        var self = this;
        var handler = function () {
            self.unregisterNotifClose();
            try { dotNetRef.invokeMethodAsync('CloseFromOutside'); } catch (e) { }
        };
        self._notifCloseHandler = handler;
        // Delay by one tick so the click that opened the dropdown doesn't fire this immediately.
        setTimeout(function () {
            document.addEventListener('click', handler);
        }, 0);
    },

    unregisterNotifClose: function () {
        if (this._notifCloseHandler) {
            document.removeEventListener('click', this._notifCloseHandler);
            this._notifCloseHandler = null;
        }
    }
};

window.eatahChat = {
    scrollToBottom: function (el) {
        if (el) {
            el.scrollTop = el.scrollHeight;
        }
    },

    // Scrolls the messages container just enough to reveal the picker popup,
    // but only if the picker would otherwise be hidden below the visible area.
    nudgeForPicker: function (el, pickerHeight) {
        if (!el) return;
        const distanceToBottom = el.scrollHeight - el.scrollTop - el.clientHeight;
        if (distanceToBottom < pickerHeight) {
            el.scrollBy({ top: pickerHeight - distanceToBottom, behavior: 'smooth' });
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
