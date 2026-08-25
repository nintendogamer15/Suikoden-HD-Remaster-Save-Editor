# Security policy

## Supported versions

Until the first release, only the current `main` branch is supported. After releases begin, the latest published version and `main` will receive save-integrity and local-security fixes.

## Reporting

Do not attach a real or decrypted save to a public issue. Report vulnerabilities through GitHub's private security-advisory feature after the repository is published. Before publication, contact the repository owner through a private channel they designate. Include a synthetic reproducer when possible.

High-priority issues include output corruption, incomplete backup recovery, path traversal or unintended file replacement, unsafe temporary-file handling, encryption incompatibility, secret/private-data disclosure, unexpected network activity, and a bypass of game/field validation.

## Data and threat model

The application is offline, processes files locally, stores recent paths but not contents, and performs no telemetry. Saves remain untrusted input: the application caps behavior through fixed schema checks, strict cryptographic/UTF-8/JSON parsing, reviewed adapter paths, and write revalidation. It is not a sandbox for maliciously enormous JSON and should open only saves from trusted sources.

Backups are unencrypted beyond the game's own save encryption and may contain personal in-game names. Protect them like the original saves. Steam Cloud synchronization is outside the application's control.
