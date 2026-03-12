# MyMicroServiceProject

A containerised microservices application built with **.NET 10**. It consists of two independent Web API back-ends, two dedicated SQL Server databases, and a Blazor WebAssembly front-end — all wired together with Docker Compose.

## Architecture

```
Browser
  │
  └─► MyClient (Blazor WASM · nginx · :5200)
        │                         │
        ▼                         ▼
  ProductService API          OrderService API
      (:5043)                     (:5044)
        │                         │    │
        ▼                         ▼    └──► ProductService API
  mssql-product-db          mssql-order-db   (service-to-service)
      (:1433)                   (:1434)
```

All containers share a single Docker bridge network (`microservices-net`).

## Services

| Service | Technology | Host Port | Description |
|---|---|---|---|
| **ProductService** | ASP.NET Core Minimal API | `5043` | CRUD for products |
| **OrderService** | ASP.NET Core Minimal API | `5044` | CRUD for orders; calls ProductService to validate products and snapshot prices |
| **MyClient** | Blazor WebAssembly + nginx | `5200` | Single-page front-end |
| **mssql-product-db** | SQL Server 2022 | `1433` | Dedicated DB for ProductService |
| **mssql-order-db** | SQL Server 2022 | `1434` | Dedicated DB for OrderService |
| **rabbitmq** | RabbitMQ 3 + Management UI | `5672` / `15672` | Message broker for async events between services |

## Tech Stack

- **.NET 10** — all projects target `net10.0`
- **ASP.NET Core Minimal APIs** — ProductService & OrderService
- **Blazor WebAssembly** — MyClient front-end
- **Entity Framework Core 10** with SQL Server provider
- **SQL Server 2022** (Docker image `mcr.microsoft.com/mssql/server:2022-latest`)
- **nginx** — serves the compiled Blazor WASM static files
- **RabbitMQ 3** — message broker (AMQP)
- **MassTransit** — messaging abstraction over RabbitMQ (publish/consume, automatic topology)
- **Docker & Docker Compose** — single-command local setup

## Messaging (RabbitMQ & MassTransit)

Asynchronous communication between the services is handled by **RabbitMQ** as the message broker and **MassTransit** as the .NET messaging library. 

### Message contracts

Both contracts live in each service's `Contracts/` folder and are decorated with `[MessageUrn]` so MassTransit uses a stable, version-independent routing key.

| Contract | Namespace / URN | Fields |
|---|---|---|
| `OrderPlaced` | `order-placed` | `OrderId`, `ProductId`, `Quantity` |
| `StockDepleted` | `stock-depleted` | `ProductId`, `ProductName` |

### Event flow

```
OrderService                        RabbitMQ                      ProductService
    │                                  │                               │
    │  POST /orders (success)           │                               │
    │──Publish(OrderPlaced)────────────►│                               │
    │                                  │──OrderPlaced event────────────►│
    │                                  │                  OrderPlacedConsumer
    │                                  │                  • decrements product.Stock
    │                                  │                  • if Stock == 0 →
    │                                  │◄──Publish(StockDepleted)───────│
    │◄──StockDepleted event────────────│                               │
StockDepletedConsumer                  │                               │
• marks productId in                   │                               │
  DepletedProductsTracker              │                               │
• POST /orders for that product        │                               │
  now returns 409 Conflict             │                               │
```

### Publishers & consumers

| Service | Publishes | Consumes |
|---|---|---|
| **OrderService** | `OrderPlaced` — after a new order is saved | `StockDepleted` — via `StockDepletedConsumer` |
| **ProductService** | `StockDepleted` — when a product's stock reaches zero | `OrderPlaced` — via `OrderPlacedConsumer` |

### RabbitMQ Management UI

While the stack is running, open **http://localhost:15672** (credentials: `guest` / `guest`) to inspect exchanges, queues, and message rates in real time.

## API Endpoints

### ProductService (`http://localhost:5043`)

| Method | Route | Description |
|---|---|---|
| `GET` | `/products` | List all products |
| `GET` | `/products/{id}` | Get a product by ID |
| `POST` | `/products` | Create a product |
| `PUT` | `/products/{id}` | Update a product |
| `DELETE` | `/products/{id}` | Delete a product |

**Product schema:**
```json
{
  "id": 1,
  "name": "Widget",
  "price": 9.99,
  "description": "A useful widget"
}
```

OpenAPI spec available at `http://localhost:5043/openapi/v1.json` when running in Development mode.

### OrderService (`http://localhost:5044`)

| Method | Route | Body | Description |
|---|---|---|---|
| `GET` | `/orders` | — | List all orders (newest first) |
| `GET` | `/orders/{id}` | — | Get an order by ID |
| `POST` | `/orders` | `{ "productId": 1, "quantity": 3 }` | Place an order |
| `PUT` | `/orders/{id}` | `{ "quantity": 5 }` | Update order quantity |
| `DELETE` | `/orders/{id}` | — | Delete an order |

When creating or updating an order, OrderService calls ProductService to verify the product exists and snapshots `price × quantity` as `totalPrice`.

