#!/bin/bash
# generate-dev-cert.sh

mkdir -p Infrastructure/Resources/https

mkcert -pkcs12 -p12-file Infrastructure/Resources/https/profile-service.pfx localhost 100.42.176.103 odi-profile.codemie.dev 127.0.0.1 ::1

echo "✅ Trusted HTTPS cert generated at Infrastructure/Resources/https/profile-service.pfx"

# ------ CAUTION
# Run this script on your host, not in Docker:
# chmod +x generate-dev-cert.sh
# ./generate-dev-cert.sh
# ------

# ------ CAUTION
# 2. Make sure your .pfx is truly passwordless:
# If it asks for a password, it's not passwordless.
# If it shows info directly or succeeds with an empty password, it's fine.
# openssl pkcs12 -in Infrastructure/Resources/https/profile-service.pfx -info
# openssl pkcs12 -in profile-service.pfx -info
# ------ 