# Newtonsoft.Json to System.Text.Json Migration Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace all Newtonsoft.Json usage with System.Text.Json across the ShitpostBot C# codebase.

**Architecture:** Systematic replacement of JSON serialization library while maintaining compatibility with Refit, EF Core, and existing functionality.

**Tech Stack:** .NET 10.0, System.Text.Json, Refit, Entity Framework Core, PostgreSQL

## Task 1: Update Package Dependencies

**Files:**
- Modify: `Directory.Packages.props`
- Modify: `src/ShitpostBot.Application/ShitpostBot.Application.csproj`
- Modify: `src/ShitpostBot.Domain/ShitpostBot.Domain.csproj`
- Modify: `src/ShitpostBot.Worker/ShitpostBot.Worker.csproj`

**Step 1: Remove Newtonsoft.Json packages from Directory.Packages.props**

```xml
<!-- Remove these lines -->
<PackageVersion Include="Newtonsoft.Json" Version="13.0.4" />
<PackageVersion Include="Refit.Newtonsoft.Json" Version="9.0.2" />
```

**Step 2: Add System.Text.Json Refit package**

```xml
<!-- Add this line -->
<PackageVersion Include="Refit.HttpClientFactory" Version="9.0.2" />
```

**Step 3: Remove package references from project files**

```xml
<!-- Remove from ShitpostBot.Application.csproj -->
<PackageReference Include="Refit.Newtonsoft.Json"/>
<PackageReference Include="Newtonsoft.Json"/>

<!-- Remove from ShitpostBot.Domain.csproj -->
<PackageReference Include="Newtonsoft.Json"/>

<!-- Remove from ShitpostBot.Worker.csproj -->
<PackageReference Include="Refit.Newtonsoft.Json"/>
```

**Step 4: Commit**

```bash
git add Directory.Packages.props src/ShitpostBot.Application/ShitpostBot.Application.csproj src/ShitpostBot.Domain/ShitpostBot.Domain.csproj src/ShitpostBot.Worker/ShitpostBot.Worker.csproj
git commit -m "chore: remove Newtonsoft.Json package references"
```

## Task 2: Update Refit Configuration

**Files:**
- Modify: `src/ShitpostBot.Application/DependencyInjection.cs`

**Step 1: Write failing test**

```csharp
// Test that Refit client can be created with System.Text.Json
[Test]
public void AddRefitClient_ShouldUseSystemTextJson()
{
    // This test will fail until we update the configuration
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder().Build();
    
    services.AddShitpostBotApplication(configuration);
    
    var serviceProvider = services.BuildServiceProvider();
    var refitClient = serviceProvider.GetService<IImageFeatureExtractorApi>();
    
    Assert.That(refitClient, Is.Not.Null);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/ShitpostBot.Application.Tests --filter "AddRefitClient_ShouldUseSystemTextJson"`
Expected: FAIL due to NewtonsoftJsonContentSerializer not found

**Step 3: Update Refit configuration to use System.Text.Json**

```csharp
using System.Text.Json;
using System.Net.Http.Json;
// ... other usages

services.AddRefitClient<IImageFeatureExtractorApi>(
        new RefitSettings(new SystemTextJsonContentSerializer(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        })))
    .ConfigureHttpClient((sp, client) =>
    {
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ImageFeatureExtractorApiOptions>>().Value;
        client.BaseAddress = new Uri(options.Uri);
    });
```

**Step 4: Run test to verify it passes**

Run: `dotnet test src/ShitpostBot.Application.Tests --filter "AddRefitClient_ShouldUseSystemTextJson"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/ShitpostBot.Application/DependencyInjection.cs
git commit -m "feat: migrate Refit to System.Text.Json"
```

## Task 3: Update API Response Model

**Files:**
- Modify: `src/ShitpostBot.Application/Services/IImageFeatureExtractorApi.cs`

**Step 1: Write failing test**

```csharp
[Test]
public void ExtractImageFeaturesResponse_ShouldSerializeCorrectly()
{
    var response = new ExtractImageFeaturesResponse
    {
        ImageUrl = "https://example.com/image.jpg",
        ImageFeatures = new float[] { 0.1f, 0.2f, 0.3f }
    };
    
    var json = JsonSerializer.Serialize(response);
    var deserialized = JsonSerializer.Deserialize<ExtractImageFeaturesResponse>(json);
    
    Assert.That(deserialized.ImageUrl, Is.EqualTo(response.ImageUrl));
    Assert.That(deserialized.ImageFeatures, Is.EqualTo(response.ImageFeatures));
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/ShitpostBot.Application.Tests --filter "ExtractImageFeaturesResponse_ShouldSerializeCorrectly"`
Expected: FAIL due to JsonProperty attributes not working

**Step 3: Update model to use System.Text.Json attributes**

```csharp
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Refit;

public interface IImageFeatureExtractorApi
{
    [Get("/images/features")]
    Task<ExtractImageFeaturesResponse> ExtractImageFeaturesAsync([AliasAs("image_url")] string imageUrl);
}

public record ExtractImageFeaturesResponse
{
    [JsonPropertyName("image_url")] public required string ImageUrl { get; init; }
    [JsonPropertyName("image_features")] public required float[] ImageFeatures { get; init; }
}

public class ImageFeatureExtractorApiOptions
{
    [Required] public required string Uri { get; init; }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test src/ShitpostBot.Application.Tests --filter "ExtractImageFeaturesResponse_ShouldSerializeCorrectly"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/ShitpostBot.Application/Services/IImageFeatureExtractorApi.cs
git commit -m "feat: migrate API response model to System.Text.Json"
```

