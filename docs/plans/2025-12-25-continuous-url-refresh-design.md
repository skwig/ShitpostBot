# Continuous Image URL Refresh Design

**Date**: 2025-12-25  
**Status**: Design Complete

## Problem

Discord CDN URLs in search results expire after ~10 days, breaking image thumbnails displayed in the search command results.

## Solution Overview

Create a new scheduled CronJob (`image-url-refresher`) that runs every 6 hours and incrementally refreshes Discord URLs for posts with image features. The job will:
- Track when each post's URL was last refreshed via a new `ImageUriFetchedAt` timestamp
- Select all posts where `ImageUriFetchedAt` is older than 6 hours (or NULL)
- Refresh their Discord URLs by fetching fresh attachment URLs from Discord API
- Handle failures by NOT updating the timestamp, allowing automatic retry on next run

Over a 7-day period (28 runs × every 6 hours), all posts will be refreshed, ensuring URLs stay fresh well before Discord's ~10 day expiration.

**Scope**: Only refreshes posts with `IsPostAvailable = true AND ImageFeatures != null` - these are evaluated posts that can appear in search results.

## Design Details

### 1. Domain Changes

**New Property on Image Value Object**:
```csharp
public DateTimeOffset? ImageUriFetchedAt { get; init; }
```

**Update Image Constructor**:
```csharp
internal Image(ulong imageId, Uri imageUri, string? mediaType, DateTimeOffset? imageUriFetchedAt, ImageFeatures? imageFeatures)
{
    ImageId = imageId;
    ImageUri = imageUri;
    MediaType = mediaType;
    ImageUriFetchedAt = imageUriFetchedAt;
    ImageFeatures = imageFeatures;
}
```

**Update Image.CreateOrDefault**:
```csharp
public static Image? CreateOrDefault(ulong imageId, Uri imageUri, string? mediaType, DateTimeOffset fetchedAt)
{
    return new Image(imageId, imageUri, mediaType, fetchedAt, null);
}
```

**Update ImagePost.RefreshImageUrl Method**:
```csharp
public void RefreshImageUrl(Uri newImageUri, string? mediaType, DateTimeOffset fetchedAt)
{
    Image = new Image(Image.ImageId, newImageUri, mediaType, fetchedAt, Image.ImageFeatures);
    IsPostAvailable = true;
}
```

**Database Migration**:
- Add column `Image_ImageUriFetchedAt` (DateTimeOffset, NULLABLE) as part of owned Image entity
- Existing posts default to `NULL` (never refreshed, will be prioritized)
- New posts set via `Image.CreateOrDefault` with fetchedAt parameter

### 2. New Project: ShitpostBot.ImageUrlRefresher

**Project Structure**:
```
ShitpostBot.ImageUrlRefresher/
├── ShitpostBot.ImageUrlRefresher.csproj
├── Program.cs
├── Dockerfile
├── appsettings.json
├── appsettings.Development.json
├── appsettings.Production.json
└── Properties/
    └── launchSettings.json
```

