FROM node:24-bookworm-slim AS frontend
WORKDIR /work
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

FROM python:3.12-slim-bookworm AS application
ENV PYTHONDONTWRITEBYTECODE=1 PYTHONUNBUFFERED=1 \
    GRC_FRONTEND_DIR=/opt/grc/web GRC_HTTP_HOST=0.0.0.0
WORKDIR /opt/grc/backend
COPY backend/pyproject.toml backend/uv.lock ./
RUN pip install --no-cache-dir uv==0.13.6 && uv sync --locked --no-dev --no-install-project
COPY backend/app ./app
COPY backend/migrations ./migrations
COPY --from=frontend /work/dist /opt/grc/web
RUN addgroup --system grc && adduser --system --ingroup grc --no-create-home grc \
    && chmod -R a+rX app migrations /opt/grc/web
USER grc
EXPOSE 8000
ENTRYPOINT ["/opt/grc/backend/.venv/bin/python", "-m", "app.deployment"]
CMD ["serve"]
