// Simple debug helper: captures click events and posts them to /debug/event
(function () {
    function getElementPath(el) {
        if (!el) return '';
        var path = [];
        while (el && el.nodeType === Node.ELEMENT_NODE) {
            var tag = el.tagName.toLowerCase();
            var id = el.id ? '#' + el.id : '';
            var cls = el.className ? ('.' + el.className.split(/\s+/).join('.')) : '';
            path.unshift(tag + id + cls);
            el = el.parentElement;
        }
        return path.join(' > ');
    }

    function sendDebugEvent(e, metadata) {
        try {
            var payload = {
                eventType: e.type,
                timestamp: new Date().toISOString(),
                path: location.pathname,
                element: getElementPath(e.target),
                tagName: e.target?.tagName,
                text: (e.target && e.target.innerText) ? e.target.innerText.trim().slice(0, 200) : null,
                metadata: metadata || null
            };

            // Console log for quick feedback
            console.debug('[Admin Debug]', payload);

            // Fire-and-forget POST to server-side debug endpoint
            navigator.sendBeacon('/debug/event', JSON.stringify(payload));
        } catch (err) {
            console.warn('debug send failed', err);
        }
    }

    document.addEventListener('click', function (e) {
        // capture most user interactions triggered by clicks (buttons, links, etc.)
        sendDebugEvent(e);
    }, { capture: true, passive: true });

    // Optional: capture form submissions
    document.addEventListener('submit', function (e) {
        sendDebugEvent(e);
    }, { capture: true, passive: true });

    // Expose a helper to manually send debug events from the console if needed
    window.__AdminDebug = {
        send: function (name, details) {
            sendDebugEvent({ type: name, target: document.body }, details);
        }
    };
})();
