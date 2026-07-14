# Cross-Platform Uno/WASM/Tailwind Template Blueprint

This document details the architecture, file configurations, and scripts needed to build a generic, premium GitHub Template Repository. It uses **Uno Platform (WASM, Windows, Skia Desktop)** for the frontend, **ASP.NET Core Web API** for the backend (hosting the compiled WASM client), **Tailwind CSS v4** (with a Node-free MSBuild auto-download chain), **Entity Framework Core (SQLite local, SQL Server ready)**, and **Docker + GitHub Actions** for automated builds and container publishing.

---

## 📁 Repository Structure

When creating the repository, structure the files as follows:

```text
├── .github/
│   └── workflows/
│       └── build-and-publish.yml  # GitHub Actions CI/CD to build/publish Docker images
├── .gitignore                     # Standard .NET + IDE ignore file
├── Directory.Build.props          # Global build configurations
├── global.json                    # SDK and Uno.Sdk version locking
├── TemplateApp.slnx               # Modern XML-based .NET Solution file
├── Dockerfile                     # Multi-stage production container build
├── docker-compose.yml             # Orchestration for local container runs
├── rename-template.sh             # Utility script to rename the template namespaces
│
├── TemplateApp.Shared/
│   ├── TemplateApp.Shared.csproj  # Pure Class Library for shared DTOs & Interfaces
│   └── Models/                    # Shared data structures
│
├── TemplateApp.Client/
│   ├── Directory.Packages.props   # Central package management for the Client
│   ├── TemplateApp.Client.sln     # Sub-solution for Uno Single-Project IDE loading
│   └── TemplateApp.Client/        # WinUI 3 / Uno Multi-targeted Client App
│       ├── TemplateApp.Client.csproj
│       ├── App.xaml / App.xaml.cs
│       └── MainPage.xaml
│
└── TemplateApp.Server/
    ├── TemplateApp.Server.csproj  # ASP.NET Core API server hosting the Client
    ├── Program.cs                 # App bootstrap, DbConfig, Static Files & Endpoints
    ├── Styles/
    │   └── app.css                # Tailwind CSS v4 source file (using @import "tailwindcss")
    └── wwwroot/                   # Output for compiled Tailwind CSS and fallback page
```

---

## 🛠️ Root Configuration Files

### `global.json`
Lock the SDK and Uno versions to ensure reproducibility.
```json
{
  "msbuild-sdks": {
    "Uno.Sdk": "6.5.36"
  },
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature",
    "allowPrerelease": false
  }
}
```