**Program.cs**:
```csharp
using Microsoft.EntityFrameworkCore;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

const int RunIntervalHours = 6;
const int FullRefreshCycleDays = 7;
const int ThrottleDelayMs = 500;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((hostContext, services) =>
{
    services.AddShitpostBotInfrastructure(hostContext.Configuration);
    services.AddDiscordClient(hostContext.Configuration);
});

var host = builder.Build();

using var scope = host.Services.CreateScope();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();
var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

logger.LogInformation("ImageUrlRefresher starting at: {Time}", DateTimeOffset.UtcNow);
logger.LogInformation(
    "Configuration: RunInterval={RunIntervalHours}h, FullRefreshCycle={FullRefreshCycleDays}d, Throttle={ThrottleDelayMs}ms",
    RunIntervalHours, FullRefreshCycleDays, ThrottleDelayMs);

await RefreshImageUrls(logger, dbContext, chatClient, unitOfWork, RunIntervalHours, ThrottleDelayMs);

logger.LogInformation("ImageUrlRefresher completed at: {Time}", DateTimeOffset.UtcNow);
return;

static async Task RefreshImageUrls(
    ILogger<Program> logger,
    IDbContext dbContext,
    IChatClient chatClient,
    IUnitOfWork unitOfWork,
    int runIntervalHours,
    int throttleDelayMs)
{
    var cutoffTime = DateTimeOffset.UtcNow.AddHours(-runIntervalHours);
    
    var postsToRefresh = await dbContext.ImagePost
        .Where(p => p.IsPostAvailable 
                    && p.Image.ImageFeatures != null
                    && (p.Image.ImageUriFetchedAt == null || p.Image.ImageUriFetchedAt < cutoffTime))
        .OrderBy(p => p.Image.ImageUriFetchedAt ?? DateTimeOffset.MinValue)
        .ToArrayAsync();
    
    logger.LogInformation(
        "Found {Count} posts to refresh (older than {CutoffTime})", 
        postsToRefresh.Length, 
        cutoffTime);
    
    foreach (var imagePost in postsToRefresh)
    {
        var utcNow = DateTimeOffset.UtcNow;
        await RefreshSinglePost(logger, chatClient, imagePost, unitOfWork, utcNow);
        await Task.Delay(throttleDelayMs);
    }
    
    logger.LogInformation("Refresh completed");
}

static async Task RefreshSinglePost(
    ILogger<Program> logger,
    IChatClient chatClient,
    ImagePost imagePost,
    IUnitOfWork unitOfWork,
    DateTimeOffset utcNow)
{
    try
    {
        var messageIdentification = new MessageIdentification(
            imagePost.ChatGuildId,
            imagePost.ChatChannelId,
            imagePost.PosterId,
            imagePost.ChatMessageId);
        
        var fetchedMessage = await chatClient.GetMessageWithAttachmentsAsync(messageIdentification);
        
        if (fetchedMessage == null)
        {
            logger.LogWarning(
                "Message or channel unavailable for ImagePost {ImagePostId}",
                imagePost.Id);
            imagePost.MarkPostAsUnavailable();
            await unitOfWork.SaveChangesAsync();
            return;
        }
        
        var matchingAttachment = fetchedMessage.Attachments
            .FirstOrDefault(a => a.Id == imagePost.Image.ImageId);
        
        if (matchingAttachment == null)
        {
            logger.LogWarning(
                "Attachment unavailable for ImagePost {ImagePostId}",
                imagePost.Id);
            imagePost.MarkPostAsUnavailable();
            await unitOfWork.SaveChangesAsync();
            return;
        }
        
        // Always refresh - updates URL and timestamp
        imagePost.RefreshImageUrl(matchingAttachment.Url, matchingAttachment.MediaType, utcNow);
        await unitOfWork.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        // Log error but don't update timestamp - will retry next run
        logger.LogError(ex,
            "Error refreshing URL for ImagePost {ImagePostId}. Will retry next run.",
            imagePost.Id);
    }
}
```

### 3. Kubernetes & Helm Configuration

**New Helm Subchart**: `charts/shitpostbot/charts/image-url-refresher/`

**Chart.yaml**:
```yaml
apiVersion: v2
name: image-url-refresher
description: Scheduled job to refresh Discord CDN URLs for image posts
type: application
version: 0.1.0
appVersion: "1.0"
```

