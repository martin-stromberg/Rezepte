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
    lastConfirmedUrl = window.location.href;
    safetyTimer = clearTimer(safetyTimer);
    hideTimer = clearTimer(hideTimer);

    hideTimer = setTimeout(function () {
      hideTimer = null;
      deactivate();
    }, hideDelay);
  }

  function isIgnorableInteraction(event) {
    return event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey;
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

  // Shared by handleLinkClick and handleFormSubmit: an element with a foreign target
  // (e.g. target="_blank") navigates a different frame/tab, and a resolved URL pointing
  // to another origin is not something loadingBar can observe completing, so neither
  // case should trigger the animation.
  function resolveSameOriginTarget(element, rawUrl) {
    if (element.target && element.target !== '_self') {
      return null;
    }

    const url = resolveUrl(rawUrl);
    return url && isSameOriginNavigation(url) ? url : null;
  }

  function isFragmentOrCurrentAddress(url) {
    const currentWithoutHash = lastConfirmedUrl.split('#')[0];
    const targetWithoutHash = url.href.split('#')[0];
    return currentWithoutHash === targetWithoutHash;
  }

  function isBarCurrentlyActive() {
    return host.classList.contains(ACTIVE_CLASS);
  }

  // event.defaultPrevented only reflects every handler's decision once event delivery has
  // fully completed. Both loadingBar's own listener and any handler that may call
  // preventDefault() (e.g. an interactive Blazor component) are registered for the same
  // event, so checking defaultPrevented synchronously here would always observe it as false
  // regardless of what those other handlers do. Deferring via setTimeout(..., 0) runs the
  // check after the browser has finished dispatching the event to all listeners, while the
  // event object itself remains valid for that check.
  function startAnimationUnlessPrevented(event) {
    setTimeout(function () {
      if (!event.defaultPrevented) {
        startAnimation();
      }
    }, 0);
  }

  function handleLinkClick(event) {
    if (isIgnorableInteraction(event)) {
      return;
    }

    const anchor = event.target && event.target.closest ? event.target.closest('a[href]') : null;
    if (!anchor) {
      return;
    }

    if (anchor.hasAttribute('download')) {
      return;
    }

    const href = anchor.getAttribute('href') || '';
    if (/^(mailto:|tel:|javascript:)/i.test(href)) {
      return;
    }

    const url = resolveSameOriginTarget(anchor, anchor.href);
    if (!url) {
      return;
    }

    if (!isBarCurrentlyActive() && isFragmentOrCurrentAddress(url)) {
      return;
    }

    startAnimationUnlessPrevented(event);
  }

  // Unlike handleLinkClick, this handler intentionally does not skip same-address targets.
  // Forms very commonly post back to their own current address (e.g. the login form on a
  // validation failure), and that is a real navigation that must be announced, whereas a
  // same-address anchor click typically navigates nowhere.
  function handleFormSubmit(event) {
    const form = event.target;
    if (!(form instanceof HTMLFormElement)) {
      return;
    }

    const submitter = event.submitter;
    const rawAction = (submitter && submitter.getAttribute('formaction')) || form.getAttribute('action') || form.action;

    const url = resolveSameOriginTarget(form, rawAction);
    if (!url) {
      return;
    }

    startAnimationUnlessPrevented(event);
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