### `TemplateApp.slnx`
Modern, clean Solution XML format for newer IDEs (VS 2022, Rider, VS Code C# Dev Kit).
```xml
<Solution>
  <Folder Name="/Solution Items/">
    <File Path="global.json" />
    <File Path="Dockerfile" />
    <File Path="docker-compose.yml" />
    <File Path="rename-template.sh" />
  </Folder>
  <Project Path="TemplateApp.Shared/TemplateApp.Shared.csproj" />
  <Project Path="TemplateApp.Client/TemplateApp.Client/TemplateApp.Client.csproj" />
  <Project Path="TemplateApp.Server/TemplateApp.Server.csproj" />
</Solution>
```

---

## 🎨 Node-Free Tailwind CSS v4 Setup

Instead of checking in a massive (100MB+) pre-compiled binary or requiring developers to install Node.js/npm, we use MSBuild to detect the developer's operating system/CPU architecture and download the exact Tailwind CLI binary directly from GitHub.

### `TemplateApp.Server/TemplateApp.Server.csproj`
Configure the `TailwindCss` target to download and compile the CSS before building:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <!-- Prevent assemblies clash with WASM target fallback -->
    <AssetTargetFallback>$(AssetTargetFallback);net8.0-browser1.0</AssetTargetFallback>
    <StaticWebAssetsFingerprintingEnabled>false</StaticWebAssetsFingerprintingEnabled>
  </PropertyGroup>

  <!-- Tailwind Standalone CLI Auto-Download Configuration -->
  <PropertyGroup>
    <TailwindVersion>4.0.0</TailwindVersion>
    <TailwindPlatform Condition="$([MSBuild]::IsOSPlatform('Windows'))">windows</TailwindPlatform>
    <TailwindPlatform Condition="$([MSBuild]::IsOSPlatform('OSX'))">macos</TailwindPlatform>
    <TailwindPlatform Condition="$([MSBuild]::IsOSPlatform('Linux'))">linux</TailwindPlatform>
    
    <TailwindArch>$([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLower())</TailwindArch>
    <TailwindArch Condition="'$(TailwindArch)' == 'x64'">x64</TailwindArch>
    <TailwindArch Condition="'$(TailwindArch)' == 'arm64'">arm64</TailwindArch>
    
    <TailwindExeExtension Condition="$([MSBuild]::IsOSPlatform('Windows'))">.exe</TailwindExeExtension>
    <TailwindExeName>tailwindcss-$(TailwindPlatform)-$(TailwindArch)$(TailwindExeExtension)</TailwindExeName>
    <TailwindUrl>https://github.com/tailwindlabs/tailwindcss/releases/download/v$(TailwindVersion)/$(TailwindExeName)</TailwindUrl>
    <TailwindLocalExe>$(ProjectDir)tailwindcss$(TailwindExeExtension)</TailwindLocalExe>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="DotNetEnv" Version="3.2.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.8" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.8">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\TemplateApp.Shared\TemplateApp.Shared.csproj" />
  </ItemGroup>

  <!-- Auto-download Tailwind CLI binary if missing -->
  <Target Name="DownloadTailwind" BeforeTargets="TailwindCss">
    <DownloadFile SourceUrl="$(TailwindUrl)" DestinationFolder="$(ProjectDir)" DestinationFileName="tailwindcss$(TailwindExeExtension)" Condition="!Exists('$(TailwindLocalExe)')" />
    <Exec Command="chmod +x &quot;$(TailwindLocalExe)&quot;" Condition="!$([MSBuild]::IsOSPlatform('Windows')) And Exists('$(TailwindLocalExe)')" />
  </Target>

  <!-- Compile styles using Tailwind CSS v4 CLI -->
  <Target Name="TailwindCss" BeforeTargets="BeforeBuild;ResolveStaticWebAssetsInputs">
    <Exec Command="&quot;$(TailwindLocalExe)&quot; -i Styles/app.css -o wwwroot/app.css --minify" />
  </Target>

</Project>
```

### `TemplateApp.Server/Styles/app.css`
Tailwind v4 is configuration-free. Use standard CSS `@theme` variables directly in your source CSS file:
```css
@import "tailwindcss";

@theme {
  --color-brand-primary: #3b82f6;
  --color-brand-dark: #0f172a;
}

@layer base {
  body {
    @apply bg-brand-dark text-slate-100 antialiased;
  }
}
```

---

## 🖥️ Server Bootstrapping & Client WASM Hosting

The ASP.NET Core server acts as both the Web API backend and the static file server hosting the compiled Uno WASM Client.

### `TemplateApp.Server/Program.cs`
Setting up clean routes, SPA routing fallbacks for WASM, and running EF Core migrations on start.
```csharp
using Microsoft.EntityFrameworkCore;
using DotNetEnv;

// Load environmental variables (needed for docker / local config splits)
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// DB Setup - Defaults to SQLite but dynamically shifts to SQL Server/Postgres if configured
var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING") 
    ?? "Data Source=app.db";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (connectionString.Contains("Host=") || connectionString.Contains("Port="))
    {
        // Add PostgreSQL support if connection string matches
        // options.UseNpgsql(connectionString);
    }
    else if (connectionString.Contains("Server="))
    {
        // options.UseSqlServer(connectionString);
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});

// Configure CORS for local hot-reload
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Run DB migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseCors();
app.UseHttpsRedirection();

// 1. Host the compiled WebAssembly Static Assets
app.UseStaticFiles();

// 2. Sample API Endpoints
var api = app.MapGroup("/api");
api.MapGet("/status", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTimeOffset.UtcNow }));

// 3. Fallback Route: Direct SPA routes to Uno WASM index.html
app.MapFallbackToFile("index.html");

