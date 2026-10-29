FROM mcr.microsoft.com/mssql/server:2022-latest

USER root

COPY 001_schema.sql 002_procedures.sql 003_triggers.sql 004_seed.sql /scripts/
COPY entrypoint.sh /entrypoint.sh

RUN chmod +x /entrypoint.sh \
    && chown -R mssql:mssql /scripts \
    && chmod -R a+rX /scripts

USER mssql

ENTRYPOINT ["/entrypoint.sh"]
