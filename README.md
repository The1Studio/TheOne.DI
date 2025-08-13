# TheOne.DI

A minimal dependency container for Unity

## Installation

### Option 1: Unity Scoped Registry (Recommended)

Add the following scoped registry to your project's `Packages/manifest.json`:

```json
{
  "scopedRegistries": [
    {
      "name": "TheOne Studio",
      "url": "https://upm.the1studio.org/",
      "scopes": [
        "com.theone"
      ]
    }
  ],
  "dependencies": {
    "com.theone.di": "1.1.0"
  }
}
```

### Option 2: Git URL

Add to Unity Package Manager:
```
https://github.com/The1Studio/TheOne.DI.git
```

## Features

- Lightweight dependency injection container
- Support for VContainer and Zenject wrappers
- Simple and intuitive API
- Minimal overhead

## Usage

### Basic Usage

```csharp
using TheOne.DI;

// Resolve a single instance
var service = container.Resolve<IMyService>();

// Try resolve with null check
if (container.TryResolve<IMyService>(out var service))
{
    service.DoSomething();
}

// Resolve all implementations
var handlers = container.ResolveAll<IEventHandler>();

// Instantiate with dependencies
var instance = container.Instantiate<MyClass>(param1, param2);
```

### VContainer Integration

```csharp
using TheOne.DI;
using VContainer;

public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // Register VContainer wrapper
        builder.Register<IDependencyContainer, VContainerWrapper>(Lifetime.Singleton);
    }
}
```

### Zenject Integration

```csharp
using TheOne.DI;
using Zenject;

public class GameInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // Register Zenject wrapper
        Container.Bind<IDependencyContainer>()
            .To<ZenjectWrapper>()
            .AsSingle();
    }
}
```

## Architecture

### Folder Structure

```
TheOne.DI/
├── Scripts/
│   ├── IDependencyContainer.cs    # Core container interface
│   ├── DependencyContainer.cs     # Default implementation
│   ├── VContainerWrapper.cs       # VContainer adapter
│   └── ZenjectWrapper.cs          # Zenject adapter
```

### Core Classes

#### `IDependencyContainer`
Universal dependency injection interface:
- `Resolve<T>()` - Resolve a single instance
- `ResolveAll<T>()` - Resolve all registered instances
- `TryResolve<T>(out T)` - Safe resolution with null check
- `Instantiate<T>()` - Create instance with dependency injection

#### `DependencyContainer`
Default lightweight implementation:
- Simple service registration and resolution
- Support for singleton and transient lifetimes
- Constructor injection support
- Minimal memory footprint

#### `VContainerWrapper`
Adapter for VContainer framework:
- Wraps VContainer's `IObjectResolver`
- Seamless integration with VContainer scopes
- Full support for VContainer features

#### `ZenjectWrapper`
Adapter for Zenject framework:
- Wraps Zenject's `DiContainer`
- Compatible with Zenject installers
- Preserves Zenject binding syntax

### Design Patterns

- **Adapter Pattern**: Wrappers for different DI frameworks
- **Interface Abstraction**: Framework-agnostic DI operations
- **Dependency Inversion**: Depend on abstractions, not implementations
- **Service Locator**: Optional pattern for global access

### Code Style & Conventions

- **Namespace**: All code under `TheOne.DI` namespace
- **Null Safety**: Uses `#nullable enable` directive
- **Interfaces**: Prefixed with `I` (e.g., `IDependencyContainer`)
- **Generic Methods**: Provide both generic and non-generic versions
- **Out Parameters**: Use `MaybeNullWhen` attribute for Try patterns
- **Method Naming**: `TryXxx` for safe operations, `Xxx` for throwing

### Integration Examples

#### Service Registration

```csharp
// Generic service registration
public static class ServiceExtensions
{
    public static void RegisterMyServices(this IDependencyContainer container)
    {
        // Register services
        container.Register<ILogger, ConsoleLogger>();
        container.Register<IDatabase, SqlDatabase>();
        container.Register<IUserService, UserService>();
    }
}
```

#### Factory Pattern

```csharp
public class ServiceFactory
{
    private readonly IDependencyContainer container;
    
    public ServiceFactory(IDependencyContainer container)
    {
        this.container = container;
    }
    
    public T Create<T>() where T : class
    {
        return container.Instantiate<T>();
    }
}
```

#### Multi-Framework Support

```csharp
public class CrossFrameworkService
{
    private readonly IDependencyContainer container;
    
    public CrossFrameworkService(IDependencyContainer container)
    {
        // Works with any DI framework
        this.container = container;
    }
    
    public void Initialize()
    {
        // Framework-agnostic resolution
        var services = container.ResolveAll<IInitializable>();
        foreach (var service in services)
        {
            service.Initialize();
        }
    }
}
```

## Performance Considerations

- Minimal abstraction overhead
- Lazy initialization support
- Efficient type caching
- Zero allocation for cached resolutions

## Best Practices

1. **Framework Choice**: Use wrappers to maintain flexibility
2. **Interface Design**: Define clear service interfaces
3. **Lifetime Management**: Prefer singleton for stateless services
4. **Circular Dependencies**: Avoid circular dependency chains
5. **Testing**: Use the default container for unit tests
6. **Resolution Scope**: Resolve at composition root when possible