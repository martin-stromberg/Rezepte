(function () {
  const HOST_ID = 'loading-bar';
  const ACTIVE_CLASS = 'loading-bar-active';

  // The host element only exists when LoadingBar:Enabled is true (see LoadingBar.razor).
  // When it is absent the feature is disabled for the whole page lifetime, so no listeners
  // are registered at all. data-permanent guarantees this exact element instance survives
  // every Enhanced Navigation, so it is safe to capture it once and reuse it below instead
  // of re-resolving it on every call.
  const host = document.getElementById(HOST_ID);
  if (!host) {
    return;
  }

  const colors = (host.dataset.colors || '')
    .split(',')
    .map(function (c) { return c.trim(); })
    .filter(Boolean);
  const hideDelay = parseInt(host.dataset.hideDelay, 10) || 0;
  const maxVisibleDuration = parseInt(host.dataset.maxVisibleDuration, 10) || 0;

  let lastColor = null;
  let hideTimer = null;
  let safetyTimer = null;

  function pickColor() {
    if (colors.length === 0) {
      return null;
    }
    if (colors.length === 1) {
      return colors[0];
    }

    let candidates = colors.filter(function (c) { return c !== lastColor; });
    if (candidates.length === 0) {
      candidates = colors;
    }

    const color = candidates[Math.floor(Math.random() * candidates.length)];
    lastColor = color;
    return color;
  }

  function clearTimer(timerId) {
    if (timerId) {
      clearTimeout(timerId);
    }
    return null;
  }

  function deactivate() {
    host.classList.remove(ACTIVE_CLASS);
  }

  function startAnimation() {
    hideTimer = clearTimer(hideTimer);

    const color = pickColor();
    if (color) {
      host.style.setProperty('--loading-bar-color', color);
    }

    host.classList.remove(ACTIVE_CLASS);
    void host.offsetWidth; // force reflow so the animation restarts
    host.classList.add(ACTIVE_CLASS);

    safetyTimer = clearTimer(safetyTimer);
    if (maxVisibleDuration > 0) {
      safetyTimer = setTimeout(function () {
        safetyTimer = null;
        deactivate();
      }, maxVisibleDuration);
    }
  }

  function completeNavigation() {
    safetyTimer = clearTimer(safetyTimer);
    hideTimer = clearTimer(hideTimer);

    hideTimer = setTimeout(function () {
      hideTimer = null;
      deactivate();
    }, hideDelay);
  }

  // Blazor's enhanced navigation intercepts same-origin link clicks and form submits itself
  // (deciding same-tab vs. new-tab, same-origin vs. cross-origin, download links, etc.) and
  // calls event.preventDefault() for every case it takes over - including cases with no real
  // navigation, such as an interactive component's own @onsubmit:preventDefault handler further
  // up the same event. That makes event.defaultPrevented useless for telling those two cases
  // apart. Blazor's own 'enhancednavigationstart'/'enhancedload' events do not have that
  // ambiguity: they fire exactly when Blazor itself starts and finishes a real enhanced
  // (fetch-based, no full page reload) navigation, which is precisely what this loading bar
  // announces for the common case of a same-app link click.
  console.log('loadingBar: DIAG init Blazor=' + (typeof window.Blazor) + ' url=' + window.location.href);
  if (window.Blazor && typeof window.Blazor.addEventListener === 'function') {
    window.Blazor.addEventListener('enhancednavigationstart', function () { console.log('loadingBar: DIAG enhancednavigationstart'); startAnimation(); });
    window.Blazor.addEventListener('enhancedload', function () { console.log('loadingBar: DIAG enhancedload'); completeNavigation(); });
  } else {
    console.warn('loadingBar: Blazor enhanced navigation events unavailable; enhanced-navigation feedback will not be shown.');
  }

  // Not every navigation goes through Blazor's enhanced navigation: an interactive component
  // calling NavigationManager.NavigateTo() from server-side code (e.g. the navbar search form)
  // performs a genuine full-page navigation (a real "document" request), not a fetch. The
  // browser's own 'beforeunload' fires exactly when such a real navigation away from this page
  // is about to happen - and, crucially, it does NOT fire for target="_blank"/new-tab clicks,
  // downloads, mailto:/tel: links, or a submit that an interactive component fully handles
  // itself (e.g. @onsubmit:preventDefault with no navigation), because none of those unload the
  // page. That makes it the correct, unambiguous complement to 'enhancednavigationstart' without
  // needing any of this script's own click/submit interception or origin/target checks.
  window.addEventListener('beforeunload', function () { console.log('loadingBar: DIAG beforeunload'); startAnimation(); });

  // Covers a full (non-enhanced) page load or a back/forward restore from the browser's
  // back-forward cache: in both cases 'enhancedload' does not fire for this page instance,
  // so the bar must be reset on its own to avoid staying stuck active.
  window.addEventListener('pageshow', function () { console.log('loadingBar: DIAG pageshow'); completeNavigation(); });
})();
