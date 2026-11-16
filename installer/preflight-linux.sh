#!/usr/bin/env bash
# VenVia Desk 0.4 alpha.1: read-only Linux preflight.
set -u
export LC_ALL=C
printf 'VenVia Desk — Linux preflight\n'
if [ "$(uname -s)" != Linux ]; then echo 'Unsupported OS'; exit 2; fi
if [ -r /etc/os-release ]; then grep -E '^(ID|VERSION_ID)=' /etc/os-release; fi
printf 'Architecture: '; uname -m
printf 'Memory (kB): '; awk '/^MemTotal:/ {print $2}' /proc/meminfo
printf 'Available disk (KiB): '; df -Pk . | awk 'NR==2 {print $4}'
printf 'Effective UID: '; id -u
for cmd in php php-fpm mariadb mysql nginx apache2 httpd systemctl ss; do
  if command -v "$cmd" >/dev/null 2>&1; then printf '%s: present\n' "$cmd"; else printf '%s: not found in PATH\n' "$cmd"; fi
done
if command -v ss >/dev/null 2>&1; then
  echo 'Existing TCP listeners on candidate ports:'
  ss -H -ltn | awk '$4 ~ /:(80|443|3306)$/'
else echo 'Port inspection unavailable; manual review required'; fi
if command -v systemctl >/dev/null 2>&1; then
  for name in nginx apache2 mariadb mysql; do
    printf '%s: ' "$name"; systemctl is-active "$name" 2>/dev/null || true
  done
fi
echo 'Preflight inventory only; no installation or compatibility approval performed.'
