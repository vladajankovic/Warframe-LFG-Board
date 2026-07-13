# API Gateway

The gateway is the public HTTP/WebSocket entry point for the backend. In Docker Compose it is
available on `http://localhost:8080`; the five microservices are only exposed inside the Compose
network.

## Responsibilities

- Routes existing `/v1/*` APIs and `/hubs/user-notifications` without changing their public paths.
- Validates UsersDirectory JWTs and mirrors the services' anonymous, authenticated, and Admin rules.
- Applies a per-user (or anonymous client IP) fixed-window limit. Login and registration use a
  separate, stricter limit.
- Encodes literal apostrophes in JSON/text requests as `&#39;` and decodes `&#39;` in JSON/text
  responses. JSON property names, non-string values, binary bodies, compressed bodies, oversized
  bodies, server-sent events, and WebSocket frames are not rewritten.
- Uses YARP round-robin clusters with active `/health` probes and passive failure detection.

## Configuration

Settings are in `ApiGateway/appsettings.json` and can be overridden with normal ASP.NET Core
environment variables. Docker Compose overrides the five default localhost destination addresses
with service-network addresses.

For real load balancing, add a destination for every instance under the relevant cluster, for
example:

```json
"Destinations": {
  "instance-1": { "Address": "http://items-store-1:8080/" },
  "instance-2": { "Address": "http://items-store-2:8080/" }
}
```

YARP distributes requests across healthy destinations using `RoundRobin`. SignalR currently has no
server-side backplane, so multiple UserNotification instances need sticky sessions or a SignalR
backplane before that service is scaled out.

Set `JWT_KEY` before starting Compose in any non-local environment. It is passed to both the token
issuer/validators and the gateway so token validation remains consistent.
