import os
from dataclasses import dataclass
from functools import lru_cache


@dataclass(frozen=True)
class Settings:
    database_url: str
    bootstrap_token: str | None = None
    session_ttl_seconds: int = 8 * 60 * 60

    def __post_init__(self) -> None:
        if not self.database_url:
            raise RuntimeError("DATABASE_URL is required")
        if self.session_ttl_seconds < 1 or self.session_ttl_seconds > 30 * 24 * 60 * 60:
            raise RuntimeError("GRC_SESSION_TTL_SECONDS must be between 1 second and 30 days")


@lru_cache(maxsize=1)
def get_settings() -> Settings:
    return Settings(
        database_url=os.environ.get("DATABASE_URL", ""),
        bootstrap_token=os.environ.get("GRC_BOOTSTRAP_TOKEN") or None,
        session_ttl_seconds=int(os.environ.get("GRC_SESSION_TTL_SECONDS", "28800")),
    )
