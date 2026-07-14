# Node-free Tailwind CSS v4 Build Integration

To compile utility classes without requiring developers or Docker environments to have Node.js or npm installed, UnoWebTemplate uses MSBuild to download and run the standalone Tailwind CLI executable.

---

## ⚙️ Tailwind CSS v4 Features
Tailwind v4 is configuration-free. It does not require a `tailwind.config.js` file. Instead, theme configurations (like custom color palettes or spacing variables) are defined directly inside your stylesheet using standard CSS `@theme` and `@import "tailwindcss"` directives:

```css
@import "tailwindcss";

@theme {
  --color-brand-primary: #3b82f6;
  --color-brand-dark: #0f172a;
}
```

---

## 🛠️ MSBuild Auto-Download Pipeline

Inside `UnoWebTemplate.Server.csproj`, we declare the version of Tailwind to run and detect the operating system/architecture of the compilation machine:

```xml
<PropertyGroup>
  <TailwindVersion>4.0.0</TailwindVersion>
  <TailwindPlatform Condition="$([MSBuild]::IsOSPlatform('Windows'))">windows</TailwindPlatform>
  <TailwindPlatform Condition="$([MSBuild]::IsOSPlatform('OSX'))">macos</TailwindPlatform>
  <TailwindPlatform Condition="$([MSBuild]::IsOSPlatform('Linux'))">linux</TailwindPlatform>
  
  <TailwindArch>$([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLower())</TailwindArch>
  <TailwindExeExtension Condition="$([MSBuild]::IsOSPlatform('Windows'))">.exe</TailwindExeExtension>
  <TailwindExeName>tailwindcss-$(TailwindPlatform)-$(TailwindArch)$(TailwindExeExtension)</TailwindExeName>
  <TailwindUrl>https://github.com/tailwindlabs/tailwindcss/releases/download/v$(TailwindVersion)/$(TailwindExeName)</TailwindUrl>
  <TailwindLocalExe>$(MSBuildProjectDirectory)/tailwindcss$(TailwindExeExtension)</TailwindLocalExe>
</PropertyGroup>
```

### Targets
1. **DownloadTailwind**: Triggers if the `tailwindcss` binary does not exist locally. It fetches the platform-correct standalone CLI from Tailwind's official GitHub releases and applies executable permissions on Linux/macOS.
```xml
<Target Name="DownloadTailwind" BeforeTargets="TailwindCss">
  <DownloadFile SourceUrl="$(TailwindUrl)" DestinationFolder="$(MSBuildProjectDirectory)" DestinationFileName="tailwindcss$(TailwindExeExtension)" Condition="!Exists('$(TailwindLocalExe)')" />
  <Exec Command="chmod +x &quot;$(TailwindLocalExe)&quot;" Condition="!$([MSBuild]::IsOSPlatform('Windows')) And Exists('$(TailwindLocalExe)')" />
</Target>
```

2. **TailwindCss**: Runs during the compilation pipeline to bundle and minify CSS classes from the source file to the final `wwwroot/app.css` target.
```xml
<Target Name="TailwindCss" BeforeTargets="BeforeBuild;ResolveStaticWebAssetsInputs">
  <Exec Command="&quot;$(TailwindLocalExe)&quot; -i Styles/app.css -o wwwroot/app.css --minify" />
</Target>
```

---

## 🏃 Tailwind Usage in Development
When you build or launch your server using standard CLI tools (`dotnet build` or `dotnet run`), MSBuild triggers these targets automatically, compiling utility styles in a fraction of a second.
The compiled styles inside `wwwroot/app.css` are immediately available to the hosted Uno WebAssembly frontend views.
