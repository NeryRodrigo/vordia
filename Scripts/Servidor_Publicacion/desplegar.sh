#!/usr/bin/env bash
# =============================================================================
#  VORDIA · Despliegue en el servidor (un solo comando)
#  1) espacio en disco  2) backup de GESCO_DB  3) código (backend y web)
#  4) actualización de la base (03)  5) verificación del esquema (98)
#  6) bitácora semanal (04, opcional)  7) reconstrucción de contenedores  8) salud
#
#  Uso (desde la carpeta del repo vordia en el servidor):
#     bash Scripts/Servidor_Publicacion/desplegar.sh
#     bash Scripts/Servidor_Publicacion/desplegar.sh --con-bitacora   (registra también la bitácora 04)
#
#  No borra datos: el backup queda en /var/opt/mssql/data/GESCO_DB_auto_AAAAMMDD_HHMM.bak
#  (se conservan los 3 backups automáticos más recientes).
# =============================================================================
set -euo pipefail

RAIZ="$(cd "$(dirname "$0")/../.." && pwd)"
WEB="${WEB:-$RAIZ/gesco-web}"
SCRIPTS="$RAIZ/Scripts/Servidor_Publicacion"
CON_BITACORA="${1:-}"
cd "$RAIZ"

paso() { printf '\n\033[1;36m== %s\033[0m\n' "$1"; }
ok()   { printf '\033[1;32m✔ %s\033[0m\n' "$1"; }
error(){ printf '\033[1;31m✘ %s\033[0m\n' "$1"; exit 1; }

docker ps --format '{{.Names}}' | grep -qx gesco-db || error "El contenedor gesco-db no está corriendo"
P="$(docker exec gesco-db printenv MSSQL_SA_PASSWORD)"
SQL=(docker exec gesco-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$P" -C)

paso "1/8 Espacio en disco"
LIBRE_GB=$(df --output=avail -BG / | tail -1 | tr -dc '0-9')
echo "Libre: ${LIBRE_GB} GB"
if [ "$LIBRE_GB" -lt 6 ]; then
  echo "Poco espacio: limpiando caché e imágenes de Docker sin uso (no toca volúmenes ni datos)…"
  docker builder prune -af >/dev/null && docker image prune -af >/dev/null
  LIBRE_GB=$(df --output=avail -BG / | tail -1 | tr -dc '0-9')
  echo "Libre ahora: ${LIBRE_GB} GB"
  [ "$LIBRE_GB" -ge 5 ] || error "Sigue habiendo menos de 5 GB libres: ampliá el disco antes de seguir"
fi
ok "Espacio suficiente"

paso "2/8 Backup de GESCO_DB"
FECHA=$(date +%Y%m%d_%H%M)
"${SQL[@]}" -b -Q "BACKUP DATABASE GESCO_DB TO DISK = N'/var/opt/mssql/data/GESCO_DB_auto_${FECHA}.bak' WITH INIT, COMPRESSION" >/dev/null
docker exec gesco-db bash -c 'ls -1t /var/opt/mssql/data/GESCO_DB_auto_*.bak 2>/dev/null | tail -n +4 | xargs -r rm -f'
ok "Backup GESCO_DB_auto_${FECHA}.bak"

paso "3/8 Código"
git pull --ff-only origin master
[ -d "$WEB/.git" ] || error "No encuentro el repo de la web en $WEB (definí WEB=/ruta/a/gesco-web)"
git -C "$WEB" pull --ff-only origin main
ok "Backend $(git log --oneline -1) · Web $(git -C "$WEB" log --oneline -1)"

paso "4/8 Actualización de la base (03)"
docker cp "$SCRIPTS/03_Actualizacion_Octubre_2026.sql" gesco-db:/tmp/03.sql
SALIDA03=$(mktemp)
if ! "${SQL[@]}" -b -i /tmp/03.sql >"$SALIDA03" 2>&1; then
  tail -20 "$SALIDA03"
  error "El script 03 falló: revisá el error de arriba. Si hace falta volver atrás, está el backup del paso 2"
fi
grep -E "creada|agregada|=== " "$SALIDA03" || true
rm -f "$SALIDA03"
ok "Script 03 aplicado"

paso "5/8 Verificación del esquema (98)"
docker cp "$SCRIPTS/98_Verificar_Esquema_Completo.sql" gesco-db:/tmp/98.sql
RESUMEN=$("${SQL[@]}" -W -s"|" -h -1 -i /tmp/98.sql | tail -1)
echo "Tablas esperadas | faltantes | columnas faltantes: $RESUMEN"
echo "$RESUMEN" | grep -Eq '\|0\|0[[:space:]]*$' || error "Faltan tablas o columnas: corré el 98 a mano para ver cuáles"
ok "Base completa"

paso "6/8 Bitácora semanal (04)"
if [ "$CON_BITACORA" = "--con-bitacora" ] && [ -f "$SCRIPTS/04_Bitacora_Semanas_19oct_1nov_2026.sql" ]; then
  docker cp "$SCRIPTS/04_Bitacora_Semanas_19oct_1nov_2026.sql" gesco-db:/tmp/04.sql
  "${SQL[@]}" -b -W -s"|" -i /tmp/04.sql | sed -n '1,12p'
  ok "Bitácora registrada"
else
  echo "Omitida (usá --con-bitacora para registrarla)"
fi

paso "7/8 Reconstrucción de backend y web"
docker compose up -d --build backend frontend
docker builder prune -af >/dev/null
ok "Contenedores actualizados"

paso "8/8 Salud"
for i in $(seq 1 30); do
  if curl -fsS -m 3 http://localhost:8080/api/System/version >/dev/null 2>&1; then break; fi
  sleep 3
done
curl -fsS -m 5 http://localhost:8080/api/System/version >/dev/null || error "La API no responde: revisá docker logs gesco-api"
ok "API respondiendo"
docker logs gesco-api --tail 5 2>&1 | sed 's/^/   /'
df -h / | tail -1 | awk '{print "   Disco: " $4 " libres (" $5 " usado)"}'
printf '\n\033[1;32mDespliegue terminado.\033[0m Probá www.vordia.com.py con Ctrl + F5.\n'
