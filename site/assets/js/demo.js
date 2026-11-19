// Start/stop control for the animated demo tour on the Rezepte presentation
// page. The tour never autoplays: index.html shows the static preview image
// and carries the animated variant in data-animated-src; the toggle button is
// hidden in the markup and only becomes visible here. With
// prefers-reduced-motion: reduce the button stays hidden and nothing ever
// animates; without JavaScript the static preview is all that renders.
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var button = document.getElementById('demo-toggle');
        var image = button ? document.querySelector('img[data-animated-src]') : null;
        if (!button || !image) {
            return;
        }

        var motionQuery = window.matchMedia ? window.matchMedia('(prefers-reduced-motion: reduce)') : null;
        var staticSrc = image.getAttribute('src');
        var animatedSrc = image.getAttribute('data-animated-src');
        var playing = false;

        function setPlaying(next) {
            playing = next;
            image.setAttribute('src', next ? animatedSrc : staticSrc);
            button.textContent = next ? 'Demo-Animation stoppen' : 'Demo-Animation starten';
        }

        function syncAvailability() {
            // Reduced motion: stop a running tour and keep the button hidden so
            // it does not appear as a dead control.
            var reduced = !!(motionQuery && motionQuery.matches);
            if (reduced && playing) {
                setPlaying(false);
            }
            button.hidden = reduced;
        }

        button.addEventListener('click', function () {
            setPlaying(!playing);
        });
        if (motionQuery && motionQuery.addEventListener) {
            motionQuery.addEventListener('change', syncAvailability);
        }
        syncAvailability();
    });
})();
