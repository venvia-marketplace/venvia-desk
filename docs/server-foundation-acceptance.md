# VenVia 0.4 — Server Foundation: acceptance criteria

Status: requirements approved by project owner; implementation incomplete.
Desk is Rust + Tauri; Core and all VenVia application plugins are PHP. The local server must not require WordPress. WordPress belongs exclusively to the Web integration.

## Installation ownership
Desk Server provisions PHP with required extensions, MariaDB and Nginx, DNS, HTTPS and the initial Core PHP package. Control Center manages all other PHP plugin installations and official Core updates through staging and rollback. No third-party WordPress plugins except authorized exceptions. No replacement or loss of previous Core 0.3 functions.
The baseline must be an inventoried complete Core 0.3 archive, reviewed for secrets, licenses, Windows paths and platform dependencies. Do not substitute an invented Core or a health-test stub.

## Mandatory functional checks
- PHP: installed version, required extensions, execution of a fixed PHP health probe through Nginx/FastCGI, not merely process existence. Do not expose phpinfo or credentials.
- MariaDB: service state, authenticated least-privilege connection and read-only SELECT 1; optional isolated test schema for migration checks; do not expose database to LAN.
- Nginx: configuration syntax, service state, HTTPS application response and FastCGI integration.
- Core: actual approved package checksum/signature, version, installation/migration, database connection, setup, admin MFA/recovery, Control Center and plugin manager compatibility with baseline 0.3. Test existing API and workflows, backup, staging and rollback.
- DNS/TLS: configurable authorized hostname, local resolution, certificate chain/name/expiry, renewal, no public inbound ports by default. Local offline daily operation; certificate issuance/renewal may require internet.
- Automatic startup and recovery: service ownership, dedicated privileges, collision checks, reboots, no unintended changes to existing installations, backup before upgrades, safe rollback.
- Desk displays local and LAN addresses, exports diagnostics as TXT without secrets, and exposes verified HTTPS Control Center link only when configuration and health checks succeed. Access remains authenticated.
- No distribution until security pipeline and functional tests succeed. Unsigned test artifacts are not an approved production release.

## Repository separation proposal
venvia: documentation and ecosystem release catalog.
venvia-desk: native app, runtime provisioning, Windows/Linux packages.
venvia-core: PHP Core and Control Center.
venvia-<plugin>: independent PHP module repositories.
venvia-web: WordPress integration.
Use independent versions and descriptive commit messages, release notes and CI job summaries. Cross-component compatibility manifest must identify exact approved versions and hashes. Start with Desk and Core, then individual plugins as needed. Preserve old history until migration verified. Public visibility alone is not an open-source license.
