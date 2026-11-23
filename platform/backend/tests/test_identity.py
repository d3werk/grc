import os
import time
from collections.abc import Iterator
from datetime import UTC, datetime, timedelta
from typing import Annotated
from uuid import uuid4

import pytest
from fastapi import Depends, FastAPI
from fastapi.testclient import TestClient

from app.config import Settings, get_settings
from app.security import hash_password, new_session_token, session_token_hash, verify_password
from app.tenancy import RequestContext, context, router

_BOOTSTRAP_TOKEN = "synthetic-identity-test-bootstrap-token"
_PASSWORD = "Synthetic test password for IAM"


def test_password_hashes_use_independent_salts_and_tokens_are_opaque():
    stored = hash_password(_PASSWORD)
    assert stored != hash_password(_PASSWORD)
    assert _PASSWORD not in stored
    assert verify_password(_PASSWORD, stored)
    assert not verify_password("incorrect password", stored)
    assert not verify_password(_PASSWORD, None)
    token = new_session_token()
    assert token != new_session_token()
    assert len(session_token_hash(token)) == 32


@pytest.fixture
def identity_client() -> Iterator[TestClient]:
    database_url = os.environ.get("GRC_TEST_DATABASE_URL")
    if not database_url:
        pytest.skip("GRC_TEST_DATABASE_URL must reference a migrated PostgreSQL test database")

    app = FastAPI()
    app.include_router(router)
    app.dependency_overrides[get_settings] = lambda: Settings(database_url, _BOOTSTRAP_TOKEN)

    @app.get("/test/rls")
    def tenant_rls(ctx: Annotated[RequestContext, Depends(context("records.read"), scope="function")]):
        return ctx.conn.execute("SELECT slug FROM tenants").fetchall()

    @app.get("/test/scope")
    def scoped_read(scope: str, ctx: Annotated[RequestContext, Depends(context("records.read"), scope="function")]):
        ctx.require_scope(scope)
        return ctx.conn.execute(
            "SELECT grc_scope_allowed(%s) AS current_grant, grc_scope_allowed(%s, 'reviews.read') AS review_grant",
            (scope, scope),
        ).fetchone()

    with TestClient(app) as client:
        yield client


def _bootstrap(client: TestClient, *, password: str = _PASSWORD) -> dict:
    slug = "identity-" + uuid4().hex
    payload = {"slug": slug, "name": f"Test {slug}", "admin_email": "admin@example.test", "admin_password": password}
    assert client.post("/api/v1/tenants", json=payload).status_code == 403
    response = client.post("/api/v1/tenants", json=payload, headers={"X-Bootstrap-Token": _BOOTSTRAP_TOKEN})
    assert response.status_code == 201, response.text
    return response.json()


def _login(client: TestClient, slug: str, email: str = "admin@example.test", password: str = _PASSWORD) -> dict[str, str]:
    response = client.post("/api/v1/auth/login", json={"tenant_slug": slug, "email": email, "password": password})
    assert response.status_code == 200, response.text
    assert response.json()["token_type"] == "bearer"
    return {"Authorization": f"Bearer {response.json()['access_token']}"}


def test_identity_is_selected_by_the_session_and_logout_revokes_the_token(identity_client: TestClient):
    first = _bootstrap(identity_client)
    second = _bootstrap(identity_client, password="Different synthetic tenant password")
    authentication = _login(identity_client, first["slug"], email="ADMIN@example.test")
    denied = identity_client.post("/api/v1/auth/login", json={
        "tenant_slug": second["slug"], "email": "admin@example.test", "password": _PASSWORD,
    })
    assert denied.status_code == 401
    assert identity_client.get("/api/v1/auth/me").status_code == 401
    forged_headers = {**authentication, "X-Tenant-ID": second["id"]}
    assert identity_client.get("/api/v1/auth/me", headers=forged_headers).json()["tenant_id"] == first["id"]
    assert identity_client.get("/test/rls", headers=forged_headers).json() == [{"slug": first["slug"]}]
    assert identity_client.post("/api/v1/auth/logout", headers=authentication).status_code == 204
    assert identity_client.get("/api/v1/auth/me", headers=authentication).status_code == 401


def test_advisor_mandate_is_scope_specific_and_revocable_with_an_open_session(identity_client: TestClient):
    tenant = _bootstrap(identity_client)
    admin = _login(identity_client, tenant["slug"])
    new_member = identity_client.post("/api/v1/members", json={
        "email": "advisor@example.test", "password": _PASSWORD, "role": "advisor",
    }, headers=admin)
    assert new_member.status_code == 201, new_member.text
    advisor_id = new_member.json()["id"]
    advisor = _login(identity_client, tenant["slug"], email="advisor@example.test")
    expiry = (datetime.now(UTC) + timedelta(days=1)).isoformat()
    grant = {"advisor_id": advisor_id, "scope": "Nord", "permissions": ["records.read"], "expires_at": expiry}
    assert identity_client.get("/test/scope", params={"scope": "Nord"}, headers=advisor).status_code == 403
    assert identity_client.get("/api/v1/members", headers=advisor).status_code == 403
    assert identity_client.post("/api/v1/advisor-mandates", headers=admin, json={**grant, "permissions": ["iam.manage"]}).status_code == 422
    assert identity_client.post("/api/v1/advisor-mandates", headers=admin, json={**grant, "permissions": ["records.write"]}).status_code == 422

    created = identity_client.post("/api/v1/advisor-mandates", json=grant, headers=admin)
    assert created.status_code == 201, created.text
    other = identity_client.post("/api/v1/advisor-mandates", headers=admin, json={
        **grant, "scope": "Süd", "permissions": ["reviews.read"],
    })
    assert other.status_code == 201, other.text
    access = identity_client.get("/test/scope", params={"scope": "Nord"}, headers=advisor)
    assert access.json() == {"current_grant": True, "review_grant": False}
    assert identity_client.get("/test/scope", params={"scope": "Süd"}, headers=advisor).status_code == 403
    assert identity_client.post(f"/api/v1/advisor-mandates/{created.json()['id']}/revoke", headers=admin).status_code == 204
    assert identity_client.get("/test/scope", params={"scope": "Nord"}, headers=advisor).status_code == 403
    assert identity_client.get("/api/v1/auth/me", headers=advisor).json()["permissions"] == ["reviews.read"]


def test_sessions_expire(identity_client: TestClient):
    tenant = _bootstrap(identity_client)
    original = identity_client.app.dependency_overrides[get_settings]()
    identity_client.app.dependency_overrides[get_settings] = lambda: Settings(
        original.database_url, _BOOTSTRAP_TOKEN, session_ttl_seconds=1
    )
    authentication = _login(identity_client, tenant["slug"])
    time.sleep(1.1)
    assert identity_client.get("/api/v1/auth/me", headers=authentication).status_code == 401