app.Run();
```

---

## 📦 Containerization (Dockerfile & Docker Compose)

A production-ready, multi-stage build structure. It installs Python (required by Emscripten for WebAssembly AOT/WASM compilation), restores NuGet dependencies, compiles the Client, compiles Tailwind CSS, and packages everything into a lightweight runtime container.

### `Dockerfile`
```dockerfile
# Stage 1: Build & Compile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Emscripten WASM toolchain requires Python
RUN apt-get update && apt-get install -y --no-install-recommends \
    python3 \
    python-is-python3 \
    && rm -rf /var/lib/apt/lists/*

# Install WASM workloads for Uno Platform WebAssembly builds
RUN dotnet workload install wasm-tools

# Copy global props and package structures
COPY global.json ./
COPY TemplateApp.slnx ./

# Copy Client targets/Central Package configuration
COPY TemplateApp.Client/Directory.Build.props TemplateApp.Client/
COPY TemplateApp.Client/Directory.Packages.props TemplateApp.Client/
COPY TemplateApp.Client/TemplateApp.Client.sln TemplateApp.Client/

# Copy csproj definitions for cacheable dependency restore
COPY TemplateApp.Shared/TemplateApp.Shared.csproj TemplateApp.Shared/
COPY TemplateApp.Client/TemplateApp.Client/TemplateApp.Client.csproj TemplateApp.Client/TemplateApp.Client/
COPY TemplateApp.Server/TemplateApp.Server.csproj TemplateApp.Server/

RUN dotnet restore TemplateApp.Client/TemplateApp.Client/TemplateApp.Client.csproj
RUN dotnet restore TemplateApp.Server/TemplateApp.Server.csproj

# Copy remaining source code files
COPY TemplateApp.Shared/ TemplateApp.Shared/
COPY TemplateApp.Client/ TemplateApp.Client/
COPY TemplateApp.Server/ TemplateApp.Server/

# Publish Client (WebAssembly target)
RUN dotnet publish TemplateApp.Client/TemplateApp.Client/TemplateApp.Client.csproj \
    -c Release -f net10.0-browserwasm -o /app/client-publish

# Publish Server (ASP.NET Core API Backend)
# (This step downloads the Linux Tailwind CSS binary and compiles styles automatically)
RUN dotnet publish TemplateApp.Server/TemplateApp.Server.csproj \
    -c Release -o /app/server-publish

# Merge compiled WASM static site into Server's wwwroot hosting folder
RUN mkdir -p /app/server-publish/wwwroot && \
    cp -rv /app/client-publish/wwwroot/* /app/server-publish/wwwroot/

# Stage 2: Minimal Runtime Container
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/server-publish .

# Expose Kestrel hosting port
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "TemplateApp.Server.dll"]
```

### `docker-compose.yml`
```yaml
services:
  app:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: template-app-service
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - DATABASE_CONNECTION_STRING=Data Source=/app/data/app.db
    volumes:
      - app-data:/app/data
    restart: unless-stopped

volumes:
  app-data:
```

---

## 🚀 CI/CD Pipeline (GitHub Actions)

This pipeline builds the Docker image and publishes it to **GitHub Container Registry (GHCR)** on pushes to the `main` branch or tag creation.

### `.github/workflows/build-and-publish.yml`
```yaml
name: Build and Publish Container Image

on:
  push:
    branches: [ "main" ]
    tags: [ "v*.*.*" ]
  workflow_dispatch:

env:
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  build-and-push:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Log in to the Container registry
        uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Extract metadata (tags, labels) for Docker
        id: meta
        uses: docker/metadata-action@v5
        with:
          images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}
          tags: |
            type=semver,pattern={{version}}
            type=ref,event=branch
            type=sha,format=short

      - name: Set up QEMU
        uses: docker/setup-qemu-action@v3

      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v3

      - name: Build and push Docker image
        uses: docker/build-push-action@v6
        with:
          context: .
          push: true
          tags: ${{ id.meta.outputs.tags }}
          labels: ${{ id.meta.outputs.labels }}
          cache-from: type=gha
          cache-to: type=gha,mode=max
```

---

## 🔄 Renaming Utility Script

To make the template easy to consume, include a shell script in the root that recursively renames the directories, file names, and content namespace references to the user's custom app name.

### `rename-template.sh`
```bash
#!/usr/bin/env bash

# Exit immediately if any command fails
set -euo pipefail

if [ "$#" -ne 1 ]; then
    echo "Usage: $0 <NewProjectName>"
    echo "Example: $0 MyAwesomeDashboard"
    exit 1
fi

NEW_NAME=$1
OLD_NAME="TemplateApp"

echo "🔄 Renaming template from '${OLD_NAME}' to '${NEW_NAME}'..."

# 1. Rename files and directories matching "TemplateApp"
# We process directories from deepest to shallowest using depth order
find . -depth -name "*${OLD_NAME}*" | while read -r path; do
    dir=$(dirname "$path")
    base=$(basename "$path")
    new_base="${base//$OLD_NAME/$NEW_NAME}"
    mv "$path" "$dir/$new_base"
done

# 2. Replace occurrences of "TemplateApp" inside text files
# Excludes .git folder and the rename script itself
find . -type f \
    -not -path '*/.git/*' \
    -not -path '*/bin/*' \
    -not -path '*/obj/*' \
    -not -name 'rename-template.sh' \
    -not -name 'tailwindcss' \
    -exec sed -i "s/$OLD_NAME/$NEW_NAME/g" {} +

echo "✅ Rename complete! Build project with: dotnet build"
```
*(Note: Run `chmod +x rename-template.sh` to make the script executable.)*
