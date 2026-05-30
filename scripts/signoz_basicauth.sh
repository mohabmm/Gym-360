#!/usr/bin/env bash
# Generate a Traefik basic-auth credential for the SigNoz UI edge gate.
#
# Prints the htpasswd line (user:bcrypt-hash) to paste into the Dokploy
# Traefik middleware for the signoz domain. The PASSWORD is a secret — this
# script only prints the hash; nothing is committed to git.
#
# Usage:
#   scripts/signoz_basicauth.sh                       # user defaults to mohab_31_8@hotmail.com, prompts for password
#   SIGNOZ_AUTH_USER='admin' scripts/signoz_basicauth.sh
set -euo pipefail

USER_NAME="${SIGNOZ_AUTH_USER:-mohab_31_8@hotmail.com}"

read -r -s -p "Password for ${USER_NAME}: " PW; echo
[ -n "$PW" ] || { echo "empty password"; exit 1; }
read -r -s -p "Confirm: " PW2; echo
[ "$PW" = "$PW2" ] || { echo "passwords do not match"; exit 1; }

# bcrypt hash via htpasswd (preferred) or the official httpd image as a fallback.
if command -v htpasswd >/dev/null 2>&1; then
  LINE="$(htpasswd -nbB "$USER_NAME" "$PW")"
elif command -v docker >/dev/null 2>&1; then
  LINE="$(docker run --rm httpd:2.4-alpine htpasswd -nbB "$USER_NAME" "$PW")"
else
  echo "need htpasswd or docker to generate the bcrypt hash"; exit 1
fi

echo
echo "── htpasswd line (user:hash) ─────────────────────────────────────────"
echo "$LINE"
echo
echo "── Dokploy: add to the SigNoz domain as a Traefik basic-auth middleware ──"
echo "If pasting raw Traefik labels, DOUBLE every \$ to \$\$ (compose escaping):"
echo "$LINE" | sed 's/\$/\$\$/g'
echo
echo "Steps:"
echo "  1. Dokploy → signoz app → Domains/Advanced → add a Traefik 'basicauth' middleware"
echo "     with the users line above, and attach it to the signoz router."
echo "  2. Reload. Hitting https://signoz.gym-365.com now prompts for ${USER_NAME}."
