// Theme toggle for the Rezepte presentation page.
// Persists the choice in localStorage; without a stored choice the page falls
// back to prefers-color-scheme (handled by CSS and the inline head script).
(function () {
    'use strict';

    var STORAGE_KEY = 'theme';

    function currentTheme() {
        var theme = document.documentElement.getAttribute('data-theme');
        if (theme === 'light' || theme === 'dark') {
            return theme;
        }
        return (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) ? 'dark' : 'light';
    }

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        updateToggle(theme);
    }

    function updateToggle(theme) {
        var button = document.getElementById('theme-toggle');
        if (!button) {
            return;
        }
        var dark = theme === 'dark';
        // The accessible name stays constant ("Dunkelmodus") while aria-pressed
        // carries the state, so name and pressed state can never contradict each
        // other (no "In den hellen Modus wechseln, gedrückt"). The title still
        // announces the next action for sighted users.
        button.setAttribute('aria-pressed', dark ? 'true' : 'false');
        button.setAttribute('aria-label', 'Dunkelmodus');
        button.setAttribute('title', dark ? 'In den hellen Modus wechseln' : 'In den dunklen Modus wechseln');
    }

    document.addEventListener('DOMContentLoaded', function () {
        var button = document.getElementById('theme-toggle');
        if (!button) {
            return;
        }
        updateToggle(currentTheme());
        button.addEventListener('click', function () {
            var next = currentTheme() === 'dark' ? 'light' : 'dark';
            try {
                window.localStorage.setItem(STORAGE_KEY, next);
            } catch (e) {
                // Storage unavailable (e.g. blocked cookies); the theme still applies for this visit.
            }
            applyTheme(next);
        });
    });
})();
