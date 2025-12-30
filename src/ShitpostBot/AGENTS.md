# Agent Guidelines for ShitpostBot C# Projects

## Build/Test Commands
- **Build solution**: `dotnet build` (from this directory)
- **Run all tests**: `dotnet test`
- **Run single test**: `dotnet test --filter "FullyQualifiedName~ClassName.MethodName"`
  - Example: `dotnet test --filter "FullyQualifiedName~ImagePostTests.MarkPostAsUnavailable_SetsIsPostAvailableToFalse"`
- **Run tests in specific class**: `dotnet test --filter "FullyQualifiedName~ImagePostTests"`
- **Format code**: `dotnet format` (run after making changes to ensure consistent formatting)
- **Add migration**: `dotnet ef migrations add <MigrationName> --project src/ShitpostBot.Infrastructure`
- **Update database**: `dotnet ef database update --project src/ShitpostBot.Infrastructure`
- **Clean build**: `dotnet clean && dotnet build`

## E2E Testing
- **Run E2E tests**: `./test/e2e/run-e2e-tests.sh` (from repository root)
- **Purpose**: High-level validation of repost detection with real services
- **When**: After changes to repost handlers, image processing, or test endpoints
- **Important**: Must be run from repository root, not from this directory

## Image Availability Handling (404 Errors)

When processing images, the system handles unavailable images (404) gracefully:

1. **ML Service**: Returns HTTP 404 when image URL returns 404
2. **Repost Handler**: Catches 404 via Refit's ApiException, sets `ImageFeatures` to `null`, saves post
3. **PostReevaluator**: Skips posts with `null` features and logs count

This prevents:
- Infinite retry loops for deleted images
- Messages stuck in error queues
- PostReevaluator never completing

**Database State:**
- `ImageFeatures`: `null` (image unavailable)
- `EvaluatedOn`: Set to current time (marks as processed)

**Logs to check:**
- ML Service: HTTP 404 errors in response
- Worker: "Image not found (404)" warnings (EvaluateRepost_ImagePostTrackedHandler.cs:262)
- PostReevaluator: "N posts have unavailable images (404)" (PostReevaluatorWorker.cs:44)

## Project Structure
- **Domain**: Entities, domain models, repository interfaces (no dependencies)
- **Infrastructure**: EF Core, repositories, readers, DB context, migrations, Discord client
- **Application**: MediatR handlers, bot commands, feature handlers
- **Worker**: Background service, event listeners (Discord)
- **WebApi**: Test endpoints, bot action store
- **Tools**: Utility tools (e.g., SendMessageTool)
- **Tests.Unit**: xUnit unit tests with FluentAssertions
- **Tests.Integration**: Integration tests with Testcontainers

## Code Style

### Framework & Configuration
- **Framework**: .NET 10.0, nullable enabled, implicit usings enabled
- **Formatting**: 4 spaces indentation (see `.editorconfig`). **Always run `dotnet format` after making code changes**
- **Line endings**: LF (Unix-style)
- **Encoding**: UTF-8

### Imports & Usings
- Use implicit global usings (enabled via `Directory.Build.props`)
- Global usings in `GlobalUsings.cs`: `System`, `System.Threading`, `System.Threading.Tasks`, `Microsoft.Extensions.Logging`
- Organize usings: System namespaces first, then third-party, then project namespaces
- Remove unused usings (enforced by `dotnet format`)

### Types & Variables
- **var keyword**: Use `var` for built-in types and when type is apparent from right side
- **Nullable**: Leverage nullable reference types; use `!` null-forgiving operator sparingly with justification
- **Records**: Use records for DTOs, identifiers, and immutable data (e.g., `ChatMessageIdentifier`, `PosterIdentifier`, `MessageDestination`)
- **Type inference**: Prefer explicit types when clarity improves (complex LINQ, ambiguous expressions)

### Naming Conventions
- **Interfaces**: Prefix with `I` (PascalCase): `IChatClient`, `IDbContext`, `IUnitOfWork`
- **Classes**: PascalCase: `ImagePost`, `TrackImageMessageHandler`
- **Methods**: PascalCase: `GetById`, `SaveChangesAsync`
- **Parameters/locals**: camelCase: `cancellationToken`, `utcNow`, `imageFeatures`
- **Private fields**: camelCase without prefix (not `_camelCase`)
- **No `this.` qualifier**: Omit `this.` when accessing members

