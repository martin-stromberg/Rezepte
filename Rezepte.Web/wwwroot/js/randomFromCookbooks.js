window.randomFromCookbooks = (function () {
  const handlers = {};

  function getGap(el) {
    try {
      const list = el.querySelector('.random-list');
      const cs = list ? getComputedStyle(list) : getComputedStyle(el);
      const gap = parseFloat(cs.gap || cs.rowGap || 16);
      return isNaN(gap) ? 16 : gap;
    } catch {
      return 16;
    }
  }

  return {
    getVisibleColumns: function (containerId, minItemWidth) {
      const el = document.getElementById(containerId);
      if (!el) return 1;
      const width = el.clientWidth || el.offsetWidth || document.documentElement.clientWidth;
      const gap = getGap(el);
      const cols = Math.max(1, Math.floor((width + gap) / (minItemWidth + gap)));
      return cols;
    },

    registerResizeHandler: function (dotNetRef, containerId, minItemWidth) {
      const handler = debounce(function () {
        dotNetRef.invokeMethodAsync('OnBrowserResize').catch(console.error);
      }, 200);

      window.addEventListener('resize', handler);
      handlers[containerId] = handler;
    },

    unregisterResizeHandler: function (containerId) {
      const h = handlers[containerId];
      if (h) {
        window.removeEventListener('resize', h);
        delete handlers[containerId];
      }
    }
  };

  function debounce(fn, wait) {
    let t;
    return function () {
      clearTimeout(t);
      t = setTimeout(fn, wait);
    };
  }
})();