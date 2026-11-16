# VenVia Desk 0.4 — alpha.1

Status: design/bootstrap; not a production release.

VenVia Desk Server prepares and supervises the local runtime: PHP, MariaDB, web server and subsequently WordPress as the hosting layer for VenVia Core. Desk clients access the local server over LAN using HTTPS; no direct database access from client machines. Store operations must remain available without WAN connectivity. Existing Core, modules, roles and Control Center workflows remain unchanged.

## Bootstrap requirements
- Guided preflight: OS, architecture, disk, RAM, ports, existing services and connectivity.
- Never overwrite existing PHP, database or web server installations; detect conflicts and support explicit reuse after validation.
- Provision a supported PHP runtime with required extensions, MariaDB with a dedicated least-privilege application account, and a web server bound to localhost during setup. LAN access only after explicit secure configuration.
- Keep credentials, database files and backups outside public web roots and Git. No default password. Do not expose MariaDB to LAN or Internet.
- Verify runtime versions, database connectivity, PHP execution, service restart and offline operation before installing Core.
- Preserve Control Center backup, staging and rollback. Separate installation, update and removal; never delete business data automatically.
- Windows and Linux are targets; exact runtime packaging and native service managers remain to be validated. Existing Ubuntu DEV uses Nginx, PHP-FPM and MariaDB; use that as a first integration target, not a mandatory platform.
- Release artifacts: versioned ZIP, checksums, install/rollback instructions and test evidence.

## Next implementation
1. Read and reconcile the approved 0.4 manual.
2. Implement read-only preflight for Windows and Linux.
3. Implement idempotent provisioning behind explicit approval, then validate in clean and existing-server environments.
4. Add guided first setup and Core installation, LAN TLS and offline tests.

Version choices, packaging strategy and open-source license are pending explicit validation.
