# Replace NUnit with xUnit Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace NUnit with xUnit across all test projects in the ShitpostBot solution

**Architecture:** Migration from NUnit to xUnit testing framework, updating package references, attributes, and test patterns

**Tech Stack:** .NET 10.0, xUnit 2.9.3, FluentAssertions 8.8.0

## Analysis Summary

Current state:
- **Integration Tests**: Already using xUnit (ShitpostBot.Tests.Integration)
- **Unit Tests**: Using NUnit (ShitpostBot.Tests.Unit) - needs migration
- **Package Management**: Central package management in Directory.Packages.props

## Task 1: Update Unit Test Project Package References

**Files:**
- Modify: `src/ShitpostBot/test/ShitpostBot.Tests.Unit/ShitpostBot.Tests.Unit.csproj`

**Step 1: Remove NUnit package references**

Remove these lines:
```xml
<PackageReference Include="nunit"/>
<PackageReference Include="NUnit3TestAdapter"/>
```

**Step 2: Add xUnit package references**

Add these lines:
```xml
<PackageReference Include="xunit"/>
<PackageReference Include="xunit.runner.visualstudio">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

**Step 3: Run build to verify package changes**

Run: `dotnet build src/ShitpostBot/test/ShitpostBot.Tests.Unit/ShitpostBot.Tests.Unit.csproj`
Expected: Build succeeds with new packages

**Step 4: Commit package reference changes**

```bash
git add src/ShitpostBot/test/ShitpostBot.Tests.Unit/ShitpostBot.Tests.Unit.csproj
git commit -m "feat: replace NUnit packages with xUnit in unit tests"
```

## Task 2: Update Test Attributes and Using Statements

**Files:**
- Modify: `src/ShitpostBot/test/ShitpostBot.Tests.Unit/LinkTests.cs`

**Step 1: Update using statements**

Replace:
```csharp
using NUnit.Framework;
```

With:
```csharp
using Xunit;
```

**Step 2: Update test class attributes**

Replace:
```csharp
[TestFixture]
public class LinkTests
```

With:
```csharp
public class LinkTests
```

**Step 3: Update test method attributes**

Replace:
```csharp
[TestCase("https://streamable.com/xb6yrr", LinkProvider.Generic, "xb6yrr")]
[TestCase("https://www.youtube.com/watch?v=eusx0VW-m3M", LinkProvider.YouTube, "eusx0VW-m3M")]
[TestCase("https://www.youtube.com/watch?v=eusx0VW-m3M&t=60", LinkProvider.YouTube, "eusx0VW-m3M")]
[TestCase("https://youtu.be/eusx0VW-m3M", LinkProvider.YouTube, "eusx0VW-m3M")]
[TestCase("https://youtu.be/eusx0VW-m3M?t=60", LinkProvider.YouTube, "eusx0VW-m3M")]
[TestCase("https://steamcommunity.com/sharedfiles/filedetails/?id=505736710", LinkProvider.SteamWorkshop, "505736710")]
[TestCase("https://steamcommunity.com/sharedfiles/filedetails/?id=2067312913", LinkProvider.SteamWorkshop, "2067312913")]
[TestCase("https://www.idnes.cz/hry/magazin/stars-of-blood-valve-zrusena-hra.A211104_215739_bw-magazin_oma", LinkProvider.Generic, "hry/magazin/stars-of-blood-valve-zrusena-hra.A211104_215739_bw-magazin_oma")]
[TestCase("https://www.google.com", null, null)]
[TestCase("https://www.google.com/", null, null)]
[TestCase("https://tenor.com/view/nodding-moon-creepy-gif-14222607", null, null)]
[TestCase("https://media.discordapp.net/attachments/138031010951593984/905070007178911774/dontbelievehislies.gif", null, null)]
public void CreateOrDefault(string linkUri, LinkProvider? expectedLinkProvider, string? expectedLinkId)
```

With:
```csharp
[Theory]
[InlineData("https://streamable.com/xb6yrr", LinkProvider.Generic, "xb6yrr")]
[InlineData("https://www.youtube.com/watch?v=eusx0VW-m3M", LinkProvider.YouTube, "eusx0VW-m3M")]
[InlineData("https://www.youtube.com/watch?v=eusx0VW-m3M&t=60", LinkProvider.YouTube, "eusx0VW-m3M")]
[InlineData("https://youtu.be/eusx0VW-m3M", LinkProvider.YouTube, "eusx0VW-m3M")]
[InlineData("https://youtu.be/eusx0VW-m3M?t=60", LinkProvider.YouTube, "eusx0VW-m3M")]
[InlineData("https://steamcommunity.com/sharedfiles/filedetails/?id=505736710", LinkProvider.SteamWorkshop, "505736710")]
[InlineData("https://steamcommunity.com/sharedfiles/filedetails/?id=2067312913", LinkProvider.SteamWorkshop, "2067312913")]
[InlineData("https://www.idnes.cz/hry/magazin/stars-of-blood-valve-zrusena-hra.A211104_215739_bw-magazin_oma", LinkProvider.Generic, "hry/magazin/stars-of-blood-valve-zrusena-hra.A211104_215739_bw-magazin_oma")]
[InlineData("https://www.google.com", null, null)]
[InlineData("https://www.google.com/", null, null)]
[InlineData("https://tenor.com/view/nodding-moon-creepy-gif-14222607", null, null)]
[InlineData("https://media.discordapp.net/attachments/138031010951593984/905070007178911774/dontbelievehislies.gif", null, null)]
public void CreateOrDefault(string linkUri, LinkProvider? expectedLinkProvider, string? expectedLinkId)
```

**Step 4: Run tests to verify migration**

Run: `dotnet test src/ShitpostBot/test/ShitpostBot.Tests.Unit/ShitpostBot.Tests.Unit.csproj --verbosity normal`
Expected: All tests pass with xUnit runner

**Step 5: Commit test attribute changes**

```bash
git add src/ShitpostBot/test/ShitpostBot.Tests.Unit/LinkTests.cs
git commit -m "feat: migrate LinkTests from NUnit to xUnit attributes"
```

## Task 3: Clean Up Central Package Management

**Files:**
- Modify: `src/ShitpostBot/Directory.Packages.props`

**Step 1: Remove NUnit package versions**

Remove these lines:
```xml
<PackageVersion Include="Nunit" Version="4.4.0" />
<PackageVersion Include="NUnit3TestAdapter" Version="5.2.0" />
```

**Step 2: Verify xUnit versions are present**

Ensure these lines exist (they should already be there):
```xml
<PackageVersion Include="xunit" Version="2.9.3" />
<PackageVersion Include="xunit.runner.visualstudio" Version="3.1.5" />
```

**Step 3: Run full solution build**

Run: `dotnet build src/ShitpostBot/ShitpostBot.slnx`
Expected: Solution builds successfully without NUnit packages

**Step 4: Commit package management cleanup**

```bash
git add src/ShitpostBot/Directory.Packages.props
git commit -m "feat: remove NUnit packages from central package management"
```

## Task 4: Verify All Tests Run Successfully

**Files:**
- Test: All test projects

**Step 1: Run all unit tests**

Run: `dotnet test src/ShitpostBot/test/ShitpostBot.Tests.Unit/ --verbosity normal`
Expected: All unit tests pass with xUnit runner

**Step 2: Run all integration tests**

Run: `dotnet test src/ShitpostBot/test/ShitpostBot.Tests.Integration/ --verbosity normal`
Expected: All integration tests continue to pass (already using xUnit)

**Step 3: Run complete test suite**

Run: `dotnet test src/ShitpostBot/ --verbosity normal`
Expected: All tests across both projects pass

**Step 4: Final verification commit**

```bash
git add .
git commit -m "feat: complete NUnit to xUnit migration - all tests passing"
```

## Task 5: Update Documentation and Configuration

**Files:**
- Modify: `src/ShitpostBot/AGENTS.md` (if it contains testing references)
- Check: Any CI/CD configuration files

**Step 1: Check for testing documentation references**

Search for "NUnit" references in documentation:
Run: `grep -r "NUnit" src/ShitpostBot/ --exclude-dir=bin --exclude-dir=obj || echo "No NUnit references found"`

**Step 2: Update any found references**

If NUnit references are found, replace with "xUnit"

**Step 3: Check GitHub Actions workflow**

Check: `.github/workflows/ci.yml` for test runner configuration
Update if necessary to use xUnit runner

**Step 4: Final documentation commit**

```bash
git add .github/workflows/ci.yml src/ShitpostBot/AGENTS.md
git commit -m "docs: update testing framework references from NUnit to xUnit"
```

## Migration Summary

**Files Modified:**
1. `src/ShitpostBot/test/ShitpostBot.Tests.Unit/ShitpostBot.Tests.Unit.csproj` - Package references
2. `src/ShitpostBot/test/ShitpostBot.Tests.Unit/LinkTests.cs` - Test attributes and using statements  
3. `src/ShitpostBot/Directory.Packages.props` - Central package management cleanup
4. `.github/workflows/ci.yml` - CI configuration (if needed)
5. `src/ShitpostBot/AGENTS.md` - Documentation (if needed)

**Key Changes:**
- `[TestFixture]` → No attribute needed
- `[TestCase]` → `[Theory]` + `[InlineData]`
- `using NUnit.Framework;` → `using Xunit;`
- Remove NUnit3TestAdapter, use xunit.runner.visualstudio

**Verification:**
- All existing tests should pass without logic changes
- Test runners use xUnit instead of NUnit
- CI/CD continues to work with xUnit runner