**templates/cronjob.yaml**:
```yaml
apiVersion: batch/v1
kind: CronJob
metadata:
  name: {{ include "image-url-refresher.fullname" . }}
  labels:
    {{- include "image-url-refresher.labels" . | nindent 4 }}
spec:
  schedule: "0 */6 * * *"  # Every 6 hours
  concurrencyPolicy: Forbid  # Don't run concurrent jobs
  successfulJobsHistoryLimit: 3
  failedJobsHistoryLimit: 3
  jobTemplate:
    spec:
      backoffLimit: {{ default 1 .Values.backoffLimit }}
      template:
        metadata:
          {{- with .Values.podAnnotations }}
          annotations:
            {{- toYaml . | nindent 12 }}
          {{- end }}
          labels:
            {{- include "image-url-refresher.labels" . | nindent 12 }}
            {{- with .Values.podLabels }}
            {{- toYaml . | nindent 12 }}
            {{- end }}
        spec:
          {{- with .Values.imagePullSecrets }}
          imagePullSecrets:
            {{- toYaml . | nindent 12 }}
          {{- end }}
          serviceAccountName: {{ include "image-url-refresher.serviceAccountName" . }}
          {{- with .Values.podSecurityContext }}
          securityContext:
            {{- toYaml . | nindent 12 }}
          {{- end }}
          restartPolicy: Never
          containers:
            - name: {{ .Chart.Name }}
              {{- with .Values.securityContext }}
              securityContext:
                {{- toYaml . | nindent 16 }}
              {{- end }}
              image: "{{ .Values.image.repository }}:{{ .Values.image.tag | default .Values.global.imageTag | default .Chart.AppVersion }}"
              imagePullPolicy: {{ .Values.image.pullPolicy }}
              {{- if or .Values.secrets .Values.config }}
              env:
              {{- end }}
              {{- if .Values.secrets }}
              {{- range .Values.secrets }}
                - name: {{ .envName }}
                  valueFrom:
                    secretKeyRef:
                      name: {{ .name }}
                      key: {{ .key }}
              {{- end }}
              {{- end }}
              {{- if .Values.config }}
              {{- range .Values.config }}
                - name: {{ .name }}
                  value: {{ .value | quote }}
              {{- end }}
              {{- end }}
              {{- with .Values.resources }}
              resources:
                {{- toYaml . | nindent 16 }}
              {{- end }}
          {{- with .Values.nodeSelector }}
          nodeSelector:
            {{- toYaml . | nindent 12 }}
          {{- end }}
          {{- with .Values.affinity }}
          affinity:
            {{- toYaml . | nindent 12 }}
          {{- end }}
          {{- with .Values.tolerations }}
          tolerations:
            {{- toYaml . | nindent 12 }}
          {{- end }}
```

**Add to `charts/shitpostbot/values.yaml`**:
```yaml
image-url-refresher:
  backoffLimit: 1

  image:
    repository: ghcr.io/skwig/shitpostbot/image-url-refresher
    pullPolicy: IfNotPresent
    tag: ""

  imagePullSecrets: []
  nameOverride: ""
  fullnameOverride: ""

  config: []
  secrets: []

  serviceAccount:
    create: true
    automount: true
    annotations: {}
    name: ""

  podAnnotations: {}
  podLabels: {}
  podSecurityContext: {}
  securityContext: {}
  resources: {}
  nodeSelector: {}
  tolerations: []
  affinity: {}
```

### 4. GitHub Actions Build

**Update `.github/workflows/build.yml`**:

Add to `env` section:
```yaml
IMAGE_URL_REFRESHER_IMAGE_NAME: ${{ github.repository }}/image-url-refresher
```

Add new job:
```yaml
build-image-url-refresher:
  name: Build ShitpostBot.ImageUrlRefresher and push Docker image
  runs-on: ubuntu-latest
  permissions:
    contents: read
    packages: write
  steps:
    - uses: actions/checkout@v2

    - name: Log in to the Container registry
      uses: docker/login-action@f054a8b539a109f9f41c372932f1ae047eff08c9
      with:
        registry: ${{ env.REGISTRY }}
        username: ${{ github.actor }}
        password: ${{ secrets.GITHUB_TOKEN }}

    - name: Extract metadata (tags, labels) for Docker
      id: meta
      uses: docker/metadata-action@98669ae865ea3cffbcbaa878cf57c20bbf1c6c38
      with:
        images: ${{ env.REGISTRY }}/${{ env.IMAGE_URL_REFRESHER_IMAGE_NAME }}
        tags: |
          type=raw,value=${{ github.sha }}

    - name: Build and push Docker image
      uses: docker/build-push-action@ad44023a93711e3deb337508980b4b5e9bcdc5dc
      with:
        context: ./src/ShitpostBot
        file: ./src/ShitpostBot/src/ShitpostBot.ImageUrlRefresher/Dockerfile
        push: ${{ github.event_name != 'pull_request' }}
        tags: ${{ steps.meta.outputs.tags }}
        labels: ${{ steps.meta.outputs.labels }}
```

### 5. PostReevaluator Updates

Update `Program.cs` line ~180 to pass timestamp:
```csharp
imagePost.RefreshImageUrl(matchingAttachment.Url, matchingAttachment.MediaType, DateTimeOffset.UtcNow);
```

## Testing Strategy

