# generate-dynamic-cert.sh (macOS, Linux, Windows)

#!/bin/bash

# Define paths
BASE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CERT_DIR="$BASE_DIR/scripts/certs"
CERT_FILE="oer.local.pem"
KEY_FILE="oer.local-key.pem"
CUSTOM_DOMAINS=("oer.local")  # Add any custom local domains here

# Detect OS
os_type="$(uname -s)"
IP_ADDRESSES=()

case "$os_type" in
    Darwin)
        # macOS
        ip=$(ipconfig getifaddr en0 2>/dev/null)
        [[ -n "$ip" ]] && IP_ADDRESSES+=("$ip")
        ;;
    Linux)
        # Linux
        IP_ADDRESSES+=($(hostname -I 2>/dev/null))
        ;;
    MINGW* | MSYS* | CYGWIN*)
        # Windows (Git Bash or similar)
        ip=$(ipconfig | grep -E "IPv4.*:" | awk -F: '{print $2}' | xargs)
        [[ -n "$ip" ]] && IP_ADDRESSES+=("$ip")
        ;;
    *)
        echo "❌ Unsupported OS: $os_type"
        ;;
esac

# Combine into SAN list
SAN_LIST=("localhost" "${IP_ADDRESSES[@]}" "${CUSTOM_DOMAINS[@]}")

# Create certs directory
mkdir -p "$CERT_DIR"

echo "🔍 Detected IPs: ${IP_ADDRESSES[*]}"
echo "📜 Generating cert for: ${SAN_LIST[*]}"
echo "📂 Output directory: $CERT_DIR"

# Generate cert with mkcert
mkcert -cert-file "$CERT_DIR/$CERT_FILE" -key-file "$CERT_DIR/$KEY_FILE" "${SAN_LIST[@]}"

echo "✅ Certificate generated:"
echo "  - Cert: $CERT_DIR/$CERT_FILE"
echo "  - Key:  $CERT_DIR/$KEY_FILE"
