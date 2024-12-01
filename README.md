# ParallelCore

ParallelCore is a C# library for building and running logic models in parallel environments. It provides an easy-to-use framework for managing and interacting with models, and distributing models over configurable parallel schema (particularly, threadpool). Attributes, Reflection and IL Generation are used to map model layout to delegates for efficient invocation.

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
   - Can attribute methods and subscribe to events to execute threaded code

2. **Parallel Schema**:
   - Defaults to a thread pool but can be customized.
   - Provides message configurable message loops to models

3. **Reflection and IL Generation**:
   - Runtime discovery of methods.
   - Flexible signature handling using ILGenerator.

4. **Synchronization Patterns**:
   - Ensures safe execution across threads.
   - Supports configurable update rates and update scheduling

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
var provider = new Provider(new ThreadPoolSchema());
provider.RunModel(new MyModel());
```

### 3. Calling a Method via Delegate
```csharp
var instance = new MyModel();
var delegateCaller = modelInstance.GetDelegate("DoWork");

delegateCaller("Hello, ParallelCore!"); //called from model thread

// Output:
// Processing: Hello, ParallelCore!
```

### 4. Subscribing to Events
```csharp
var instance = new MyModel();
instance.OnTick += () => Console.WriteLine("Model Ticked!");
// Output:
// Model Ticked!
// Model Ticked!
// ...
```
---

## Patterns Used

- **Observer Pattern**: For subscribing to and broadcasting events.
- **Message Loop Pattern**: Centralized processing of model messages.
- **ThreadPool Pattern**: Efficient parallel execution and thread management.
- **Synchronization Patterns**: Thread-safe access and updates to shared resources.

---

## Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/ParallelCore.git
   ```
2. Build the project and add the compiled library to your application.
3. Follow the examples above to create your first parallel model.

---

## Contributing
Contributions are welcome! Please open an issue or submit a pull request.

---

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

