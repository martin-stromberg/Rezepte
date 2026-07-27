(function () {
  const HOST_ID = 'loading-bar';
  const ACTIVE_CLASS = 'loading-bar-active';

  function getHost() {
    return document.getElementById(HOST_ID);
  }

  // The host element only exists when LoadingBar:Enabled is true (see LoadingBar.razor).
  // When it is absent the feature is disabled for the whole page lifetime, so no listeners
  // are registered at all. data-permanent guarantees this exact element instance survives
  // every Enhanced Navigation, so it is safe to capture it once and reuse it below instead
  // of re-resolving it on every call.
  const host = getHost();
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
  // Enhanced navigation updates window.location optimistically before the fetch resolves,
  // so same-address detection must compare against the last *confirmed* address instead of
  // the live one; otherwise a second click on the still-loading link would look like a no-op.
  let lastConfirmedUrl = window.location.href;

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

  function clearSafetyTimer() {
    if (safetyTimer) {
      clearTimeout(safetyTimer);
      safetyTimer = null;
    }
  }

  function clearHideTimer() {
    if (hideTimer) {
      clearTimeout(hideTimer);
      hideTimer = null;
    }
  }

  function deactivate() {
    host.classList.remove(ACTIVE_CLASS);
  }

  function startAnimation() {
    clearHideTimer();

    const color = pickColor();
    if (color) {
      host.style.setProperty('--loading-bar-color', color);
    }

    host.classList.remove(ACTIVE_CLASS);
    void host.offsetWidth; // force reflow so the animation restarts
    host.classList.add(ACTIVE_CLASS);

    clearSafetyTimer();
    if (maxVisibleDuration > 0) {
      safetyTimer = setTimeout(function () {
        safetyTimer = null;
        deactivate();
      }, maxVisibleDuration);
    }
  }

  function completeNavigation() {
    lastConfirmedUrl = window.location.href;
    clearSafetyTimer();
    clearHideTimer();

    hideTimer = setTimeout(function () {
      hideTimer = null;
      deactivate();
    }, hideDelay);
  }

  function isIgnorableInteraction(event) {
    return event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey;
  }

  function resolveUrl(rawUrl) {
    try {
      return new URL(rawUrl, window.location.href);
    } catch {
      return null;
    }
  }

  function isSameOriginNavigation(url) {
    return url.origin === window.location.origin;
  }

  function isFragmentOrCurrentAddress(url) {
    const currentWithoutHash = lastConfirmedUrl.split('#')[0];
    const targetWithoutHash = url.href.split('#')[0];
    return currentWithoutHash === targetWithoutHash;
  }

  function isBarCurrentlyActive() {
    return host.classList.contains(ACTIVE_CLASS);
  }

  function handleLinkClick(event) {
    if (isIgnorableInteraction(event)) {
      return;
    }

    const anchor = event.target && event.target.closest ? event.target.closest('a[href]') : null;
    if (!anchor) {
      return;
    }

    if (anchor.target && anchor.target !== '_self') {
      return;
    }

    if (anchor.hasAttribute('download')) {
      return;
    }

    const href = anchor.getAttribute('href') || '';
    if (/^(mailto:|tel:|javascript:)/i.test(href)) {
      return;
    }

    const url = resolveUrl(anchor.href);
    if (!url || !isSameOriginNavigation(url)) {
      return;
    }

    if (!isBarCurrentlyActive() && isFragmentOrCurrentAddress(url)) {
      return;
    }

    startAnimation();
  }

  // Unlike handleLinkClick, this handler intentionally does not skip same-address targets.
  // Forms very commonly post back to their own current address (e.g. the login form on a
  // validation failure), and that is a real navigation that must be announced, whereas a
  // same-address anchor click typically navigates nowhere.
  function handleFormSubmit(event) {
    if (event.defaultPrevented) {
      return;
    }

    const form = event.target;
    if (!(form instanceof HTMLFormElement)) {
      return;
    }

    if (form.target && form.target !== '_self') {
      return;
    }

    const submitter = event.submitter;
    const rawAction = (submitter && submitter.getAttribute('formaction')) || form.getAttribute('action') || form.action;

    const url = resolveUrl(rawAction);
    if (!url || !isSameOriginNavigation(url)) {
      return;
    }

    startAnimation();
  }

  document.addEventListener('click', handleLinkClick, true);
  document.addEventListener('submit', handleFormSubmit, true);
  window.addEventListener('pageshow', completeNavigation);

  if (window.Blazor && typeof window.Blazor.addEventListener === 'function') {
    window.Blazor.addEventListener('enhancedload', completeNavigation);
  } else {
    console.warn('loadingBar: Blazor enhanced navigation events unavailable; falling back to the safety timeout.');
  }
})();