**Order schema:**
```json
{
  "id": 1,
  "productId": 1,
  "quantity": 3,
  "totalPrice": 29.97,
  "orderedAt": "2026-03-06T10:00:00Z"
}
```

OpenAPI spec available at `http://localhost:5044/openapi/v1.json` when running in Development mode.

## Getting Started

### Prerequisites

- [Docker](https://docs.docker.com/get-docker/) and Docker Compose

That's it — no local .NET SDK or SQL Server installation required for running the app.

### Run with Docker Compose

```bash
git clone <repo-url>
cd MyMicroServiceProject
sudo docker compose up --build
```

Then open **http://localhost:5200** in your browser.

> On first startup the databases are created and EF Core migrations are applied automatically. SQL Server can take ~30 s to become ready; the services will retry the connection automatically.

### Stop

```bash
sudo docker compose down
```

To also remove the persistent database volumes:

```bash
sudo docker compose down -v
```

## Debugging with Docker

The repository includes `docker-compose.debug.yml`, a Compose override that builds `ProductService` and `OrderService` with their `Dockerfile.debug` files instead of the production `Dockerfile`.

**What `Dockerfile.debug` does differently:**
- Compiles in `Debug` configuration (preserves PDB symbol files).
- Installs `vsdbg` (the VS Code / Visual Studio remote debugger) at `/vsdbg` inside the container.

### Start the stack in debug mode

```bash
sudo docker compose -f docker-compose.yml -f docker-compose.debug.yml up --build
```

### Attach a debugger (VS Code)

1. Install the [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) extension.
2. Create `.vscode/launch.json` with an entry for each service you want to attach to:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Attach: ProductService (Docker)",
      "type": "coreclr",
      "request": "attach",
      "processId": "${command:pickProcess}",
      "pipeTransport": {
        "pipeProgram": "docker",
        "pipeArgs": ["exec", "-i", "productservice"],
        "debuggerPath": "/vsdbg/vsdbg",
        "pipeCwd": "${workspaceFolder}"
      },
      "sourceFileMap": {
        "/src": "${workspaceFolder}/ProductService"
      }
    },
    {
      "name": "Attach: OrderService (Docker)",
      "type": "coreclr",
      "request": "attach",
      "processId": "${command:pickProcess}",
      "pipeTransport": {
        "pipeProgram": "docker",
        "pipeArgs": ["exec", "-i", "orderservice"],
        "debuggerPath": "/vsdbg/vsdbg",
        "pipeCwd": "${workspaceFolder}"
      },
      "sourceFileMap": {
        "/src": "${workspaceFolder}/OrderService"
      }
    }
  ]
}
```

3. Open the **Run and Debug** panel (`Ctrl+Shift+D`), select the desired configuration, and press **F5**.
4. When prompted by `pickProcess`, choose the `dotnet` process running the service DLL.

### Stop the debug stack

```bash
sudo docker compose -f docker-compose.yml -f docker-compose.debug.yml down
```

> **Tip:** The `vsdbg` installation is in its own Docker layer. After the images are built once, subsequent rebuilds that only change application code will reuse the cached `vsdbg` layer and be significantly faster.

## Project Structure

```
MyMicroServiceProject/
├── docker-compose.yml          # Orchestrates all services
├── MyMicroServiceProject.sln
│
├── ProductService/             # ASP.NET Core Minimal API
│   ├── Program.cs              # All routes defined here
│   ├── Models/Product.cs
│   ├── Data/AppDbContext.cs
│   ├── Migrations/
│   └── Dockerfile
│
├── OrderService/               # ASP.NET Core Minimal API
│   ├── Program.cs              # All routes defined here
│   ├── Models/Order.cs
│   ├── Models/ProductDto.cs    # DTO for ProductService responses
│   ├── Services/ProductServiceClient.cs  # HTTP client for ProductService
│   ├── Data/OrderDbContext.cs
│   ├── Migrations/
│   └── Dockerfile
│
└── MyClient/                   # Blazor WebAssembly front-end
    ├── Program.cs
    ├── Pages/                  # Products, Orders, ProductCreate, ProductEdit, …
    ├── Services/               # ProductApiService, OrderApiService
    ├── Models/
    ├── wwwroot/appsettings.json  # Service URLs (overridden by nginx in Docker)
    └── Dockerfile              # Build → nginx serve
```

## Local Development (without Docker)

You will need the [.NET 10 SDK](https://dotnet.microsoft.com/download) and a SQL Server instance (or use the Docker databases).

```bash
# Terminal 1 — ProductService
cd ProductService
dotnet run

# Terminal 2 — OrderService
cd OrderService
dotnet run

# Terminal 3 — Blazor client
cd MyClient
dotnet run
```

Update `MyClient/wwwroot/appsettings.json` with the correct service URLs if they differ from the defaults.

### Adding EF Core Migrations

```bash
# ProductService
cd ProductService
dotnet ef migrations add <MigrationName>

# OrderService
cd OrderService
dotnet ef migrations add <MigrationName>
```

Migrations are applied automatically at service startup, so no manual `dotnet ef database update` is needed.
