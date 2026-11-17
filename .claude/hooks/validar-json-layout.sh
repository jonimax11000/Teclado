#!/usr/bin/env bash
# PostToolUse: un JSON roto en Resources solo falla en runtime dentro del visor VR.
f=$(jq -r '.tool_input.file_path // empty')
case "$f" in
  */Assets/Resources/*.json)
    if ! out=$(jq empty "$f" 2>&1); then
      echo "JSON inválido en $f: $out" >&2
      exit 2
    fi
    ;;
esac
exit 0
