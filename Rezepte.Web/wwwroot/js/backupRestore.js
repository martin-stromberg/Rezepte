window.backupRestore = {
    uploadFileToApi: async function (inputId, uploadUrl) {
        try {
            const input = document.getElementById(inputId);
            if (!input || !input.files || input.files.length === 0) {
                console.error("backupRestore: no file selected");
                return false;
            }

            // request short-lived bearer token from server (cookie auth required)
            const tokenResp = await fetch('/api/session/token', { credentials: 'same-origin' });
            if (!tokenResp.ok) {
                console.error('backupRestore: could not obtain token', tokenResp.status);
                return false;
            }
            const tokenJson = await tokenResp.json();
            const token = tokenJson?.token;
            if (!token) {
                console.error('backupRestore: token response invalid');
                return false;
            }

            // Ensure uploadUrl is absolute (root-based)
            let resolvedUrl = uploadUrl;
            if (!/^https?:\/\//i.test(uploadUrl) && !uploadUrl.startsWith("/")) {
                resolvedUrl = location.origin + "/" + uploadUrl.replace(/^\/+/, "");
            }

            const file = input.files[0];
            const form = new FormData();
            form.append("file", file, file.name);

            console.debug("backupRestore: uploading to", resolvedUrl, file.name);

            const resp = await fetch(resolvedUrl, {
                method: "POST",
                body: form,
                credentials: "same-origin",
                headers: {
                    "Authorization": "Bearer " + token
                }
            });

            if (!resp.ok) {
                const text = await resp.text().catch(() => "<no body>");
                console.error("backupRestore: upload failed", resp.status, text);
                return false;
            }

            return true;
        } catch (err) {
            console.error("uploadFileToApi error:", err);
            return false;
        }
    }
};