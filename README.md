# UnoWebTemplate

UnoWebTemplate is a clean, multi-targeted boilerplate template designed to build modern cross-platform web and desktop applications. It integrates an **Uno Platform (WASM, Windows, Skia Desktop)** frontend, an **ASP.NET Core Web API** backend server, a Node-free **Tailwind CSS v4** build chain, **EF Core (SQLite)**, and production-ready **Docker / GitHub Actions** CI/CD container publishing.

## 🌐 Live Demo
You can view a live demonstration of the WebAssembly frontend (deployed automatically via GitHub Pages) here:
**[UnoWebTemplate Live Demo](https://jav76.github.io/UnoWebTemplate/)**

---

## 🚀 Features

* 📱 **Multi-targeted Frontend**: Write UI once using WinUI 3 XAML and deploy to WebAssembly, Windows (native WinAppSDK), or Linux/macOS (Skia Desktop).
* ⚡ **Interactive Showcase**: Pre-loaded modular widgets illustrating real-time vector charts, 2.5D grab-and-drag tilt card physics, and a 60fps particle sandbox engine.
* ⚙️ **Integrated Backend**: Serves minimal API endpoints and hosts the WebAssembly client assets dynamically using optimized static file MIME mappings.
* 🎨 **Node-free Tailwind CSS v4**: Automagically downloads the correct platform binary and compiles Tailwind utility classes during MSBuild cycles. No `node` or `npm` required.
* 🪵 **Structured logging (log4net)**: Logs to Stdout/Console, Visual Studio Debug output, rolling daily files, and directly to the database.
* 📦 **Production Containerization**: Multi-stage `Dockerfile` and `docker-compose.yml` set up for instant production deployment.
* 🔄 **Easy Renamer Utility**: Instantly rename all references, namespaces, and directories to your own application's name.

---

## 📂 Repository Structure

```text
├── .github/
│   └── workflows/
│       └── build-and-publish.yml  # GitHub Actions to build/publish Docker images
├── docs/                          # Detailed technical document guides
├── Dockerfile                     # Multi-stage production container build
├── docker-compose.yml             # Orchestration for local container run
├── rename-template.sh             # Namespace and file renamer script
│
├── UnoWebTemplate.Shared/         # Pure Class Library for shared DTOs & Models
│
├── UnoWebTemplate.Client/         # Single-Project WinUI 3 / Uno Client
│
└── UnoWebTemplate.Server/         # ASP.NET Core API server hosting the Client
```

---

## ⚡ Quickstart Guide

### 1. Instantiate the Template
Click **"Use this template"** on GitHub to create a new repository, then clone it to your local machine.

### 2. Rename the Template
Run the renaming script from the root directory to replace all instances of `UnoWebTemplate` with your own application's name:
```bash
chmod +x rename-template.sh
./rename-template.sh MyNewAppName
```

### 3. Build & Run Local Development

#### Run Backend Server (API + Web Client):
```bash
dotnet run --project MyNewAppName.Server/MyNewAppName.Server.csproj
```
Open [http://localhost:5000](http://localhost:5000) in your browser. The page will auto-serve the client WASM interface and connect to the API.

#### Run Skia Desktop Client (Linux/macOS):
```bash
dotnet run --project MyNewAppName.Client/MyNewAppName.Client/MyNewAppName.Client.csproj -f net10.0-desktop
```

---

## 📖 In-Depth Documentation

* [Architecture & Hosting Guide](docs/Architecture.md)
* [Interactive Showcase Dashboard Guide](docs/InteractiveShowcase.md)
* [Node-free Tailwind CSS v4 Guide](docs/Tailwind.md)
* [Database & log4net Logging Guide](docs/Logging.md)
* [Docker & CI/CD Deployment Guide](docs/Containerization.md)
