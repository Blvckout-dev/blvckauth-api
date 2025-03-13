# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env

# Set the working directory for build
WORKDIR /src

# Copy the project file and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the application code
COPY . ./

# Build and publish the application
RUN dotnet publish -c Release -o /app

# Stage 2: Create the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Set the working directory for runtime
WORKDIR /app

# Copy the application from the build stage
COPY --from=build-env /app .

# Set environment variables
ENV DOTNET_EnableDiagnostics=0

# Expose the port the app runs on
EXPOSE 8080

# Specify the entry point for the application
ENTRYPOINT ["dotnet", "BlvckAuth.dll"]