# Docker & CI/CD Deployment Guide

UnoWebTemplate comes packaged with a multi-stage `Dockerfile`, a local orchestration setup (`docker-compose.yml`), and a pre-configured GitHub Actions pipeline to publish container images.

---

## 📦 Multi-stage Dockerfile Layout

The [Dockerfile](file:///home/jaret/Documents/GitHub/UnoWebTemplate/Dockerfile) uses a two-stage layout to build the app from source and produce a lightweight runtime container.

### Stage 1: Build & Compile
1. **Installs WASM toolchain AOT dependencies**: Emscripten compilation requires Python, which is installed in the SDK container.
2. **Installs dotnet workload**: Loads `wasm-tools` into the SDK.
3. **Caches NuGet package layers**: Selectively copies configuration files and project definitions to optimize cache hits before pulling dependency restores.
4. **Publishes WASM Client**: Compiles the client app targeting WebAssembly.
5. **Publishes Server**: Publishes the ASP.NET Core server (triggering the Tailwind MSBuild target to download the Linux standalone CLI and compile styles).
6. **Asset Merging**: Creates the `wwwroot` folder in the server output and copies the compiled WASM static files inside it.

### Stage 2: Runtime Environment
Copies the compiled server output from the build stage into a minimal `aspnet:10.0` container, setting the entry point and exposing port `8080` (mapped via `ASPNETCORE_HTTP_PORTS=8080`).

---

## 🐳 Docker Compose Orchestration

The [docker-compose.yml](file:///home/jaret/Documents/GitHub/UnoWebTemplate/docker-compose.yml) orchestrates local deployment:
* Binds port `8080:8080` of the host.
* Sets up a persistent Docker volume `app-data` to preserve the SQLite database file (`/app/data/app.db`).
* Overrides the database connection string environment variable to store the DB file in the volume.

Run the containerized stack locally with:
```bash
docker compose up -d --build
```

---

## 🚀 GitHub Actions CI/CD Pipeline

The `.github/workflows/build-and-publish.yml` pipeline automates continuous integration and image publishing:
* **Trigger Events**: Fires when commits are pushed to the `main` branch or when a release tag is pushed (e.g. `v1.0.0`).
* **Target Registry**: Builds the multi-stage image and publishes it directly to **GitHub Container Registry (GHCR)** at `ghcr.io/your-github-username/unowebtemplate`.
* **Caching**: Uses GitHub actions build cache mechanism to cache Docker build layers (`cache-from` and `cache-to`), reducing subsequent build times.
* **Security & Tokens**: Uses the built-in `GITHUB_TOKEN` secret to log in and write packages, avoiding the need for manual registry token configuration.
* **Automatic Tagging**: Applies the current branch name, git short SHA, or semantic version tag (resolved via `docker/metadata-action`) to the published container image.
