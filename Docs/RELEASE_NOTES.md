# Release Notes

## Important Notes Before Update

- There are no special notices.

## What's New

- Scheduled review workflows (`.github/workflows/review-architecture-security.yml` monthly, `review-nuget-packages.yml` weekly, `review-compliance.yml` yearly) create deduplication-guarded review issues whose `@softwarewerkstatt` mention triggers the external analysis automation; they run on `main` only after promotion.
- The installation guide (`Docs/install.md`) was reworked and now documents two alternative deployment modes — standalone (direct port binding) and behind a Caddy reverse proxy (external TLS termination, `PathBase` path prefix, `X-Forwarded-*` headers) — including a dedicated `rezepte` service account, a hardened systemd unit, firewall and verification steps, plus an update and backup path.

## Wichtige Hinweise vor dem Update

- Es gibt keine besonderen Hinweise.

## Neuerungen

- Zeitgesteuerte Review-Workflows (`.github/workflows/review-architecture-security.yml` monatlich, `review-nuget-packages.yml` wöchentlich, `review-compliance.yml` jährlich) erzeugen duplikatgeschützte Review-Issues, deren `@softwarewerkstatt`-Erwähnung die externe Analyse-Automatik anstößt; sie laufen erst nach der Promotion auf `main`.
- Die Installationsanleitung (`Docs/install.md`) wurde überarbeitet und beschreibt nun zwei alternative Betriebsarten — eigenständig (direkte Port-Bindung) und hinter einem Caddy-Reverse-Proxy (externe TLS-Terminierung, `PathBase`-Pfadpräfix, `X-Forwarded-*`-Header) — inklusive dediziertem `rezepte`-Service-Account, gehärteter systemd-Unit, Firewall- und Verifikationsschritten sowie einem Update- und Backup-Pfad.
