# VenVia Desk — Macro 1
Version: 0.4.0-alpha.4. Windows first; Linux later.
Rust and Tauri 2 are exclusively for the native desktop shell and infrastructure management. VenVia Core and ALL VenVia plugins remain PHP. MariaDB, Nginx and WordPress hosting architecture remain unchanged.
## Installation and validation
Download the successful Windows Actions artifact for this version, extract it, compare the EXE SHA-256 with SHA256SUMS.txt, then run the installer on a test Windows 11 machine. Launch VenVia Desk from Start. Check CPU, RAM, disk, loopback port bind and process inventory. Select server or client, save, close and reopen to confirm persistence. Disconnect WAN and repeat diagnostics. Uninstall via Windows Settings > Apps; user settings may remain. No production data or services are created by this macro.
The bundled WebView2 offline installer is intended to avoid a runtime download at installation. Build and packaging still require validation. The installer is not yet Authenticode signed. Do not disable Windows security policies.
## Scope
No automatic PHP/MariaDB/Nginx installation, database connection, DNS, TLS, fiscal operations or Core deployment in this macro. Port checks are momentary bind probes, not guaranteed availability; process inventory does not include stopped services. Mode selection is persisted configuration only.
## Next macro
Service provisioning through a separately authorized, least-privilege native service; verified versioned packages, conflict detection, backup and rollback. Domain, DNS, TLS and PHP ecosystem integration follow. Preserve legacy scripts only as historical references; don't run them for this release.
## Build
From src-tauri, with Rust stable and Tauri CLI 2, use cargo test then cargo tauri build --bundles nsis. The CI generates a temporary generic application icon. Windows build tools and WebView2 build dependencies are required. Dependency lockfile and reproducibility hardening are pending. License selection pending; public visibility alone is not an open-source license.
