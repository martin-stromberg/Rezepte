window.auth = {
  login: async function (payload) {
    const res = await fetch('/api/session/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'same-origin',
      body: JSON.stringify(payload)
    });
    if (!res.ok) {
      try {
        const err = await res.json();
        throw new Error(err?.message || 'Login failed');
      } catch {
        throw new Error('Login failed');
      }
    }
  }
};
