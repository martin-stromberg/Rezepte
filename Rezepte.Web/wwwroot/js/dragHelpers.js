window.dragHelpers = {
    register: function (selector, dotNetRef) {
        try {
            console && console.debug && console.debug("dragHelpers.register", selector);

            // register dragstart on matching elements
            const nodes = document.querySelectorAll(selector);
            console && console.debug && console.debug("dragHelpers: found nodes", nodes.length);
            nodes.forEach(el => {
                if (el.__dragHelpersRegistered) return;
                el.__dragHelpersRegistered = true;

                el.addEventListener('dragstart', function (ev) {
                    const id = el.getAttribute('data-id') ?? el.dataset.id;
                    console && console.debug && console.debug("dragstart on", id);
                    if (!id || !ev.dataTransfer) return;
                    try {
                        ev.dataTransfer.setData('text/plain', id);
                        ev.dataTransfer.effectAllowed = 'move';
                    } catch (err) {
                        console && console.warn && console.warn("dragHelpers: setData failed", err);
                    }
                }, { passive: true });
            });

            // ensure document-level dragover to allow drops anywhere
            if (!window.__dragHelpersDragOverRegistered) {
                window.__dragHelpersDragOverRegistered = true;
                document.addEventListener('dragover', function (ev) {
                    // necessary in many browsers to allow drop
                    ev.preventDefault();
                }, false);
                console && console.debug && console.debug("dragHelpers: registered document dragover");
            }

            // ensure single document-level drop handler that notifies .NET
            if (!window.__dragHelpersDropRegistered) {
                window.__dragHelpersDropRegistered = true;
                document.addEventListener('drop', function (ev) {
                    try {
                        ev.preventDefault();
                        const src = ev.dataTransfer && ev.dataTransfer.getData ? ev.dataTransfer.getData('text/plain') : null;
                        // find target element under pointer
                        const targetEl = document.elementFromPoint(ev.clientX, ev.clientY);
                        let tgt = null;
                        let node = targetEl;
                        while (node) {
                            if (node && node.getAttribute && node.getAttribute('data-id')) {
                                tgt = node.getAttribute('data-id');
                                break;
                            }
                            node = node && node.parentElement;
                        }
                        console && console.debug && console.debug("dragHelpers.drop event, src:", src, "tgt:", tgt);

                        // call back into .NET if dotNetRef provided
                        if (dotNetRef && typeof dotNetRef.invokeMethodAsync === 'function') {
                            dotNetRef.invokeMethodAsync('OnJsDrop', src, tgt).catch(e => console && console.warn && console.warn("dragHelpers.invoke failed", e));
                        } else if (window.__dotNetDropHandler) {
                            // fallback global
                            try { window.__dotNetDropHandler(src, tgt); } catch (e) { console && console.warn && console.warn("dragHelpers.global handler failed", e); }
                        }
                    } catch (ex) {
                        console && console.warn && console.warn('dragHelpers.drop handler failed', ex);
                    }
                }, false);
                console && console.debug && console.debug("dragHelpers: registered document drop");
            }
        } catch (e) {
            console && console.warn && console.warn("dragHelpers.register failed", e);
        }
    }
};

document.querySelectorAll('.cookbook-card-wrapper').forEach(e => console.log(e.dataset.id));