import hashlib
from collections.abc import Iterator
from contextlib import contextmanager
from pathlib import Path
from typing import TYPE_CHECKING, Any

import psycopg
from psycopg.rows import dict_row
from psycopg.types.json import Jsonb

if TYPE_CHECKING:
    from .tenancy import RequestContext

Connection = psycopg.Connection[dict[str, Any]]
MIGRATIONS = Path(__file__).resolve().parent.parent / "migrations"


@contextmanager
def transaction(database_url: str) -> Iterator[Connection]:
    with psycopg.connect(database_url, row_factory=dict_row, connect_timeout=5) as connection:
        with connection.transaction():
            login = connection.execute(
                "SELECT rolsuper, rolbypassrls FROM pg_roles WHERE rolname = session_user"
            ).fetchone()
            if login is None or login["rolsuper"] or login["rolbypassrls"]:
                raise RuntimeError("The API requires a dedicated PostgreSQL login without BYPASSRLS")
            connection.execute("SET LOCAL ROLE grc_runtime")
            yield connection


def migrate(database_url: str, migrations_directory: Path = MIGRATIONS) -> None:
    files = sorted(migrations_directory.glob("[0-9][0-9][0-9]_*.sql"))
    if not files:
        raise RuntimeError("No platform database migrations found")

    with psycopg.connect(database_url, row_factory=dict_row, connect_timeout=5) as connection:
        with connection.transaction():
            connection.execute("SELECT pg_advisory_xact_lock(5219451268512902839)")
            connection.execute(
                """
                CREATE TABLE IF NOT EXISTS schema_migrations (
                    filename text PRIMARY KEY,
                    sha256 text NOT NULL,
                    applied_at timestamptz NOT NULL DEFAULT now()
                )
                """
            )
            applied = {
                row["filename"]: row["sha256"]
                for row in connection.execute("SELECT filename, sha256 FROM schema_migrations")
            }
            if applied.keys() - {file.name for file in files}:
                raise RuntimeError("The database contains an unknown platform migration")

            for file in files:
                source = file.read_bytes()
                checksum = hashlib.sha256(source).hexdigest()
                if file.name in applied:
                    if applied[file.name] != checksum:
                        raise RuntimeError(f"Applied platform migration was modified: {file.name}")
                    continue
                connection.execute(source.decode("utf-8"))
                connection.execute(
                    "INSERT INTO schema_migrations (filename, sha256) VALUES (%s, %s)", (file.name, checksum)
                )


def audit_event(
    ctx: "RequestContext",
    action: str,
    entity_type: str,
    entity_id: object,
    details: dict[str, Any] | None = None,
    scope: str = "",
) -> None:
    ctx.conn.execute(
        """
        INSERT INTO audit_events (tenant_id, actor_id, action, entity_type, entity_id, details, scope)
        VALUES (%s, %s, %s, %s, %s, %s, %s)
        """,
        (ctx.tenant_id, ctx.user_id, action, entity_type, str(entity_id), Jsonb(details or {}), scope),
    )
