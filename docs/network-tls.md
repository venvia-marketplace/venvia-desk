# LAN DNS and TLS — 0.4 alpha.1

Desk Server must offer guided hostname/subdomain configuration, certificate issuance, renewal, and local DNS resolution. A unique subdomain of an administrator-authorized domain is preferred; existing custom domains and local-only installations remain supported. Never assume authority over a public DNS zone.

- Publicly trusted certificate: ACME DNS-01 with narrowly scoped DNS credentials or delegated challenge zone. No inbound WAN ports required. Initial issuance and renewal require connectivity; record expiration and alert early. Store credentials and private keys outside Git and web roots with restricted permissions.
- Local DNS: resolve the chosen subdomain to the LAN server address through local DNS; do not depend on external DNS for normal store operations. Validate hostname, certificate chain, SAN, DNS and HTTPS from LAN clients.
- Offline-only installation: offer administrator-controlled local CA and explicit trust enrollment, or a previously provisioned valid certificate. Never disable certificate validation or silently downgrade HTTPS. Public ACME cannot issue without connectivity; expiry is not bypassed.
- Bind database to local socket or loopback. Expose only approved HTTPS endpoints to LAN. No public exposure by default.
- Preserve identity, configuration, renewal state and recoverable keys through encrypted backups and approved migration.
- Test WAN disconnection, DNS resolution, certificate validation, renewal failure, restart, and rollback in staging before release.

Exact hostname naming convention, DNS provider integration, local CA enrollment and Windows/Linux service packaging are pending validation. This document is not executable provisioning code.
