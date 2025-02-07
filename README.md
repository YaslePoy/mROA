# mROA

mROA is very fast and light, use-friendly RPC library with providing execution context. It allows to work with objects 
on server like they are local.

## Extensibility

Thanks to modularity and using the simplified Dependency Injection approach, you can change the logic of work, add your 
own layers and redefine modules to get the desired result.

## Speed

mROA is a very lightweight and fast library in its basic implementation using TCP protocol. mROA can be up to 2.8 times 
faster than gRPC when mROA is using JSON serialization (it can be changed).

---

## Getting started

When using this library, it is recommended to use 3 projects: a client, a server, and a project that contains an 
abstraction of their interaction. Two packages need to be added to the third project:

* [mROA](https://www.nuget.org/packages/mROA/) - this library is needed to launch and implement communication between 
the client and the server, and also allows you to mark up the interaction interfaces.
* [mROA.Codegen](https://www.nuget.org/packages/mROA.Codegen/) - it generates implementation for remote access.

Basically, it is possible that there is no common project, but in this case, the correct binding of functions will not be guaranteed.

### Declaration 

Use SharedObjectInterface attribute in shared project to mark interface as transmittable object. It allows to use it on both sides.

You can transmit implementation of that interface using TransmittedSharedObject.

### Implementation

On server side you all interfaces must be implemented by classes and each interface should have one implementation class
with SharedObjectSingleton attribute. That class will be used as default and it can produce another objects with another
implementations.

To get that default implementation on client, call GetSingleObject.

Full example is represented in Example projects

---

## Roadmap

* [ ] Encryption. RSA + AES standards
* [ ] CBOR or another binary format serialization
* [ ] Sessions for clients, tracking and clearing their remote objects