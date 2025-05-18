#!/bin/bash

# Define paths
BASE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CERT_DIR="$BASE_DIR/scripts/certs"
CERT_FILE="oer.local.pem"
KEY_FILE="oer.local-key.pem"
CUSTOM_DOMAINS=("oer.local")  # Add any custom local domains here

# Detect OS and get the host's IP addresses
get_ip_addresses() {
    case "$(uname -s)" in
        Darwin)
            # macOS: Get IP address from en0 (Wi-Fi or Ethernet)
            ip=$(ipconfig getifaddr en0 2>/dev/null)
            [[ -n "$ip" ]] && echo "$ip"
            ;;
        Linux)
            # Linux: Get all available IPs
            hostname -I
            ;;
        MINGW* | MSYS* | CYGWIN*)
            # Windows (Git Bash or similar): Extract the IPv4 address
            ip=$(ipconfig | grep -E "IPv4.*:" | awk -F: '{print $2}' | xargs)
            [[ -n "$ip" ]] && echo "$ip"
            ;;
        *)
            echo "❌ Unsupported OS: $(uname -s)"
            exit 1
            ;;
    esac
}

# Get IP addresses and combine with custom domains
IP_ADDRESSES=($(get_ip_addresses))
SAN_LIST=("localhost" "${IP_ADDRESSES[@]}" "${CUSTOM_DOMAINS[@]}")

# Create certs directory if it doesn't exist
mkdir -p "$CERT_DIR"

# Display detected IPs and SAN list
echo "🔍 Detected IPs: ${IP_ADDRESSES[*]}"
echo "📜 Generating cert for: ${SAN_LIST[*]}"
echo "📂 Output directory: $CERT_DIR"

# Generate the certificate with mkcert
mkcert -cert-file "$CERT_DIR/$CERT_FILE" -key-file "$CERT_DIR/$KEY_FILE" "${SAN_LIST[@]}"

# Confirmation of certificate generation
echo "✅ Certificate generated:"
echo "  - Cert: $CERT_DIR/$CERT_FILE"
echo "  - Key:  $CERT_DIR/$KEY_FILE"
