# Cereal
Transport independent resource locking server.

## Challenges
Cereal was born out of the need to synchronize access to resource in, but not limited to, the following scenarios:
- When multiple instances of an application/system, such as web application or web api, are running on different hosts for the purpose of load balancing
- When an application/system is composed of multiple subsystems running on different hosts interacting with the same set of resources

While Cereal may be used to provide resource locking to applications/systems running on the same host, it may not be the most efficient solution compared to global Mutex.

## Concept
Cereal handles the complexities of providing resource locking service to application/system, referred to as subject from hereon, without dictating communication protocol and security implementation.

As resource locking and release require applications to communicate with Cereal over the network, implementation of Cereal is meant to be straightforward and fast and has ommited resource identifier validation and deadlock detection to avoid from being the 'thing' that takes the most time in application business logic.

It is up to application/system designer to decide identifier scheme of resources which allows an instance of Cereal to provide resource locking for one or multiple types of resources depending on requirements such as, but not limited to, computing resources and performance.

### Deadlock Detection

To prevent deadlock, Cereal keeps a graph of resource lock requested and locks held by subjects.

When a requested resource is locked by a peer subject, Cereal checks if subject holding lock for a resource requested by the peer and throws exception accordingly.

To ensure consistency of resource lock graph, a subject is restricted to one lock request/release call at any one time, increasing overall latency.

This feature is enabled only when `DETECT_DEADLOCK` symbol is defined during build.

## Feedback

Please leave your feedback either by creating new issue or email me at [Andrian@ND.id.au](mailto:Andrian@ND.id.au).
