# Parallel Core

A concurrent framework that encapsulates program state into containerized models that can be processed across multiple threads with configurable parallel schema.

# Technical Details

The Model superclass provides a container for a thread context. IO is explicitly defined by the layout of the model class. Attributed methods are located via reflection, and generated into delegates that trigger the invocation of the method within the thread context of the model instance. These delegates accept a data object as a parameter. Delegates can also be generated that return awaitable tasks, that will contain the returns of the invoked methods.

Using reflection and IL generation, almost all methods can be attributed, including static, but limitations exist and most parameters will be set to null/default. It is recommended to provide only a selection of Model, Signal, and object? parameters, where the object? represents a data packet, and can be typed explicitly.

In many cases, parameter expectations can be provided through generic and explicit typing. Where types are unassignable, these values will be replaced with null/default equivalents.

A future goal may be to provide parameters from a global registry by their string name, but the current scope of the project is complete.

