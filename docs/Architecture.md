# Architecture & WebAssembly Hosting Guide

UnoWebTemplate separates logic, data schemas, frontend interface, and backend APIs using a clean separation of concerns, optimized for cross-platform deployment.

---

## 🏗️ Architectural Layers

### 1. Shared Model Library (`UnoWebTemplate.Shared`)
A pure `.NET Class Library` containing shared Data Transfer Objects (DTOs), constants, enums, and database entities (like `LogEntry`). It is completely independent of UI or web frameworks, allowing it to be compiled for native mobile, desktop, or web applications without dependency bloat.

### 2. Client Application (`UnoWebTemplate.Client`)
A multi-targeted **Uno Platform Single-Project** application. It contains all XAML layouts, pages, styles, client-side viewmodels, and custom showcase widgets. 
* Target Frameworks:
  * `net10.0-browserwasm`: Generates web static assets.
  * `net10.0-desktop`: Runs Skia-based native window on Linux and macOS.
  * `net10.0-windows10.0.*`: Runs WinUI 3 native application on Windows.
* Subdirectories:
  * `Widgets/`: Contains standalone, modular UI controls (like the 3D Tilt Card, Connectivity Sparkline, and Physics Particle sandbox) designed to demonstrate platform capabilities. Refer to the [Interactive Showcase Guide](InteractiveShowcase.md) for more details.

### 3. API & Web Host (`UnoWebTemplate.Server`)
An **ASP.NET Core Web API** application. It serves backend endpoints and dynamically hosts static compiled WebAssembly assets compiled from the Client project.

---

## 🌐 WebAssembly Static Hosting

In ASP.NET Core, standard static file middleware does not serve files with unrecognized mime-types, which results in `404 Not Found` errors for WASM assemblies (`.dat`, `.odb`, `.pdb`). To address this, the server explicitly registers a `FileExtensionContentTypeProvider`:

```csharp
var contentTypeProvider = new FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".dat"] = "application/octet-stream";
contentTypeProvider.Mappings[".odb"] = "application/octet-stream";
contentTypeProvider.Mappings[".pdb"] = "application/octet-stream";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypeProvider
});
```

### Development vs Production Asset Serving
* **In Production**: Assets are compiled into the server's `wwwroot` directory during the Docker build stage and served directly.
* **In Development**: To enable rapid prototyping, the server configures a `PhysicalFileProvider` pointing back to the client's output build directory. The server serves WASM assets directly from the client's debug compilation folder:
```csharp
if (app.Environment.IsDevelopment())
{
    var clientWasmPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "../UnoWebTemplate.Client/UnoWebTemplate.Client/bin/Debug/net10.0-browserwasm/wwwroot"));
    if (Directory.Exists(clientWasmPath))
    {
        var wasmFileProvider = new PhysicalFileProvider(clientWasmPath);
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = wasmFileProvider,
            ContentTypeProvider = contentTypeProvider
        });
    }
}
```

---

## 🚦 SPA Fallback Routing

Since the frontend is a Single Page Application (SPA), all URL navigation pathings must fallback to the root `index.html` file to let the client-side router handle nested URLs. In development, the fallback points to the Client's local file provider:

```csharp
if (fallbackOptions != null)
{
    app.MapFallbackToFile("index.html", fallbackOptions);
}
else
{
    app.MapFallbackToFile("index.html");
}
```
