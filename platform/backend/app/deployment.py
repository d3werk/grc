"""Container commands for migration and serving with separate database roles."""

import os
import sys

import psycopg
from psycopg import sql
from psycopg.conninfo import make_conninfo
import uvicorn

from .db import migrate


def connection_url(user: str, password: str) -> str:
    return make_conninfo(
        host=os.environ.get("GRC_DB_HOST", "db"),
        port=os.environ.get("GRC_DB_PORT", "5432"),
        dbname=os.environ.get("GRC_DB_NAME", "grc"),
        user=user,
        password=password,
    )


def migration() -> None:
    admin_url = os.environ.get("GRC_MIGRATION_DATABASE_URL") or connection_url(
        os.environ.get("GRC_DB_ADMIN_USER", "grc_owner"), os.environ["GRC_DB_ADMIN_PASSWORD"]
    )
    password = os.environ["GRC_DB_RUNTIME_PASSWORD"]
    if len(password) < 24:
        raise ValueError("Das Kennwort für die Anwendungsdatenbank muss mindestens 24 Zeichen haben.")
    runtime_user = os.environ.get("GRC_DB_RUNTIME_USER", "grc_api")
    migrate(admin_url)
    with psycopg.connect(admin_url) as connection:
        session_role = connection.execute("SELECT session_user").fetchone()
        if session_role is not None and runtime_user == session_role[0]:
            raise ValueError("Migration und Anwendung brauchen getrennte Datenbankrollen.")
        exists = connection.execute(
            "SELECT rolsuper, rolbypassrls, pg_has_role(rolname, 'grc_runtime', 'MEMBER') "
            "FROM pg_roles WHERE rolname = %s", (runtime_user,)
        ).fetchone()
        if exists and (exists[0] or exists[1] or not exists[2]):
            raise ValueError("Die vorgesehene Anwendungsrolle ist bereits anderweitig vergeben.")
        action = sql.SQL("ALTER ROLE") if exists else sql.SQL("CREATE ROLE")
        connection.execute(sql.SQL("{} {} LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT "
                                   "NOREPLICATION NOBYPASSRLS PASSWORD {}").format(
            action, sql.Identifier(runtime_user), sql.Literal(password)
        ))
        connection.execute(sql.SQL("GRANT grc_runtime TO {}").format(sql.Identifier(runtime_user)))


def serve() -> None:
    if not os.environ.get("DATABASE_URL"):
        os.environ["DATABASE_URL"] = connection_url(
            os.environ.get("GRC_DB_RUNTIME_USER", "grc_api"), os.environ["GRC_DB_RUNTIME_PASSWORD"]
        )
    uvicorn.run("app.main:app", host=os.environ.get("GRC_HTTP_HOST", "127.0.0.1"),
                port=int(os.environ.get("GRC_HTTP_PORT", "8000")))


if __name__ == "__main__":
    commands = {"migrate": migration, "serve": serve}
    if len(sys.argv) != 2 or sys.argv[1] not in commands:
        raise SystemExit("python -m app.deployment {migrate|serve}")
    commands[sys.argv[1]]()
