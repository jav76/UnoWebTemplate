# Stage 1: Build & Compile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Emscripten WASM toolchain requires Python
RUN apt-get update && apt-get install -y --no-install-recommends \
    python3 \
    python-is-python3 \
    clang \
    zlib1g-dev \
    && rm -rf /var/lib/apt/lists/*

# Install WASM workloads for Uno Platform WebAssembly builds
RUN dotnet workload install wasm-tools

# Copy global props and package structures
COPY global.json ./
COPY UnoWebTemplate.slnx ./

# Copy Client targets/Central Package configuration
COPY UnoWebTemplate.Client/Directory.Build.props UnoWebTemplate.Client/
COPY UnoWebTemplate.Client/Directory.Packages.props UnoWebTemplate.Client/
COPY UnoWebTemplate.Client/UnoWebTemplate.Client.sln UnoWebTemplate.Client/

# Copy csproj definitions for cacheable dependency restore
COPY UnoWebTemplate.Shared/UnoWebTemplate.Shared.csproj UnoWebTemplate.Shared/
COPY UnoWebTemplate.Client/UnoWebTemplate.Client/UnoWebTemplate.Client.csproj UnoWebTemplate.Client/UnoWebTemplate.Client/
COPY UnoWebTemplate.Server/UnoWebTemplate.Server.csproj UnoWebTemplate.Server/

RUN dotnet restore UnoWebTemplate.Client/UnoWebTemplate.Client/UnoWebTemplate.Client.csproj
RUN dotnet restore UnoWebTemplate.Server/UnoWebTemplate.Server.csproj

# Copy remaining source code files
COPY UnoWebTemplate.Shared/ UnoWebTemplate.Shared/
COPY UnoWebTemplate.Client/ UnoWebTemplate.Client/
COPY UnoWebTemplate.Server/ UnoWebTemplate.Server/

# Publish Client (WebAssembly target)
RUN dotnet publish UnoWebTemplate.Client/UnoWebTemplate.Client/UnoWebTemplate.Client.csproj \
    -c Release -f net10.0-browserwasm -o /app/client-publish

# Publish Server (ASP.NET Core API Backend)
# (This step downloads the Linux Tailwind CSS binary and compiles styles automatically)
RUN dotnet publish UnoWebTemplate.Server/UnoWebTemplate.Server.csproj \
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

ENTRYPOINT ["./UnoWebTemplate.Server"]
