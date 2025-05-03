#!/bin/bash

# Set your desired domain names and IPs
HOSTNAMES=("localhost" "127.0.0.1" "ddt-oer.local")
IPS=($(hostname -I))
CERT_NAME="ddt-oer-cert"

# Combine SANs
SAN_LIST=("${HOSTNAMES[@]}" "${IPS[@]}")
echo "Generating cert for:"
printf " - %s\n" "${SAN_LIST[@]}"

# Create output directory
mkdir -p ./nginx/certs
cd ./nginx/certs || exit 1

# Generate certificate with mkcert
mkcert -cert-file "$CERT_NAME.crt" -key-file "$CERT_NAME.key" "${SAN_LIST[@]}"

echo "✅ Certificate created:"
echo " - $(pwd)/$CERT_NAME.crt"
echo " - $(pwd)/$CERT_NAME.key"
