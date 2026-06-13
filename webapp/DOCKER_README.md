# Docker Compose Setup

This directory contains Docker configuration for running all microservices locally.

## Prerequisites

- Docker Desktop installed and running
- Docker Compose installed (included with Docker Desktop)

## Services

The docker-compose.yml includes:

- **PostgreSQL Database** (port 5432)
  - All microservices share a single PostgreSQL instance
  - Creates separate databases for each service
  - Credentials: `postgres` / `kolkoparatolkomuzike`

- **RabbitMQ Broker** (ports 5672, 15672)
  - Message broker for inter-service communication
  - Management UI available at `http://localhost:15672`
  - Credentials: `guest` / `guest`

- **Microservices**
  - UsersDirectory (port 8001)
  - ItemsStore (port 8002)
  - GroupRequests (port 8003)

## Running the Services

### Start all services:
```bash
docker-compose up -d
```

### View logs:
```bash
docker-compose logs -f
```

### Stop all services:
```bash
docker-compose down
```

### Remove all data (volumes):
```bash
docker-compose down -v
```

## Environment Variables

All services are configured with:
- JWT configuration for authentication
- Database connection strings
- RabbitMQ connection details

These are set in the docker-compose.yml file. Modify them as needed for your environment.

## Accessing Services

- **Users Directory API**: http://localhost:8001
- **Items Store API**: http://localhost:8002
- **Group Requests API**: http://localhost:8003
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)
- **PostgreSQL**: localhost:5432

## Troubleshooting

### Database connection fails
- Ensure PostgreSQL container is healthy: `docker-compose ps`
- Check logs: `docker-compose logs postgres`

### RabbitMQ connection fails
- Ensure RabbitMQ container is healthy: `docker-compose ps`
- Check logs: `docker-compose logs rabbitmq`

### Services fail to start
- Check service logs: `docker-compose logs <service-name>`
- Ensure port conflicts don't exist
- Verify Docker daemon is running

## Database Migrations

Database migrations are run automatically when services start (as configured in Program.cs).