## Task 4: Replace Database JSON Configuration

**Files:**
- Modify: `src/ShitpostBot.Infrastructure/Public/Config.cs`
- Delete: `src/ShitpostBot.Infrastructure/Internal/Configurations/PrivatePropertyResolver.cs`

**Step 1: Write failing test**

```csharp
[Test]
public void Config_ShouldProvideSystemTextJsonOptions()
{
    var options = Config.DatabaseJsonSerializerOptions;
    
    Assert.That(options, Is.Not.Null);
    Assert.That(options.PropertyNamingPolicy, Is.EqualTo(JsonNamingPolicy.CamelCase));
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/ShitpostBot.Infrastructure.Tests --filter "Config_ShouldProvideSystemTextJsonOptions"`
Expected: FAIL due to DatabaseJsonSerializerOptions not existing

**Step 3: Replace Config.cs with System.Text.Json options**

```csharp
using System.Text.Json;

namespace ShitpostBot.Infrastructure;

public static class Config
{
    public static readonly JsonSerializerOptions DatabaseJsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
}
```

**Step 4: Delete PrivatePropertyResolver.cs**

```bash
rm src/ShitpostBot.Infrastructure/Internal/Configurations/PrivatePropertyResolver.cs
```

**Step 5: Run test to verify it passes**

Run: `dotnet test src/ShitpostBot.Infrastructure.Tests --filter "Config_ShouldProvideSystemTextJsonOptions"`
Expected: PASS

**Step 6: Commit**

```bash
git add src/ShitpostBot.Infrastructure/Public/Config.cs
git rm src/ShitpostBot.Infrastructure/Internal/Configurations/PrivatePropertyResolver.cs
git commit -m "feat: replace database JSON config with System.Text.Json"
```

## Task 5: Update EF Core Configuration

**Files:**
- Modify: `src/ShitpostBot.Infrastructure/Internal/Configurations/PostConfiguration.cs`

**Step 1: Write failing test**

```csharp
[Test]
public void PostConfiguration_ShouldConfigureCorrectly()
{
    // Test that Post configuration doesn't depend on Newtonsoft.Json
    var builder = new ModelBuilder(new ConventionSet());
    var configuration = new PostConfiguration();
    
    Assert.DoesNotThrow(() => configuration.Configure(builder.Entity<Post>()));
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/ShitpostBot.Infrastructure.Tests --filter "PostConfiguration_ShouldConfigureCorrectly"`
Expected: FAIL due to missing Newtonsoft.Json using

**Step 3: Remove Newtonsoft.Json using from PostConfiguration.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.HasKey(b => b.Id);

        builder.HasDiscriminator(b => b.Type)
            .HasValue<ImagePost>(PostType.Image)
            .HasValue<LinkPost>(PostType.Link);

        builder.HasIndex(b => b.PostedOn);
        builder.HasIndex(b => b.ChatMessageId);
        builder.HasIndex(b => b.PosterId);
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test src/ShitpostBot.Infrastructure.Tests --filter "PostConfiguration_ShouldConfigureCorrectly"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/ShitpostBot.Infrastructure/Internal/Configurations/PostConfiguration.cs
git commit -m "chore: remove Newtonsoft.Json using from PostConfiguration"
```

## Task 6: Integration Testing

**Files:**
- Test: `test/ShitpostBot.Tests.Integration/MlServiceIntegrationTests.cs`

**Step 1: Run existing integration tests**

Run: `dotnet test test/ShitpostBot.Tests.Integration`
Expected: All tests pass

**Step 2: If any tests fail, update them to use System.Text.Json serialization**

**Step 3: Commit any test fixes**

```bash
git add test/ShitpostBot.Tests.Integration/
git commit -m "test: update integration tests for System.Text.Json"
```

## Task 7: Final Verification

**Files:**
- All project files

**Step 1: Build entire solution**

Run: `dotnet build src/ShitpostBot/ShitpostBot.slnx`
Expected: Build succeeds with no warnings about Newtonsoft.Json

**Step 2: Run all tests**

Run: `dotnet test src/ShitpostBot/`
Expected: All tests pass

**Step 3: Verify no Newtonsoft.Json references remain**

Run: `grep -r "Newtonsoft.Json" src/ShitpostBot/ --exclude-dir=bin --exclude-dir=obj`
Expected: No results found

**Step 4: Final commit**

```bash
git add -A
git commit -m "chore: complete Newtonsoft.Json to System.Text.Json migration"
```

## Migration Notes

### Key Differences Handled:
1. **Attribute Names**: `[JsonProperty]` → `[JsonPropertyName]`
2. **Refit Integration**: `NewtonsoftJsonContentSerializer` → `SystemTextJsonContentSerializer`
3. **Property Naming**: Explicit configuration for snake_case in API calls
4. **Private Property Resolution**: System.Text.Json handles private setters differently, simplified approach

### Testing Strategy:
- Unit tests for each component change
- Integration tests to verify API communication
- Build verification to ensure no package conflicts
- Final grep verification to ensure complete migration

### Rollback Plan:
If issues arise, the migration can be rolled back by:
1. Restoring Newtonsoft.Json packages
2. Reverting attribute changes
3. Restoring Refit configuration
4. Reverting Config.cs changes
