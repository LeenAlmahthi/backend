# TrackUp Backend Naming Guide

## 1. Purpose

This document defines the naming conventions for files, classes, interfaces, and functions in the TrackUp backend.

Follow these conventions when adding new features to keep the project consistent and easy to maintain.

## 2. File and Class Naming

Use **PascalCase** for C# class and interface filenames.

| Purpose | File name | Class or interface name |
|---|---|---|
| Registration request DTO | `SignUpRequest.cs` | `SignUpRequest` |
| Registration feature | `Register.cs` | `Register` |
| Authentication interface | `IAuthService.cs` | `IAuthService` |
| Registration validation | `ValidateSignUpRequest.cs` | `ValidateSignUpRequest` |
| User entity | `ApplicationUser.cs` | `ApplicationUser` |
| Authentication controller | `AuthController.cs` | `AuthController` |
| Database context | `TrackUpDataContext.cs` | `TrackUpDataContext` |

### Rules

- Use PascalCase for class and interface names.
- Prefix interface names with `I`, for example, `IAuthService`.
- Use descriptive names that explain the responsibility of the file.
- Keep filenames consistent with their primary class or interface.
- Use `Request` for DTOs representing incoming API requests.
- Use `Response` for DTOs representing outgoing API responses.

## 3. Function Naming

Use **PascalCase** for public and private C# methods.

Use verbs that describe the action performed by the method.

| Responsibility | Recommended method name | Notes |
|---|---|---|
| Register a user | `RegisterAsync` | Creates an account |
| Log in a user | `LoginAsync` | Verifies login credentials |
| Find a user by email | `FindByEmailAsync` | Prefer Identity's built-in method |
| Generate an access token | `GenerateAccessToken` | Use `Async` only when the method performs asynchronous work |
| Validate registration data | `ValidateSignUpRequest` | Validates the request |
| Check whether an email exists | `EmailExistsAsync` | Checks for an existing account |
| Check whether a username exists | `UserNameExistsAsync` | Only if TrackUp uses usernames |

## 4. Authentication and Identity

### ApplicationUser

File: `Domain/Model/ApplicationUser.cs`

Represents a user in the application and inherits from ASP.NET Core Identity's `IdentityUser`.

### UserManager

Use `UserManager<ApplicationUser>` to manage user accounts.

Examples:

- `CreateAsync` — creates a user.
- `FindByEmailAsync` — finds a user by email.
- `CheckPasswordAsync` — verifies a user's password.
- `FindByNameAsync` — finds a user by username, if needed.

Prefer Identity's built-in methods instead of implementing the same operations yourself.

### Access Token

Use a clearly named method such as `GenerateAccessToken` when implementing token generation.

The method should be placed in the authentication service or a dedicated token service, depending on the project's architecture.

Use `GenerateAccessTokenAsync` only if the implementation genuinely needs asynchronous operations.

## 5. Folder Responsibilities

### Application/DTOS

Contains request and response DTOs used to transfer data through the application API.

Examples:

- `SignUpRequest.cs`
- `LoginRequest.cs`

### Application/Feature

Contains application use cases and feature logic.

Examples:

- `Register.cs`
- `Login.cs` (when implemented)

### Application/Interfaces

Contains interfaces defining contracts for application services.

Example:

- `IAuthService.cs`

### Application/validation

Contains request validation logic.

Example:

- `ValidateSignUpRequest.cs`

### Domain/Model

Contains domain models, including the application's user model.

Example:

- `ApplicationUser.cs`

### Infrastructure/Data

Contains database access configuration and the Entity Framework Core context.

Example:

- `TrackUpDataContext.cs`

### Infrastructure/SqlAuth

Contains authentication infrastructure components that interact with external or persistence-related mechanisms, if needed.

### Presentation/Controllers

Contains API controllers that receive HTTP requests and return HTTP responses.

Example:

- `AuthController.cs`

## 6. General Rules

1. Use meaningful names instead of abbreviations.
2. Use `Async` for asynchronous methods returning `Task` or `Task<T>`.
3. Keep controllers focused on HTTP handling.
4. Keep business and application workflows out of controllers when a service or feature class is responsible for them.
5. Use ASP.NET Core Identity for user management and password hashing.
6. Never store a plaintext password or a confirmation password in `ApplicationUser`.
7. Avoid duplicating functionality already provided by Identity.
8. Keep naming consistent across files, classes, interfaces, and methods.

## 7. Before Adding a New File

Ask yourself:

- What responsibility does this file have?
- Which folder should contain it?
- What should the class or interface be named?
- Does an existing Identity method already provide the functionality?
- Should this operation be asynchronous?
- Does the method name clearly describe what it does?
