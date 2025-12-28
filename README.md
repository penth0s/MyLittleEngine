# MyLittleEngine

A custom 3D game engine built with C# and OpenTK, featuring modern rendering techniques, physics simulation, and an integrated editor.

> **Note:** This engine was created as a personal learning project to explore game engine architecture and graphics programming.
> This is the first version built entirely for educational purposes, with **no optimizations** implemented (no camera culling, static/dynamic batching, LOD systems, etc.).
> My future plan is to learn C++ and rebuild this project in C++, focusing on deep optimization techniques and performance improvements.

> Some assets used in the toon scene (such as environment models) are **not included in this public repository**
> due to licensing and distribution restrictions.

## 🎮 Features

### Rendering
- **OpenGL-based 3D Renderer** - Built on OpenTK for cross-platform graphics
- **PBR (Physically Based Rendering)** - Realistic material system
- **Skeletal Animation System** - Full rigging and animation support
- **Dynamic Skybox** - Day-night cycle simulation
- **Shadow Mapping** - PCF soft shadows for realistic lighting
- **Post-Processing Effects** - Vignette and tone mapping
- **Water Rendering** - Advanced water shader with foam effects
- **Debug Wireframe Rendering** - Visual debugging tools
- **Forward Rendering Pipeline**

### Physics
- **Jitter2 Integration** - Robust physics simulation
- **Rigidbody Dynamics** - Full rigid body physics support
- **Character Controller** - WASD movement with collision detection
- **Step Climbing** - Automatic stair/obstacle climbing

### Editor & Tools
- **ImGui-Based Editor** - Lightweight, immediate-mode UI
- **Hierarchy Panel** - Scene graph visualization
- **Inspector Panel** - Component property editing
- **Asset Browser** - Asset management and preview
- **Performance Monitor** - Real-time FPS and performance metrics
- **Scene Serialization** - JSON-based scene saving/loading

### Core Architecture
- **ECS (Entity Component System)** - Data-oriented design pattern
- **Scene Management** - Multi-scene workflow support
- **Resource Management** - Efficient texture, model, and shader loading
- **Input System** - Keyboard and mouse input handling

### Asset Support
- **Models**: FBX, OBJ, DAE, GLTF, and 40+ formats via Assimp
- **Textures**: PNG, JPG, BMP, TGA, HDR via StbImageSharp

## 📸 Media

### 🎥 Engine Demo Video

[![MyLittleEngine Demo](https://img.youtube.com/vi/BEtuLOdnoeE/0.jpg)](https://www.youtube.com/watch?v=BEtuLOdnoeE)

### 🖼️ Screenshots

<table>
  <tr>
    <td align="center">
      <b>Planar Reflection</b><br>
      <img src="Engine/Screenshots/Reflection.png" width="400"/>
    </td>
    <td align="center">
      <b>Toon Scene</b><br>
      <img src="Engine/Screenshots/Toon.png" width="400"/>
    </td>
  </tr>
  <tr>
    <td align="center">
      <b>Vertex Animation</b><br>
      <img src="Engine/Screenshots/Wave.png" width="400"/>
    </td>
    <td align="center">
      <b>Humanoid Animations</b><br>
      <img src="Engine/Screenshots/Character.png" width="400"/>
    </td>
  </tr>
  <tr>
    <td align="center">
      <b>GPU Grass</b><br>
      <img src="Engine/Screenshots/Grass.png" width="400"/>
    </td>
    <td align="center">
      <b>Car Controller</b><br>
      <img src="Engine/Screenshots/Car.png" width="400"/>
    </td>
  </tr>
</table>


## 🚀 Getting Started

### Prerequisites
- .NET 8.0 SDK or higher
- OpenGL 4.5 compatible GPU
- Visual Studio 2022 or JetBrains Rider (recommended)

### Building from Source

1. **Clone the repository**
```bash
git clone https://github.com/penth0s/MyLittleEngine
cd MyLittleEngine
```

2. **Restore dependencies**
```bash
dotnet restore
```

3. **Build the project**
```bash
dotnet build
```

4. **Run the engine**
```bash
dotnet run --project MyLittleEngine
```

## 📦 Dependencies

- **[OpenTK](https://opentk.net/)** - OpenGL bindings for .NET
- **[AssimpNet](https://bitbucket.org/Starnick/assimpnet)** - 3D model loading (40+ formats)
- **[StbImageSharp](https://github.com/StbSharp/StbImageSharp)** - Image loading library
- **[Jitter2](https://github.com/notgiven688/jitterphysics2)** - Physics engine
- **[ImGui.NET](https://github.com/ImGuiNET/ImGui.NET)** - Immediate-mode GUI

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with [OpenTK](https://opentk.net/)
- Physics powered by [Jitter2](https://github.com/notgiven688/jitterphysics2)
- UI framework: [Dear ImGui](https://github.com/ocornut/imgui)
