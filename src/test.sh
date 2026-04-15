#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${1:-http://localhost:5266}"
PASS=0
FAIL=0
GUID_RE='[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}'

check() {
    local label="$1" expected="$2" actual="$3"
    if [[ "$actual" == *"$expected"* ]]; then
        echo "  PASS: $label"
        ((++PASS))
    else
        echo "  FAIL: $label (expected: $expected)"
        ((++FAIL))
    fi
}

request() {
    local accept="$1"
    curl -s -D- -H "Accept: $accept" "$BASE_URL"
}

headers() { echo "$1" | sed '/^\r$/q'; }
body()    { echo "$1" | sed '1,/^\r$/d'; }

# --- Content negotiation tests ---
# Format: accept_header | expected_content_type | body_check
#   body_check: "html" = check for DOCTYPE, "guid" = bare GUID, "json" = JSON keys
tests=(
    "text/html|text/html|html"
    "application/json|application/json|json"
    "text/plain|text/plain|guid"
    "|text/plain|guid"
)

for test in "${tests[@]}"; do
    IFS='|' read -r accept content_type body_check <<< "$test"
    label="${accept:-no Accept header}"
    echo "=> Accept: $label"

    RESP=$(request "$accept")
    check "Content-Type" "$content_type" "$(headers "$RESP")"

    BODY=$(body "$RESP")
    case "$body_check" in
        html) check "HTML page"  "<!DOCTYPE html>" "$BODY" ;;
        json) for key in '"n"' '"d"' '"b"' '"p"' '"motd"'; do
                  check "JSON key $key" "$key" "$BODY"
              done ;;
        guid) check "bare GUID" "$(echo "$BODY" | grep -oE "$GUID_RE")" "$BODY" ;;
    esac
    echo
done

# --- robots.txt ---
echo "=> robots.txt"
RESP=$(curl -s -D- "$BASE_URL/robots.txt")
check "Content-Type" "text/plain" "$(headers "$RESP")"
BODY=$(body "$RESP")
check "User-agent" "User-agent: *" "$BODY"
check "Allow" "Allow: /" "$BODY"
echo

# --- Uniqueness ---
echo "=> Uniqueness"
G1=$(curl -s -H "Accept: text/plain" "$BASE_URL" | tr -d '[:space:]')
G2=$(curl -s -H "Accept: text/plain" "$BASE_URL" | tr -d '[:space:]')
if [[ "$G1" != "$G2" ]]; then
    echo "  PASS: different GUIDs ($G1 vs $G2)"
    ((++PASS))
else
    echo "  FAIL: same GUID ($G1)"
    ((++FAIL))
fi
echo

# --- Summary ---
echo "========================="
echo "Results: $PASS passed, $FAIL failed"
[[ $FAIL -eq 0 ]] && echo "All tests passed!" || exit 1
