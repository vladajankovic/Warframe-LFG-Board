# Nginx Use and Functionality Analysis

## Nginx's role

Nginx is the project's browser-facing entry point in the Docker deployment. It has two responsibilities:

1. Serve the compiled React application.
2. Reverse-proxy API and SignalR traffic to the appropriate ASP.NET microservice.

The request flow is:

`Browser -> localhost:3000 -> Nginx container -> React files or backend service`

The configuration is in `webapp/frontend/nginx.conf`, while the runtime image is defined in `webapp/frontend/Dockerfile`.

## Static frontend hosting

The Dockerfile first builds the React/Vite application using Node and then copies `dist` into an `nginx:1.27-alpine` runtime image:

- Build output: `/app/dist`
- Nginx document root: `/usr/share/nginx/html`
- Container port: `80`
- Host port through Compose: `3000`

The relevant deployment mapping is in `webapp/docker-compose.yml`.

Nginx supports React client-side routing through:

```nginx
try_files $uri $uri/ /index.html;
```

Therefore, visiting a route such as `/resources/argon-crystal` directly still loads `index.html`, allowing React Router to interpret the route. Without this fallback, refreshing a client-side route would normally produce a 404.

## Reverse-proxy routing

Nginx uses Docker Compose service names as internal DNS hostnames:

| Public route | Internal destination |
|---|---|
| `/v1/directory...` | `users-directory:8080` |
| `/v1/itemstore...` | `items-store:8080` |
| `/v1/grouprequests...` | `group-requests:8080` |
| `/v1/audit...` | `audit:8080` |
| `/v1/user-notification...` | `user-notification:8080` |
| `/hubs/user-notifications...` | `user-notification:8080` |

Because each `proxy_pass` has no URI suffix, Nginx preserves the original path. For example:

```text
GET /v1/itemstore/resources
-> http://items-store:8080/v1/itemstore/resources
```

This matches the ASP.NET controller routes used by the services.

The frontend deliberately uses relative URLs such as `/v1/itemstore`, as seen in `webapp/frontend/src/api/itemsApi.ts`. Consequently:

- The frontend does not need to know individual service ports.
- Browser requests remain same-origin.
- Normal browser CORS configuration is generally unnecessary.
- Backend service locations can change without rebuilding frontend API URLs, provided the Nginx mapping is updated.

Authorization headers and query strings are passed upstream by default.

## SignalR/WebSocket support

The notification hub has a dedicated proxy block in `webapp/frontend/nginx.conf`.

It:

- Uses HTTP/1.1.
- Forwards `Upgrade` and `Connection` headers.
- Preserves the SignalR `access_token` query parameter.
- Permits long-running connections with one-hour read and send timeouts.

This supports the frontend SignalR connection created in `webapp/frontend/src/api/notificationsApi.ts`. The notification service extracts the query-string JWT specifically for this hub.

## Proxy metadata

Each proxy route sets:

- `Host`
- `X-Real-IP`
- `X-Forwarded-For`
- `X-Forwarded-Proto`

This is the correct metadata to send, but none of the inspected ASP.NET pipelines calls `UseForwardedHeaders`. As a result, the applications may still consider Nginx to be the client and HTTP to be the original scheme. This matters for:

- Client-IP logging.
- Generated absolute URLs.
- HTTPS redirection.
- Secure-cookie behavior.
- Deployments behind an additional TLS load balancer.

## Development versus Docker

Nginx is not used when running `npm run dev`. Vite provides a parallel proxy configuration in `webapp/frontend/vite.config.ts`, forwarding requests to host ports `8001` through `8005`.

Therefore:

- Local Vite development: browser -> Vite proxy -> backend host ports.
- Docker frontend: browser -> Nginx -> Docker service names.

The two proxy configurations currently match, but they are duplicated and can drift when new endpoints are added.

## Strengths

The current setup provides:

- A single browser-facing frontend/API origin.
- Correct React SPA fallback behavior.
- Clear path-based routing between microservices.
- Working WebSocket/SignalR upgrade handling.
- A small multi-stage frontend image.
- Isolation through the shared Docker bridge network.
- Preservation of client and scheme metadata for future backend processing.

## Gaps and risks

The configuration is suitable for local or basic container deployment, but it is not yet a complete production gateway.

### 1. No TLS

Nginx listens only on HTTP port 80. TLS must be added here or terminated by an external proxy/load balancer.

### 2. Backend ports bypass Nginx

Compose publishes ports `8001-8005` to the host. Anyone who can access those ports can call the services directly, bypassing Nginx. For an Nginx-only production entry point, these should normally use `expose` or no host mapping.

### 3. Forwarded headers are not consumed

The ASP.NET services should configure trusted proxies/networks and call `UseForwardedHeaders` before HTTPS redirection and authentication-related middleware.

### 4. No static asset caching or compression policy

There are no explicit long-lived cache headers for Vite's hashed assets and no configured gzip/Brotli behavior. Adding caching for `/assets/` would improve repeat-load performance while keeping `index.html` uncached or short-lived.

### 5. No security headers

Headers such as `X-Content-Type-Options`, `Referrer-Policy`, a suitable Content Security Policy, and framing restrictions are absent.

### 6. Broad prefix matching

A location such as `location /v1/directory` also matches `/v1/directoryAnything`. Using exact base-route handling plus slash-delimited locations would create stricter route boundaries.

### 7. Limited resilience

The frontend only waits for backend containers to be started, not healthy. There are no backend health checks or restart policies. Backend recreation may also require an Nginx reload or restart if Docker assigns a different service IP.

### 8. No gateway protections

Nginx currently performs no rate limiting, request-size policy, API-specific timeout policy, access control, or custom upstream error handling.

### 9. Development-oriented deployment

The backends run with `ASPNETCORE_ENVIRONMENT=Development`, and their ports, database, RabbitMQ, and management interfaces are all published. The overall Compose setup should be treated as local-development infrastructure unless hardened separately.

## Assessment

Nginx is functioning as a lightweight static server and path-based API gateway. Its routing accurately matches the application's API calls, and the special SignalR configuration is appropriate. Its biggest architectural benefit is presenting the React application and all microservices through one origin at `http://localhost:3000`.

For production, the highest-priority improvements are TLS, removing direct backend port exposure, configuring ASP.NET forwarded-header processing, adding health checks, and defining caching/security headers.

`docker compose config --quiet` confirmed that the Compose configuration parses successfully, although a live Nginx container test was unavailable because the Docker daemon was not running.
