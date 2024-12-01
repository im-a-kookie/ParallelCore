# ParallelCore

ParallelCore is a C# library for building and running logic models in parallel environments. It provides an easy-to-use framework for managing and interacting with models, and distributing models over configurable parallel schema (particularly, threadpool). Attributes, Reflection and IL Generation are used to map model layout to delegates for efficient invocation. Task Parallel Library integration provides async/await for dispatched commands.

---

## Features

- **Model-Driven Design**: Create logic models and run them concurrently through parallelized containers
- **Command Reflection**: Discover and call methods dynamically at runtime using command strings and attributed methods.
- **IL Generation**: Uses ILGeneration to dynamically and efficiently map parameters into method signatures
- **Thread-Aware Delegates**: Delegates generated for model methods are executed within the thread context of the model.
- **Patterns and Synchronization**: Implements the Observer, Message, and ThreadPool patterns, and various synchronization mechanisms.

---

## Technical Overview

### Architecture
1. **Models**:
   - Define the core logic of the application.
   - Provides clear threadsafe state encapsulation
   - Method discovery and events for threaded execution
   - Supports configurable update rates and scheduling

2. **Parallel Schema**:
   - Configurable. Defaults to threadpool.
   - Defines how thread context is provided to Models

3. **Reflection and IL Generation**:
   - Runtime discovery of methods via Reflection
   - Efficient runtime mapping of method signatures via IL Generation
   - Only supports one arbitrary parameter. TODO support more.

4. **Synchronization Patterns**:
   - Ensures safe threaded execution and encapsulation of thread context by model
   - Actor-Model, State Pattern, Observer Pattern, Message Pattern, etc
---

## Code Examples

### 1. Creating a Simple Model
```csharp
using ParallelCore;

public class MyModel : Model
{
    public Model()
    {
      TickRate = 1; // 1 tick per second
    }

    [Endpoint]
    public void DoWork(string? message)
    {
        Console.WriteLine($"Processing: {message}");
    }
}
```

### 2. Setting Up a Parallel Schema

```csharp
var provider = new new ThreadPoolSchema();
```

### 3. Calling a Method via Delegate
```csharp
// Create and run a new instance of the model
var instance = new MyModel(provider);

// Delegate can now be invoked within the model container
var delegateCaller = instance.GetDelegate("DoWork");
delegateCaller("Hello, ParallelCore!");

// Output:
// Processing: Hello, ParallelCore!
```

### 4. Subscribing to Events
```csharp
var instance = new MyModel(provider);

// Subscribe to the configurable tick event
instance.OnTick += () => Console.WriteLine("Model Ticked!");

// Output:
// Model Ticked!
// Model Ticked!
// ...
```
---

## Patterns Used

- **State Pattern**: Encapsulation of state for resource safety.
- **Observer Pattern**: For subscribing to and broadcasting events.
- **Message Loop Pattern**: Centralized processing of model messages.
- **ThreadPool Pattern**: Efficient parallel execution and thread management.
- **Synchronization Patterns**: Thread-safe access and updates to shared resources.

---

## Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/im-a-kookie/ParallelCore.git
   ```
2. Build and reference library in project
3. Follow examples above to get started

---

## Contributions

Contributions welcome, please open an issue or send PR.

---

## License
This project is licensed under the MIT License

