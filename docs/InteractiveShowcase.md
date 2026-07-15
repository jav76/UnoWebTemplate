# Interactive Showcase Dashboard Guide

UnoWebTemplate includes an **Interactive Showcase Dashboard** on the home landing page. This page demonstrates Uno Platform's capabilities for rendering real-time vector graphics, composition-based input tracking, responsive code-behind layouts, and backend API/SignalR connectivity.

---

## 📁 Repository Widget Layout

To ensure easy cleanup and modularity, all showcase elements are fully isolated as standalone `UserControl` components inside the `Widgets/` folder:

```text
UnoWebTemplate.Client/UnoWebTemplate.Client/Widgets/
├── ConnectivityWidget.xaml       # Real-time ping sparkline view
├── ConnectivityWidget.xaml.cs    # Dispatcher pinger and Polyline drawing
├── TiltCardWidget.xaml           # 2.5D glassmorphic member card view
├── TiltCardWidget.xaml.cs        # Grab-and-drag tilt physics calculations
├── ParticleSandboxWidget.xaml    # Particle playground viewport canvas
└── ParticleSandboxWidget.xaml.cs # 60fps rendering physics loop
```

---

## ⚡ Widget Breakdowns

### 1. Connectivity & Vector Sparkline (`ConnectivityWidget`)
* **Core Functions**: Measures HTTP and SignalR latency in real-time.
* **Vector Rendering**: Utilizes a XAML `Polyline` control inside a Canvas. As latency values are fetched, they are mapped to coordinate space and drawn as a continuous sparkline chart.
* **Background Timer**: An active `DispatcherTimer` ticks every 3 seconds to ping backend endpoints, driving a neon glowing pulse animation inside the control.

### 2. 2.5D Hologram Card (`TiltCardWidget`)
* **Core Functions**: Implements a glassmorphic credential card that responds dynamically to mouse/touch interaction.
* **2.5D Skew/Scale Model**: Standard WinUI `PlaneProjection` is not fully supported on WebAssembly. To solve this, the card utilizes a hardware-accelerated 2.5D `CompositeTransform` (`SkewX`, `SkewY`, `ScaleX`, `ScaleY`).
* **Hover Interaction**: Hovering over the card scales it up to `1.03` (creating a physical "lift" off the page) and skews it slightly (up to `2.5` degrees) to track the mouse position.
* **Grab & Drag Interaction**: Clicking and dragging on the card captures the cursor, depresses the card scale to `0.94` (creating a tactile press-depth feeling), and skews the card up to `11.0` degrees following your drag offset.
* **Specular Glare**: Shifts a shiny diagonal linear gradient sheen overlay in opposition to the cursor offset to simulate metallic light reflection.
* **Damping (Inertia)**: Applies a linear interpolation (Lerp) damping coefficient (`0.12`) on every rendering tick to create smooth momentum and drag transitions.

### 3. Particle Physics Sandbox (`ParticleSandboxWidget`)
* **Core Functions**: Runs a physics particle engine on a raw XAML Canvas at 60 FPS.
* **Rendering Loop**: Hooks into the `CompositionTarget.Rendering` event to execute a physics simulation cycle on every frame.
* **Interaction**:
  * **Gravity Attraction**: Moving the cursor inside the canvas applies a gravity pull, causing the swarm of particles to flock towards the cursor.
  * **Click Burst**: Clicking inside the canvas releases a sudden burst of new particles that fly outward in random directions, slowly slowing down and fading out.

---

## 📐 Responsive C# Layout Manager

In WebAssembly, standard XAML `VisualStateManager` setters can encounter evaluation issues when trying to modify attached properties (like `(Grid.Row)` or `(Grid.Column)`). This can result in overlapping elements when switching from desktop to mobile viewport sizes.

To prevent this layout lock:
1. **Root Event Binding**: Resizing is bound to the root `Page.SizeChanged` event in `MainPage.xaml`. Because the Page stretches to fill the browser viewport, its size changes dynamically in both directions.
2. **Dynamic Position Manager**: Inside `MainPage.xaml.cs`, the `Page_SizeChanged` handler monitors page width using a **`760px` breakpoint**:
   * **Desktop Mode ($\ge$ 760px)**: Rebuilds `DashboardGrid` into 2 columns. Places the connectivity widget (top-left), the 3D hologram card (bottom-left), and the particle sandbox (spanning both rows on the right). Bottom info panels organize horizontally.
   * **Mobile Mode (< 760px)**: Rebuilds `DashboardGrid` into 1 column. Stacks all three widgets vertically for single-column scrolling. Bottom info panels stack vertically.

---

## 🧹 Removing the Showcase (Modularity)

If you are using this boilerplate as a production template and wish to delete the showcase demo:

1. **Delete the Widgets Directory**:
   ```bash
   rm -rf UnoWebTemplate.Client/UnoWebTemplate.Client/Widgets
   ```

2. **Simplify MainPage.xaml**:
   Open [MainPage.xaml](file:///home/jaret/Documents/GitHub/UnoWebTemplate/UnoWebTemplate.Client/UnoWebTemplate.Client/MainPage.xaml) and delete the `<Grid x:Name="DashboardGrid" ...>` element block.
   
3. **Clean MainPage.xaml.cs**:
   Open [MainPage.xaml.cs](file:///home/jaret/Documents/GitHub/UnoWebTemplate/UnoWebTemplate.Client/UnoWebTemplate.Client/MainPage.xaml.cs), delete the `Page_SizeChanged` layout code and remove references to `ConnectivityCard`, `TiltCard`, and `ParticleCard`.
   
*(Note: The static backend API verification buttons and system architecture card at the bottom of the page will remain in place as standard template defaults).*
