# Agent Guidelines for ShitpostBot C# Projects

## Build/Test Commands
- **Build solution**: `dotnet build` (from this directory)
- **Run all tests**: `dotnet test`
- **Run single test**: `dotnet test --filter "FullyQualifiedName~ClassName.MethodName"`
- **Add migration**: `dotnet ef migrations add <MigrationName> --project src/ShitpostBot.Infrastructure`
- **Update database**: `dotnet ef database update --project src/ShitpostBot.Infrastructure`

## E2E Testing
- **Run E2E tests**: `./test/e2e/run-e2e-tests.sh` (from repository root)
- **Purpose**: High-level validation of repost detection with real services
- **When**: After changes to repost handlers, image processing, or test endpoints
- **Important**: Must be run from repository root, not from this directory

## Project Structure
- **Domain**: Entities, domain models, repository interfaces (no dependencies)
- **Infrastructure**: EF Core, repositories, readers, DB context, migrations, Discord client
- **Worker**: MediatR handlers, bot commands, event listeners, background service
- **Tools**: Utility tools (e.g., SendMessageTool)
- **Tests.Unit**: xUnit unit tests with FluentAssertions
- **Tests.Integration**: Integration tests with Testcontainers

## Code Style
- **Framework**: .NET 10.0, nullable enabled, implicit usings enabled
- **Formatting**: 4 spaces indentation (see `.editorconfig`)
- **Types**: Use `var` for built-in types and when type is apparent
- **Naming**: Interfaces prefixed with `I` (PascalCase), no `this.` qualifier
- **Namespaces**: File-scoped namespaces. Skip folders named `Base`, `Posts`, `Services` in namespace path
- **Constructors**: Use primary constructors where applicable (handlers, repositories, services)
- **Modifiers**: Order: public, private, protected, internal, file, new, static, abstract, virtual, sealed, readonly, override, extern, unsafe, volatile, async, required
- **Properties**: Private setters for encapsulation in domain entities
- **Records**: Use records for DTOs/identifiers (e.g., `ChatMessageIdentifier`, `PosterIdentifier`)
- **Access**: Implementation classes are `internal`, exposed via DI. Use `InternalsVisibleTo` for test access
- **Async**: Always pass `CancellationToken` to async methods; no `ConfigureAwait(false)` needed
- **Dependencies**: Register services in `DependencyInjection.cs` (public extension methods)
- **Testing**: xUnit with `[Theory]`, `[InlineData]`, FluentAssertions for assertions
- **Patterns**: MediatR for commands/events, MassTransit for messaging, Refit for HTTP clients
