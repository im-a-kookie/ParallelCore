# Parallel Core

A concurrent framework that encapsulates program state into containerized models that can be processed across multiple threads with configurable parallel schema.

# Technical Details

The Model superclass provides a container for a thread context. IO is explicitly defined by the layout of the model class. Attributed methods are located via reflection, and generated into delegates that trigger the invocation of the method within the thread context of the model instance. These delegates accept a data object as a parameter. Delegates can also be generated that return awaitable tasks, that will contain the returns of the invoked methods.

Using reflection and IL generation, almost all methods can be attributed, including static, but limitations exist and most parameters will be set to null/default. It is recommended to provide only a selection of Model, Signal, and Object? parameters, where the Object? type can be given explicitly, in which case an unsassignable data object will be provided as null.

A future goal may be to provide parameters from a global registry by their string name, but the current scope of the project is complete.

# Basic Signalling

In addition, all models can receive basic signalling through a simple observer pattern. In this mode, signals can be sent as string values, with callback actions and data objects. This allows a simple model to be templated out very quickly without creating new classes, through simple event subscription.