**Unit Tests** (`ShitpostBot.Tests.Unit/ImagePostTests.cs`):
```csharp
[Test]
public void RefreshImageUrl_UpdatesImageUriFetchedAt()
{
    var imagePost = CreateTestImagePost();
    var newUri = new Uri("https://cdn.discordapp.com/attachments/123/456/new.jpg");
    var fetchedAt = DateTimeOffset.UtcNow;
    
    imagePost.RefreshImageUrl(newUri, "image/jpeg", fetchedAt);
    
    imagePost.Image.ImageUriFetchedAt.Should().Be(fetchedAt);
}

[Test]
public void Create_SetsInitialImageUriFetchedAt()
{
    var trackedOn = DateTimeOffset.UtcNow;
    var image = Image.CreateOrDefault(123, new Uri("https://example.com/image.jpg"), "image/jpeg", trackedOn);
    
    var imagePost = ImagePost.Create(
        DateTimeOffset.UtcNow,
        new ChatMessageIdentifier(1, 2, 3),
        new PosterIdentifier(4),
        trackedOn,
        image!);
    
    imagePost.Image.ImageUriFetchedAt.Should().Be(trackedOn);
}
```

**Manual Testing**:
- Run locally with docker-compose to verify job executes
- Check logs for proper refresh behavior
- Verify database `ImageUriFetchedAt` column updates correctly
- Test search command shows fresh URLs after refresh

## Edge Cases Handled

1. **NULL `ImageUriFetchedAt` (existing posts)**: Query treats NULL as `DateTimeOffset.MinValue`, prioritized first
2. **Concurrent job runs**: `concurrencyPolicy: Forbid` prevents overlapping executions
3. **Discord API rate limiting**: 500ms throttle between requests, job fails and retries next schedule
4. **Post/attachment deleted**: Marks `IsPostAvailable = false`, excluded from future queries
5. **Transient errors**: Caught and logged, timestamp NOT updated for automatic retry
6. **URL unchanged**: Still updates `ImageUriFetchedAt` to track verification
7. **No posts need refresh**: Query returns empty, job completes successfully
8. **New posts**: `ImageUriFetchedAt = trackedOn`, won't refresh until >6 hours old
9. **Posts without features**: Excluded from query, only refresh searchable posts

## Performance Characteristics

- **Runs every 6 hours**: 4 runs per day × 7 days = 28 runs per week
- **Time-based selection**: All posts >6 hours old get refreshed
- **Full cycle**: Every post refreshed within 7 days
- **Estimated runtime**: ~500 posts × 500ms throttle = ~4 minutes per run (for 14K posts distributed over 28 runs)
- **Discord API usage**: Lightweight, distributed load

## Files to Create

1. `src/ShitpostBot/src/ShitpostBot.ImageUrlRefresher/Program.cs`
2. `src/ShitpostBot/src/ShitpostBot.ImageUrlRefresher/ShitpostBot.ImageUrlRefresher.csproj`
3. `src/ShitpostBot/src/ShitpostBot.ImageUrlRefresher/Dockerfile`
4. `src/ShitpostBot/src/ShitpostBot.ImageUrlRefresher/appsettings.json`
5. `src/ShitpostBot/src/ShitpostBot.ImageUrlRefresher/appsettings.Development.json`
6. `src/ShitpostBot/src/ShitpostBot.ImageUrlRefresher/appsettings.Production.json`
7. `src/ShitpostBot/src/ShitpostBot.ImageUrlRefresher/Properties/launchSettings.json`
8. `charts/shitpostbot/charts/image-url-refresher/Chart.yaml`
9. `charts/shitpostbot/charts/image-url-refresher/templates/_helpers.tpl`
10. `charts/shitpostbot/charts/image-url-refresher/templates/cronjob.yaml`
11. `charts/shitpostbot/charts/image-url-refresher/templates/serviceaccount.yaml`
12. New migration file: `AddImageUriFetchedAt`

## Files to Modify

1. `src/ShitpostBot/src/ShitpostBot.Domain/Posts/ImagePost.cs` - Add property, update method, update Create()
2. `src/ShitpostBot/src/ShitpostBot.PostReevaluator/Program.cs` - Pass timestamp to RefreshImageUrl
3. `charts/shitpostbot/values.yaml` - Add image-url-refresher section
4. `.github/workflows/build.yml` - Add build job
5. `.github/workflows/deploy.yml` - Add deployment (if needed)
6. `docker-compose.yml` - Add service for local testing (optional)
7. `src/ShitpostBot/test/ShitpostBot.Tests.Unit/ImagePostTests.cs` - Add new tests
