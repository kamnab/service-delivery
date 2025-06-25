#!/bin/bash

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

# Display detected IPs and SAN list
echo "🔍 Detected IPs: ${IP_ADDRESSES[*]}"
echo "📜 Generating cert for: ${SAN_LIST[*]}"

