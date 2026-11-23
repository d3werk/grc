import base64
import binascii
import hashlib
import hmac
import secrets

_PASSWORD_VERSION = "scrypt-v1"
_SALT_SIZE = 16
_KEY_SIZE = 32
_MAX_PASSWORD_LENGTH = 1024


def _encode(value: bytes) -> str:
    return base64.urlsafe_b64encode(value).decode("ascii")


def _decode(value: str) -> bytes:
    return base64.b64decode(value, altchars=b"-_", validate=True)


def _derive(password: str, salt: bytes) -> bytes:
    return hashlib.scrypt(
        password.encode("utf-8"), salt=salt, n=2**16, r=8, p=2, maxmem=128 * 1024 * 1024, dklen=_KEY_SIZE
    )


def hash_password(password: str) -> str:
    if not 12 <= len(password) <= _MAX_PASSWORD_LENGTH:
        raise ValueError("Das Passwort muss zwischen 12 und 1.024 Zeichen lang sein.")
    salt = secrets.token_bytes(_SALT_SIZE)
    return f"{_PASSWORD_VERSION}${_encode(salt)}${_encode(_derive(password, salt))}"


def verify_password(password: str, stored: str | None) -> bool:
    if len(password) > _MAX_PASSWORD_LENGTH:
        return False
    salt, digest = bytes(_SALT_SIZE), bytes(_KEY_SIZE)
    verified_format = False
    if stored is not None:
        try:
            version, encoded_salt, encoded_digest = stored.split("$", 2)
            parsed_salt, parsed_digest = _decode(encoded_salt), _decode(encoded_digest)
            if version == _PASSWORD_VERSION and len(parsed_salt) == _SALT_SIZE and len(parsed_digest) == _KEY_SIZE:
                salt, digest = parsed_salt, parsed_digest
                verified_format = True
        except (ValueError, binascii.Error):
            pass
    matches = hmac.compare_digest(_derive(password, salt), digest)
    return verified_format and matches


def new_session_token() -> str:
    return secrets.token_urlsafe(32)


def session_token_hash(token: str) -> bytes:
    return hashlib.sha256(token.encode("ascii")).digest()