### Namespaces & File Organization
- **File-scoped namespaces**: Always use file-scoped namespace declarations
- **Namespace path**: Skip folders named `Base`, `Posts`, `Services` in namespace path
  - Example: `ShitpostBot.Infrastructure.Extensions` (not `ShitpostBot.Infrastructure.Public.Extensions`)
- **One type per file**: Each class/interface/record in its own file

### Constructors & Methods
- **Primary constructors**: Use for handlers, repositories, services when all parameters are dependencies
  ```csharp
  public class TrackImageMessageHandler(
      ILogger<TrackImageMessageHandler> logger,
      IDbContext dbContext,
      IUnitOfWork unitOfWork) : INotificationHandler<ImageMessageCreated>
  ```
- **Private constructors**: For EF Core entities (parameterless), use comments `// For EF`
- **CancellationToken**: Always pass `CancellationToken` to async methods; place as last parameter with default `= default`
- **No ConfigureAwait**: Do not use `ConfigureAwait(false)` (not needed in modern .NET)

### Access Modifiers & Visibility
- **Modifier order**: public, private, protected, internal, file, new, static, abstract, virtual, sealed, readonly, override, extern, unsafe, volatile, async, required
- **Implementation classes**: Mark as `internal`; expose via dependency injection
- **Test visibility**: Use `InternalsVisibleTo` in `.csproj` for test access
- **Properties in domain**: Private setters for encapsulation
  ```csharp
  public bool IsPostAvailable { get; private set; }
  ```

### Extension Methods
- Use C# 13 extension method syntax with `extension(Type t)` pattern for query extensions
  ```csharp
  extension(IQueryable<ImagePost> query)
  {
      public Task<ImagePost?> GetById(long id, CancellationToken cancellationToken = default) { ... }
  }
  ```
- Place extension methods in `Extensions` namespace folders
- Keep extensions focused: query extensions, model extensions, etc.

### Error Handling
- **Validation**: Throw `InvalidOperationException` for invalid state/data
- **API errors**: Use Refit's `ApiException` for HTTP error handling
- **404 handling**: Catch `HttpStatusCode.NotFound` explicitly; set null features instead of retrying
- **Logging**: Log errors with structured logging (use placeholders, not string interpolation)
  ```csharp
  logger.LogError("Image not found (404) for ImagePost {ImagePostId}, URL: {ImageUrl}", postId, imageUri);
  ```

### Logging
- Use structured logging with `ILogger<T>`
- Use placeholders for values: `{PropertyName}` instead of string interpolation
- Log levels:
  - `LogDebug`: Detailed flow, skipped operations
  - `LogInformation`: Key milestones (startup, completion)
  - `LogWarning`: Degraded operations (404s, fallbacks)
  - `LogError`: Failures requiring attention

### Dependency Injection
- Register services in `DependencyInjection.cs` with public extension methods
  ```csharp
  public static IServiceCollection AddShitpostBotApplication(this IServiceCollection services, IConfiguration configuration)
  ```
- Use appropriate lifetimes: Scoped for handlers/repositories, Singleton for stateless services

### Testing
- **Framework**: xUnit with `[Fact]` and `[Theory]`
- **Assertions**: FluentAssertions (`.Should().Be()`, `.Should().NotBeNull()`)
- **Test structure**: Arrange/Act/Assert comments
- **Test naming**: `MethodName_Scenario_ExpectedBehavior`
  ```csharp
  [Fact]
  public void MarkPostAsUnavailable_SetsIsPostAvailableToFalse()
  ```
- **Test data**: Use `[InlineData]` for multiple test cases with `[Theory]`
- **Helper methods**: Extract common setup to private helper methods (e.g., `CreateTestImagePost()`)

### Patterns & Frameworks
- **MediatR**: For commands/notifications within application layer
- **MassTransit**: For messaging between services (publish/consume)
- **Refit**: For HTTP API clients (e.g., `IImageFeatureExtractorApi`)
- **EF Core**: For database access; query extensions for reusable queries
- **Repository pattern**: Via `IDbContext` exposing `DbSet<T>` properties

### Comments & Documentation
- Use XML docs (`///`) for public APIs and complex domain methods
- Inline comments for complex logic, non-obvious workarounds
- Avoid obvious comments; prefer self-documenting code
- Use `// For EF` to mark EF Core-required constructors
