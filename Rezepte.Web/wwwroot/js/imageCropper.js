// Robust JS-Interop for Cropper.js
// - checks global UMD variant
// - fallback: dynamic import of ESM build if global not present
// - registers onload/onerror before setting src
// Ergänze aussagekräftige Logs (nur Debug, später entfernen)
window.imageCropper = (function () {

    async function resolveCropperCtor() {
        console.debug("imageCropper: resolveCropperCtor start");
        if (typeof window.Cropper === "function") { console.debug("imageCropper: found window.Cropper"); return window.Cropper; }
        if (window.Cropper && typeof window.Cropper.default === "function") { console.debug("imageCropper: found window.Cropper.default"); return window.Cropper.default; }
        try {
            console.debug("imageCropper: trying dynamic import cropper.esm.js");
            const module = await import('https://unpkg.com/cropperjs@1.5.13/dist/cropper.esm.js');
            console.debug("imageCropper: dynamic import result:", module);
            return module.default ?? module.Cropper;
        } catch (e) {
            console.warn("imageCropper: dynamic import failed", e);
        }
        console.debug("imageCropper: resolveCropperCtor end -> null");
        return null;
    }

    // init liefert Promise<boolean>
    function init(imgId, dataUrl) {
        console.debug("imageCropper.init called for", imgId);
        return new Promise(async (resolve) => {
            const img = document.getElementById(imgId);
            if (!img) { console.error("imageCropper.init: image element not found", imgId); return resolve(false); }
            // destroy previous if any
            if (img._cropper) { try { img._cropper.destroy(); } catch {} img._cropper = null; }

            // Attach handlers before setting src so onload/onerror always fire
            img.onload = async function () {
                console.debug("imageCropper: img.onload for", imgId);
                try {
                    const Ctor = await resolveCropperCtor();
                    console.debug("imageCropper: resolved ctor", !!Ctor);
                    if (!Ctor) return resolve(false);
                    img._cropper = new Ctor(img, { viewMode:1, autoCropArea:1 });
                    console.debug("imageCropper: cropper instance created for", imgId);
                    return resolve(true);
                } catch (ex) {
                    console.error("imageCropper: init failed", ex);
                    return resolve(false);
                }
            };

            img.onerror = function (ev) { console.error("imageCropper: img.onerror", ev, imgId); return resolve(false); };
            // trigger load
            try { img.src = dataUrl; } catch (ex) { console.error("imageCropper: setting src failed", ex); return resolve(false); }
        });
    }

    async function getCroppedDataUrl(imgId, maxWidth = 1600, maxHeight = 1600, timeoutMs = 8000) {
        const img = document.getElementById(imgId);
        if (!img || !img._cropper) return null;

        return new Promise((resolve) => {
            let timedOut = false;
            const t = setTimeout(() => {
                timedOut = true;
                console.warn("getCroppedDataUrl: timeout");
                resolve(null);
            }, timeoutMs);

            (async () => {
                try {
                    const cropBox = img._cropper.getCropBoxData();
                    const options = {};
                    const scale = Math.min(1, maxWidth / cropBox.width || 1, maxHeight / cropBox.height || 1);
                    if (scale < 1) {
                        options.width = Math.round(cropBox.width * scale);
                        options.height = Math.round(cropBox.height * scale);
                    }

                    const canvas = img._cropper.getCroppedCanvas(options);
                    if (!canvas) {
                        if (!timedOut) { clearTimeout(t); resolve(null); }
                        return;
                    }

                    canvas.toBlob(function (blob) {
                        if (timedOut) return;
                        if (!blob) { clearTimeout(t); resolve(null); return; }

                        const reader = new FileReader();
                        reader.onloadend = function () {
                          if (timedOut) return;
                          clearTimeout(t);
                          resolve(reader.result); // dataURL string
                        };
                        reader.onerror = function (err) {
                          if (timedOut) return;
                          clearTimeout(t);
                          console.error("FileReader error", err);
                          resolve(null);
                        };
                        reader.readAsDataURL(blob);
                      }, "image/jpeg", 0.9);
                } catch (ex) {
                    if (!timedOut) { clearTimeout(t); console.error("getCroppedDataUrl error", ex); resolve(null); }
                }
            })();
        });
    }

    function destroy(imgId) {
        const img = document.getElementById(imgId);
        if (img && img._cropper) {
            try { img._cropper.destroy(); } catch { }
            img._cropper = null;
        }
    }

    // Ergänzung: uploadCroppedBlob — lädt das zugeschnittene Blob direkt per fetch zum Server.
    // Vermeidet große Base64‑Transfers über SignalR (Blazor Server) und verhindert Verbindungsabbrüche.
    async function uploadCroppedBlob(imgId, uploadUrl, fileName = "photo.jpg", token = null, quality = 0.9, maxWidth = 1600, maxHeight = 1600, timeoutMs = 15000) {
        const img = document.getElementById(imgId);
        if (!img || !img._cropper) return { ok: false, status: 0 };

        return new Promise((resolve) => {
            let timedOut = false;
            const t = setTimeout(() => {
                timedOut = true;
                console.warn("uploadCroppedBlob: timeout");
                resolve({ ok: false, status: 0 });
            }, timeoutMs);

            (async () => {
                try {
                    const cropBox = img._cropper.getCropBoxData();
                    const options = {};
                    const scale = Math.min(1, maxWidth / cropBox.width || 1, maxHeight / cropBox.height || 1);
                    if (scale < 1) {
                        options.width = Math.round(cropBox.width * scale);
                        options.height = Math.round(cropBox.height * scale);
                    }

                    const canvas = img._cropper.getCroppedCanvas(options);
                    if (!canvas) {
                        if (!timedOut) { clearTimeout(t); resolve({ ok: false, status: 0 }); }
                        return;
                    }

                    canvas.toBlob(async function (blob) {
                        if (timedOut) return;
                        if (!blob) { clearTimeout(t); resolve({ ok: false, status: 0 }); return; }

                        try {
                          const form = new FormData();
                          form.append("file", blob, fileName);

                          const headers = {};
                          if (token) headers['Authorization'] = 'Bearer ' + token;

                          const resp = await fetch(uploadUrl, {
                            method: "POST",
                            body: form,
                            credentials: "same-origin",
                            headers: headers
                          });

                          if (!timedOut) { clearTimeout(t); resolve({ ok: resp.ok, status: resp.status }); }
                        } catch (e) {
                          if (!timedOut) { clearTimeout(t); console.error("uploadCroppedBlob fetch failed", e); resolve({ ok: false, status: 0 }); }
                        }
                      }, "image/jpeg", quality);
                } catch (ex) {
                  if (!timedOut) { clearTimeout(t); console.error("uploadCroppedBlob error", ex); resolve({ ok: false, status: 0 }); }
                }
            })();
        });
    }

    return {
        init,
        getCroppedDataUrl,
        destroy,
        uploadCroppedBlob
    };
})();