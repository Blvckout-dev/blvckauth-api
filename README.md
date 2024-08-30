# my-masternode User Authentication API

## Description

This is a C# .NET Web API for user authentication and JWT token generation as well as user management.
It provides endpoints for user registration, login, and user management.
The application is deployed using GitHub Actions, Docker Hub, and Kubernetes.

## Table of Contents

1. [Tech Stack](#tech-stack)
2. [Installation](#installation)
3. [Usage](#usage)
4. [Endpoints](#endpoints)
6. [Deployment](#deployment)
7. [License](#license)
8. [Contact](#contact)

## Tech Stack

- **Programming Language:** C#
- **Framework:** .NET 8
- **Authentication:** JWT with Bearer
- **Authorization:** ASP.NET Core Authorization Policies
- **Database:** MySQL
- **ORM:** Entity Framework Core (EF Core)
- **Testing Framework**: xUnit
- **Mocking Framework**: Moq
- **Logging**: Microsoft.Extensions.Logging
- **Object-Object Mapping:** AutoMapper
- **Migration Tool:** dotnet-ef
- **Deployment:**
  - **CI:** GitHub Actions
  - **Containerization:** Docker
  - **Orchestration:** Kubernetes

## Installation

### Prerequisites

- .NET 8 SDK
- MySQL
- dotnet-ef CLI tool
- Docker
- Kubernetes (Minikube or any other K8s environment)

### Clone Repositories

To set up the project locally, you need to clone both the main application repository and the data models repository.

**Clone main repository**
```bash
git clone https://github.com/Bl4ckout-dev/my-masternode-auth.git
```

**Clone data models repository**
```bash
git clone https://github.com/Bl4ckout-dev/my-masternode-data-models.git
```

**Switch working directory**
```bash
cd my-masternode-auth
```

### Configuration

For local development, you can set environment variables directly, use a `.env` file in combination with Docker, or modify the `appsettings.json`.

#### Database

Set up the connection to your MySQL database.

1. Set up MySQL and create a database.
2. Set the connection string as an environment variable.

```env
Database__ConnectionString="Server=mysql_server;Database=database;User=user;Password=password;"
```

3. Control data seeding in development mode by setting the `SeedData` environment variable:

- **Note:** The `SeedData` variable is only applicable in development mode. Data seeding is disabled in production.
- **Default:** The default value is set to `true`.

```env
Database__SeedData=true
```


#### JWT Key

Responsible for signing and verifying JWT tokens.

1. Generate a strong JWT Key.
2. Set the JWT Key as an environment variable:

```env
Jwt__Key="strongJwtKey"
```

#### Admin User

Creates an user who is assigned to the admin role, which grants access to all endpoints.

>**Note:** Providing administrator credentials is optional. However, aside from direct database insertion, this is the only method available for registering an administrator account.

1. Generate a strong password for the Admin User.
2. Set the Admin username and password as environment variables:
   
```env
Admin__Username="admin"
Admin__Password="strongPass"
```

#### Example Configuration

Here’s an example `.env` configuration for local development:

```env
Database__ConnectionString="Server=mysql_server;Database=database;User=user;Password=password;"
Database__SeedData=true
Jwt__Key="strongJwtKey"
Admin__Username="admin"
Admin__Password="strongPass"
```

### Apply Migrations

Migrations ensure that your database schema is in sync with your application's data models by applying any necessary schema changes.

>**Note:** Ensure that your database server is running and accessible before applying migrations.

**Check for `dotnet-ef`**:
```bash
dotnet ef --version
```

**Install `dotnet-ef` tool**
```bash
dotnet tool install --global dotnet-ef
```

**Run the migrations**
```bash
dotnet ef database update --project src/my-masternode-auth.csproj
```

### Build and Run Locally

To set up the application locally, follow these steps:

**Restore Dependencies**
```bash
dotnet restore
```

**Build the Application**
```bash
dotnet build
```

**Run Unit Tests**
```bash
dotnet test
```

**Run the Application**
```bash
dotnet run
```

### Docker Setup

To containerize and run the application using Docker, follow these steps:

**Build Docker Image**
```bash
docker build -t yourusername/my-masternode-auth:latest .
```

**Run Docker Container**
```bash
docker run -p 5001:8080 --env-file .env yourusername/my-masternode-auth:latest
```

## Usage

### Running the Application

Once the application is running, you can access it at `http://localhost:5001`.

### Interacting with the API

#### Using `curl`:

To test the register and login functionality, you can use the following `curl` commands:

**Register new user**
```bash
curl -v -X POST http://localhost:5001/api/auth/register -H "Content-Type: application/json" -d '{"username":"user", "password":"pass"}'
```

**Login as newly created user**
```bash
curl -v -X POST http://localhost:5001/api/auth/login -H "Content-Type: application/json" -d '{"username":"user", "password":"pass"}'
```

## Endpoints

### Authentication

<details>
  <summary>Register</summary>
  
</br>
  
  Register a new user.
  
  `POST /api/auth/register`

  **Authentication**: none
  
  Request body:
  ```json
  {
    "username": "string",
    "password": "string"
  }
  ```
</details>

<details>
  <summary>Login</summary>
  
</br>
  
  Authenticate a user and receive a JWT token.
  
  `POST /api/auth/login`

  **Authentication**: none
  
  Request body:
  ```json
  {
    "username": "string",
    "password": "string"
  }
  ```
  
  Response body:
  ```json
  {
    "token": "string"
  }
  ```
</details>

### Users

<details>
  <summary>List</summary>
  
</br>

  Fetches a list of users. \
  You can optionally include the list of scope IDs associated with each user by using the includeScopeIds query parameter.
  
  `GET /api/users[?includeScopeIds=<bool>]`

  **Authentication**: Bearer Token \
  **Authorization**:
  | Role    | Scope       |
  | ------- | ----------- |
  | `Admin` | none        |
  | none    | `user.read` |

  Response body:
  ```json
  [
    {
      "id": "int",
      "username": "string",
      "roleId": "int"
    }
  ]
  ```
  
  Response body with `includeScopeIds=true`:
  ```json
  [
    {
      "id": "int",
      "username": "string",
      "roleId": "int",
      "scopeIds": [
        "int",
        "int"
      ]
    }
  ]
  ```
</details>

<details>
  <summary>Detail</summary>
  
</br>

  Fetch a user's profile by ID.
  
  `GET /api/users/{id}`

  **Authentication**: Bearer Token \
  **Authorization**:
  | Role    | Scope       |
  | ------- | ----------- |
  | `Admin` | none        |
  | none    | `user.read` |
  
  Response body:
  ```json
  {
    "id": "int",
    "username": "string",
    "role": {
      "id": "int",
      "name": "string"
    },
    "scopes": [
      {
        "id": "int",
        "name": "string"
      },
      {
        "id": "int",
        "name": "string"
      }
    ]
  }
  ```
</details>

<details>
  <summary>Create</summary>
  
</br>

  Creates a new user.
  
  `POST /api/users`

  **Authentication**: Bearer Token \
  **Authorization**:
  | Role    | Scope         |
  | ------- | ------------- |
  | `Admin` | none          |
  | none    | `user.create` |
  
  Request body:
  ```json
  {
    "username": "string",
    "password": "string",
    "roleId": "int(optional)"
  }
  ```
  
  Response body:
  ```json
  {
    "id": "int",
    "username": "string",
    "roleId": "int",
    "scopeIds": []
  }
  ```
</details>

<details>
  <summary>Update</summary>
  
</br>

  Updates a user's information using a JSON Patch request.
  
  `PATCH /api/users/{id}`

  **Authentication**: Bearer Token \
  **Authorization**:
  | Role    | Scope        |
  | ------- | ------------ |
  | `Admin` | none         |
  | none    | `user.write` |
  
  Request body:
  ```json
  [
    {
      "op": "string",
      "path": "string",
      "value": "string"
    }
  ]
  ```
</details>

<details>
  <summary>Delete</summary>
  
</br>
  
  Deletes a user.
  
  `DELETE /api/users/{id}`

  **Authentication**: Bearer Token \
  **Authorization**:
  | Role    | Scope         |
  | ------- | ------------- |
  | `Admin` | none          |
  | none    | `user.delete` |
</details>

<details>
  <summary>Add Scopes</summary>
  
</br>

  Adds new scopes to an existing user.
  
  `POST /api/users/{id}/scopes`

  **Authentication**: Bearer Token \
  **Authorization**:
  | Role    | Scope        |
  | ------- | ------------ |
  | `Admin` | none         |
  | none    | `user.write` |
  
  Request body:
  ```json
  [
    "int",
    "int"
  ]
  ```
</details>

<details>
  <summary>Remove Scopes</summary>
  
</br>

  Removes scopes from an existing user.
  
  `DELETE /api/users/{id}/scopes`

  **Authentication**: Bearer Token \
  **Authorization**:
  | Role    | Scope        |
  | ------- | ------------ |
  | `Admin` | none         |
  | none    | `user.write` |
  
  Request body:
  ```json
  [
    "int",
    "int"
  ]
  ```
</details>

## Deployment

### CI/CD Pipeline

#### Continuous Integration (CI)

- **Tool:** GitHub Actions
- **Trigger:** Every push to the repository's **release** branch.
- **Process:**
  - Builds the application.
  - Packages the application as a Docker image.
  - Pushes the Docker image to **Docker Hub**.

#### Continuous Deployment (CD)

- **Status:** *Not currently configured.*
- **Note:** Deployment to any environment (staging, production, etc.) must be performed manually.

### Kubernetes

#### Prerequisites

- Ensure you have access to a Kubernetes cluster.
- `kubectl` is installed and configured to interact with your cluster.

#### Configuration

- Ensure that the Docker image references in the `k8s/deployment.yaml` file match the images pushed to Docker Hub.
- Update any environment variables or secrets as required in the manifests.

#### Secrets Configuration

Use Kubernetes Secrets for sensitive data like connection strings and JWT keys:

**Connection String**
```bash
kubectl create secret generic my-masternode-auth --from-literal=DatabaseConnectionString="Server=myser..."
```

**Jwt Key**
```bash
kubectl create secret generic my-masternode-auth --from-literal=JwtKey="strongKey"
```

#### Apply Kubernetes Manifests

```bash
kubectl apply -f k8s/deployment.yaml
```
```bash
kubectl apply -f k8s/service.yaml
```

#### Verify Deployment

Check the status of deployments:
```bash
kubectl get deployments
```

Check the status of services:
```bash
kubectl get services
```

## License

This project is **not licensed** under any open-source license. 

All rights are reserved. You may not copy, distribute, or modify this code or any part of this project without explicit permission from the author.

## Contact

- Maintainer: Maximilian Bauer
- GitHub: Bl4ckout-dev