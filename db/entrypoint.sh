#!/bin/bash
set -eo pipefail

echo "Iniciando SQL Server en segundo plano..."
/opt/mssql/bin/sqlservr &
SQLPID=$!

SQLCMD=""
for c in /opt/mssql-tools18/bin/sqlcmd /opt/mssql-tools/bin/sqlcmd; do
  if [ -x "$c" ]; then SQLCMD="$c"; break; fi
done
if [ -z "$SQLCMD" ]; then
  echo "ERROR: sqlcmd no encontrado en la imagen." >&2
  kill "$SQLPID" 2>/dev/null || true
  exit 1
fi

run_sqlcmd() {
  if "$SQLCMD" -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -I -b "$@" </dev/null; then
    return 0
  fi
  "$SQLCMD" -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -I -b "$@" </dev/null
}

echo "Esperando a que SQL Server acepte conexiones..."
READY=0
for i in $(seq 1 120); do
  if run_sqlcmd -Q "SELECT 1" -o /dev/null >/dev/null 2>&1; then
    READY=1
    break
  fi
  if ! kill -0 "$SQLPID" 2>/dev/null; then
    echo "ERROR: sqlservr termino antes de estar listo." >&2
    exit 1
  fi
  sleep 1
done

if [ "$READY" != "1" ]; then
  echo "ERROR: SQL Server no acepto conexiones en 120 s." >&2
  kill "$SQLPID" 2>/dev/null || true
  exit 1
fi

echo "SQL Server listo. Creando base de datos (si no existe)..."
run_sqlcmd -Q "IF DB_ID('Ingrata') IS NULL CREATE DATABASE Ingrata;"
run_sqlcmd -Q "ALTER DATABASE Ingrata SET QUOTED_IDENTIFIER ON;" -o /dev/null

for f in /scripts/*.sql; do
  echo "Aplicando $f ..."
  if ! run_sqlcmd -d Ingrata -i "$f"; then
    echo "ERROR: fallo al aplicar $f" >&2
    kill "$SQLPID" 2>/dev/null || true
    exit 1
  fi
done

echo "Esquema aplicado correctamente."
touch /tmp/ingrata-scripts-done

wait "$SQLPID"
