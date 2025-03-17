# BlvckAuth - User Authentication API

## Description

This C# .NET Web API provides endpoints for user authentication, JWT token generation, and user management, including registration and login.
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

To set up the project locally.

**Clone repository**

```bash
git clone https://github.com/Blvckout-dev/blvckauth-api.git
```

**Switch working directory**

Navigate to the root directory of the project:

```bash
cd blvckauth-api
```

### Configuration

You can configure the application in one of the following ways:

- Set them directly as environment variables.
- Use a .env file in combination with Docker.
- For local development, you can use the appsettings.json.

**Change directory**

Navigate to the source directory of the `blvckauth-api` project:

```bash
cd src
```

#### Database

Set up the connection to your MySQL database.

1. Set up MySQL and create a database.
2. Set the connection string:

```env
Database__ConnectionString="Server=mysql_server;Database=database;User=user;Password=password;"
```

3. Control data seeding in development mode by setting the `SeedData` option:

- **Note:** The `SeedData` variable is only applicable in development mode. Data seeding is disabled in production.
- **Default:** The default value is set to `false`.

```env
Database__SeedData=false
```

#### JWT

Responsible for signing and verifying JWT tokens.

1. Generate a strong JWT Key.
2. Set the JWT Key:

```env
Jwt__Key="strongJwtKey"
```

3. Set the JWT Issuer:

```env
Jwt__Issuer="blvckauth-api"
```

4. Set the JWT Audience:

```env
Jwt__Audience="blvckout"
```

#### Admin User

Creates a user who is assigned to the Admin role, which grants access to all endpoints.

>**Note:** Providing administrator credentials is optional. However, aside from direct database insertion, this is the only method available for registering an administrator account.

1. Generate a strong password for the Admin User.
2. Set the Admin username and password:
   
```env
Admin__Username="admin"
Admin__Password="strongPass"
```

#### Example Configuration

Here’s an example `.env` configuration for local development:

```env
Database__ConnectionString=Server=mysql_server;Database=database;User=user;Password=password;
Database__SeedData=false
Jwt__Key=strongJwtKey
Jwt__Issuer=blvckauth-api
Jwt__Audience=blvckout
Admin__Username=admin
Admin__Password=strongPass

ASPNETCORE_ENVIRONMENT=Development
LOGGING__LOGLEVEL__DEFAULT=Debug
```
As well as an example `appsettings.json` for local development:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Database": {
    "ConnectionString": "Server=mysql_server;Database=database;User=user;Password=password;",
    "SeedData": false
  },
  "Jwt": {
    "Key": "strongJwtKey",
    "Issuer": "blvckauth-api",
    "Audience": "blvckout"
  },
  "Admin": {
    "Username": "admin",
    "Password": "strongPass"
  }
}
```

### Apply Migrations

Migrations ensure that your database schema is in sync with your application's data models by applying any necessary schema changes.

>**Note:** Ensure that your database server is running and accessible before applying migrations.

**Check for `dotnet-ef`**

```bash
dotnet ef --version
```

**Install `dotnet-ef` tool**

```bash
dotnet tool install --global dotnet-ef
```

**Run the migrations**

```bash
dotnet ef database update
```

**Change directory**

Navigate back to the root directory of the `blvckauth-api` project:

```bash
cd ..
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
dotnet run --project src/blvckauth-api.csproj
```

### Docker Setup

To containerize and run the application using Docker, follow these steps:

**Change directory**

Navigate into the source directory of `blvckauth-api` project, where the Dockerfile is located:

```bash
cd src
```

**Build Docker Image**

```bash
docker build -t <yourusername>/blvckauth-api:latest .
```

**Run Docker Container**

The following docker run command expects a local `.env` file with the application configuration:

```bash
docker run -d -p 5001:8080 --env-file .env --name <containerName> <yourusername>/blvckauth-api:latest
```

**Change directory**

Navigate back to the root directory of the `blvckauth-api` project:

```bash
cd ..
```

## Usage

### Running the Application

Once the application is running, you can access it at `http://localhost:5001`.

### Interacting with the API

#### Using `curl`

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
- **Trigger:** Every push to the repository's **main** branch.
- **Process:**
  - Builds and tests the application.
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

- Ensure that the Docker image references in the `src/k8s/deployment.yml` file match the images pushed to Docker Hub.
- Update any environment variables or secrets as required in the manifests.

#### Secrets

Use Kubernetes Secrets for sensitive data like connection strings and JWT keys:

**Change directory**

Navigate into the k8s directory of `blvckauth-api` project:

```bash
cd src/k8s
```

**Create `.env.secrets`**

Here’s an example `.env.secrets` configuration for local development:

```env
Database__ConnectionString=Server=mysql_server;Database=database;User=user;Password=password;
Jwt__Key=strongJwtKey
Jwt__Audience=blvckout
Jwt__Issuer=blvckauth-api
Admin__Username=admin
Admin__Password=strongPass
```

**Create secret**

```bash
kubectl create secret generic blvckauth-api --from-env-file=.env.secrets
```

#### Apply Kubernetes Manifests

**Apply service**

```bash
kubectl apply -f service.yml
```

**Apply deployment**

```bash
kubectl apply -f deployment.yml
```

#### Verify Deployment

**Check deployments status**

```bash
kubectl get deployments
```

**Check services status**

```bash
kubectl get services
```

**Change directory**

Navigate back to the root directory of the `blvckauth-api` project:

```bash
cd ../..
```

## License

This project is **not licensed** under any open-source license. 

All rights are reserved. You may not copy, distribute, or modify this code or any part of this project without explicit permission from the author.

## Contact

- Maintainer: Maximilian Bauer
- GitHub: Blvckout-dev
