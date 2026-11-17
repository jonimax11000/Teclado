#!/usr/bin/env bash
# PreToolUse: Unity serializa .meta/.unity/.prefab con GUIDs; editarlos a mano rompe referencias en silencio.
f=$(jq -r '.tool_input.file_path // empty')
if [[ "$f" =~ \.(meta|unity|prefab)$ || "$f" == */Library/* ]]; then
  echo "Bloqueado: $f lo gestiona Unity. Haz el cambio en el Editor (o deja que Unity genere el .meta al importar)." >&2
  exit 2
fi
exit 0
