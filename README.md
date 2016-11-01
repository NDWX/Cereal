# Cereal
Transport independent resource locking server.

## Challenges
Cereal was born out of the need to synchronize access to resource in, but not limited to, the following scenarios:
- When multiple instances of an application/system, such as web application or web api, are running on different hosts for the purpose of load balancing
- When an application/system is composed of multiple subsystems running on different hosts interacting with the same set of resources

While Cereal may be used to provide resource locking to applications/systems running on the same host, it may not be the most efficient solution compared to global Mutex.

## Concept
Cereal handles the complexities of providing resource locking service to application/system, referred to as subject from hereon, without dictating communication protocol and security implementation.

A subject requests resource-lock by providing identifier of the resource and is, currently, allowed to specify the maximum amount of time the lock will be held.

Cereal does not validate or provide a way to accept a way to validate resource identifier and instead leaves it to the application/system designer to decide identifier scheme of resources.

As consequence, an instance of Cereal may be used to provide resource locking for one or multiple types of resources depending on requirements such as, but not limited to, computing resources and performance.

It is also worth noting that while allowing a subject to specify maximum lock hold time ensures other subjects are able to request lock to a resource on lock-time elapsed, there is no guarantee that the first subject has ceased interacting with the resource and it is currently up to the application/system designer to ensure resource-lock is released when no longer required.

## Feedback

Please leave your feedback either by creating new issue or email me at [Andrian@ND.id.au](mailto:Andrian@ND.id.au).
