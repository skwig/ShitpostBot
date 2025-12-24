# Video Preview URL Implementation Plan
**Date:** 2025-12-24  
**Goal:** Process and store video preview thumbnail URLs instead of full video URLs to fix thumbnail display and reduce bandwidth

## Problem Statement

Currently, video attachments are stored with direct video URLs (`cdn.discordapp.com`). This causes:
1. Search results show no thumbnails for videos (Discord doesn't generate previews for raw video URLs)
2. ML service downloads and processes full video files for feature extraction
3. High bandwidth usage for video processing

## Solution Overview

Transform video URLs to preview thumbnail URLs using Discord's `media.discordapp.net` CDN with `format=webp` parameter. Store media type for all attachments to enable this transformation and support future media-type-based features.

### Key Changes
1. Add `MediaType` property to `Image` domain entity
2. Create extension methods on `DiscordAttachment` for URL transformation and media type checking
3. Update attachment handling in Worker, Infrastructure, and PostReevaluator
4. Generate database migration to add `MediaType` column

---

## Implementation Steps

### Step 1: Domain Model Updates

**File:** `src/ShitpostBot/src/ShitpostBot.Domain/Posts/Image.cs`

1. Add `MediaType` property to `Image` class:
   ```csharp
   public string? MediaType { get; init; }
   ```

2. Update constructor to accept `mediaType`:
   ```csharp
   internal Image(ulong imageId, Uri imageUri, string? mediaType, ImageFeatures? imageFeatures)
   {
       ImageId = imageId;
       ImageUri = imageUri;
       MediaType = mediaType;
       ImageFeatures = imageFeatures;
   }
   ```

3. Update `CreateOrDefault` factory method:
   ```csharp
   public static Image? CreateOrDefault(ulong imageId, Uri imageUri, string? mediaType)
   {
       return new Image(imageId, imageUri, mediaType, null);
   }
   ```

4. Update `WithImageFeatures` method:
   ```csharp
   public Image WithImageFeatures(ImageFeatures? imageFeatures)
   {
       return new Image(ImageId, ImageUri, MediaType, imageFeatures);
   }
   ```

**File:** `src/ShitpostBot/src/ShitpostBot.Domain/Posts/ImagePost.cs`

5. Update `RefreshImageUrl` method to accept and update MediaType:
   ```csharp
   public void RefreshImageUrl(Uri newImageUri, string? mediaType)
   {
       Image = new Image(Image.ImageId, newImageUri, mediaType, Image.ImageFeatures);
   }
   ```

---

### Step 2: Infrastructure Model Updates

**File:** `src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/Models/ImageMessage.cs`

1. Update `ImageMessageAttachment` record:
   ```csharp
   public record ImageMessageAttachment(ulong Id, string FileName, Uri Uri, string? MediaType);
   ```

**File:** `src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/Services/IChatClient.cs`

2. Update `MessageAttachment` record:
   ```csharp
   public record MessageAttachment(ulong Id, Uri Url, string? MediaType);
   ```

---

### Step 3: Extension Methods

**File:** `src/ShitpostBot/src/ShitpostBot.Application/Extensions/Extensions.cs`

Add extension methods for `DiscordAttachment`:

```csharp
using System.Web;
using DSharpPlus.Entities;

public static class DiscordAttachmentExtensions
{
    /// <summary>
    /// Gets the appropriate URI for the attachment.
    /// For videos, returns a preview thumbnail URL (media.discordapp.net with format=webp).
    /// For images, returns the original URL.
    /// </summary>
    public static Uri GetAttachmentUri(this DiscordAttachment attachment)
    {
        if (attachment.IsVideo())
        {
            // Transform: cdn.discordapp.com -> media.discordapp.net with format=webp
            var builder = new UriBuilder(attachment.Url);
            builder.Host = "media.discordapp.net";
            
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["format"] = "webp";
            builder.Query = query.ToString();
            
            return builder.Uri;
        }
        
        return new Uri(attachment.Url);
    }
    
    /// <summary>
    /// Determines if the attachment is an image or video suitable for processing.
    /// </summary>
    public static bool IsImageOrVideo(this DiscordAttachment attachment)
    {
        return attachment.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true
            || attachment.MediaType?.StartsWith("video/", StringComparison.OrdinalIgnoreCase) == true;
    }
    
    /// <summary>
    /// Determines if the attachment is a video.
    /// </summary>
    public static bool IsVideo(this DiscordAttachment attachment)
    {
        return attachment.MediaType?.StartsWith("video/", StringComparison.OrdinalIgnoreCase) == true;
    }
}
```

**Note:** Verify that DSharpPlus exposes `MediaType` or `ContentType` property. If it's named differently, adjust accordingly.

---

### Step 4: Worker Updates

**File:** `src/ShitpostBot/src/ShitpostBot.Worker/Internal/Core/ChatMessageCreatedListener.cs`

1. Update `TryHandleImageAsync` method:
   - Replace `IsImage(a) || IsVideo(a)` with `a.IsImageOrVideo()`
   - Update attachment creation to use extension methods and include MediaType

   ```csharp
   private async Task<bool> TryHandleImageAsync(MessageIdentification messageIdentification,
       MessageCreateEventArgs message,
       CancellationToken cancellationToken)
   {
       var imageAttachments = message.Message.Attachments
           .Where(a => a.IsImageOrVideo())  // NEW: Use extension method
           .Where(a => a.Height >= 299 && a.Width >= 299)
           .ToArray();
       
       if (!imageAttachments.Any())
       {
           return false;
       }

       foreach (var i in imageAttachments)
       {
           var attachment = new ImageMessageAttachment(
               i.Id, 
               i.FileName, 
               i.GetAttachmentUri(),  // NEW: Transform URL for videos
               i.MediaType             // NEW: Include MediaType
           );
           await mediator.Publish(
               new ImageMessageCreated(new ImageMessage(messageIdentification, attachment,
                   message.Message.CreationTimestamp)),
               cancellationToken
           );
       }

       return true;
   }
   ```

2. Remove old methods:
   - Delete `IsImage(DiscordAttachment)` method (lines 164-169)
   - Delete `IsVideo(DiscordAttachment)` method (lines 171-176)

---

### Step 5: Application Layer Updates

**File:** `src/ShitpostBot/src/ShitpostBot.Application/Features/PostTracking/TrackImageMessageHandler.cs`

Update `Handle` method to pass MediaType:

```csharp
var image = Image.CreateOrDefault(
    notification.ImageMessage.Attachment.Id, 
    notification.ImageMessage.Attachment.Uri,
    notification.ImageMessage.Attachment.MediaType  // NEW: Pass MediaType
);
```

---

### Step 6: Infrastructure Service Updates

**File:** `src/ShitpostBot/src/ShitpostBot.Infrastructure/Internal/Services/DiscordChatClient.cs`

Update `GetMessageWithAttachmentsAsync` method:

```csharp
var attachments = message.Attachments
    .Select(a => new MessageAttachment(
        a.Id, 
        a.GetAttachmentUri(),  // NEW: Use extension method
        a.MediaType             // NEW: Include MediaType
    ))
    .ToList();
```

**Note:** Add `using ShitpostBot.Application.Extensions;` at the top of the file.

---

### Step 7: PostReevaluator Updates

**File:** `src/ShitpostBot/src/ShitpostBot.PostReevaluator/Program.cs`

Update `RefreshDiscordUrl` method to handle MediaType:

```csharp
if (matchingAttachment.Url.ToString() != imagePost.Image.ImageUri.ToString() 
    || matchingAttachment.MediaType != imagePost.Image.MediaType)  // NEW: Also check MediaType
{
    logger.LogDebug(
        "Refreshing URL for ImagePost {ImagePostId}: {OldUrl} -> {NewUrl}, MediaType: {MediaType}",
        imagePost.Id, imagePost.Image.ImageUri, matchingAttachment.Url, matchingAttachment.MediaType);
    imagePost.RefreshImageUrl(matchingAttachment.Url, matchingAttachment.MediaType);  // NEW: Pass MediaType
}
```

---

### Step 8: Database Migration

**Execute from:** `src/ShitpostBot/src/ShitpostBot.Infrastructure/`

1. Generate migration:
   ```bash
   dotnet ef migrations add AddMediaTypeToImage --startup-project ../ShitpostBot.Infrastructure.Migrator
   ```

2. Review generated migration to ensure:
   - Adds nullable `Image_MediaType` column (type: `text`)
   - Existing records will have `NULL` value (acceptable - will populate on next refresh)

3. The migration should look similar to:
   ```csharp
   migrationBuilder.AddColumn<string>(
       name: "Image_MediaType",
       table: "Post",
       type: "text",
       nullable: true);
   ```

---

## Testing Strategy

### Manual Testing
1. **New video post:**
   - Post a video in Discord
   - Verify URL stored uses `media.discordapp.net` with `format=webp`
   - Verify MediaType is stored (e.g., `video/mp4`)
   - Run search command and verify thumbnail appears

2. **New image post:**
   - Post an image in Discord
   - Verify URL uses original `cdn.discordapp.com`
   - Verify MediaType is stored (e.g., `image/png`)
   - Run search command and verify thumbnail appears

3. **PostReevaluator:**
   - Run PostReevaluator
   - Verify existing posts get MediaType populated
   - Verify video URLs get transformed to preview URLs

### Integration Tests
Consider adding tests in `ShitpostBot.Tests.Integration`:
- Test that video attachments transform to `media.discordapp.net`
- Test that image attachments remain unchanged
- Test MediaType detection for various content types

---

## Rollout Considerations

### Database Migration
- **Backward compatible:** Adding nullable column won't break existing code
- **Existing data:** NULL MediaType for existing posts is acceptable
  - Will be populated naturally during next URL refresh
  - PostReevaluator will fill in MediaType when it runs

### Deployment Order
1. Deploy database migration
2. Deploy Worker, WebApi, and PostReevaluator together
3. Optionally run PostReevaluator to backfill MediaType for existing posts

### Rollback Plan
If issues arise:
1. Revert application code
2. MediaType column can remain (nullable, ignored by old code)
3. No data loss - old URLs still work

---

## Future Enhancements

With MediaType stored, future features become possible:
- Filter searches by media type (images only, videos only)
- Different processing pipelines for images vs videos
- Analytics on media type distribution
- Video-specific features (duration, resolution optimization)

---

## Files to Modify

### Domain Layer
- `src/ShitpostBot/src/ShitpostBot.Domain/Posts/Image.cs`
- `src/ShitpostBot/src/ShitpostBot.Domain/Posts/ImagePost.cs`

### Application Layer
- `src/ShitpostBot/src/ShitpostBot.Application/Extensions/Extensions.cs` (add extension methods)
- `src/ShitpostBot/src/ShitpostBot.Application/Features/PostTracking/TrackImageMessageHandler.cs`

### Infrastructure Layer
- `src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/Models/ImageMessage.cs`
- `src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/Services/IChatClient.cs`
- `src/ShitpostBot/src/ShitpostBot.Infrastructure/Internal/Services/DiscordChatClient.cs`

### Worker
- `src/ShitpostBot/src/ShitpostBot.Worker/Internal/Core/ChatMessageCreatedListener.cs`

### PostReevaluator
- `src/ShitpostBot/src/ShitpostBot.PostReevaluator/Program.cs`

### Database
- Generate migration in `src/ShitpostBot/src/ShitpostBot.Infrastructure/Migrations/`

**Total:** 9 files to modify + 1 migration to generate

---

## Verification Checklist

Before considering implementation complete:

- [ ] Extension methods created and working for `DiscordAttachment`
- [ ] `Image` domain entity includes `MediaType` property
- [ ] `ImageMessageAttachment` includes `MediaType` parameter
- [ ] `MessageAttachment` includes `MediaType` parameter
- [ ] Worker uses `IsImageOrVideo()` extension method
- [ ] Worker uses `GetAttachmentUri()` for URL transformation
- [ ] DiscordChatClient uses extension methods
- [ ] PostReevaluator updates MediaType during refresh
- [ ] Database migration generated and reviewed
- [ ] Search results show thumbnails for videos
- [ ] Video URLs use `media.discordapp.net` with `format=webp`
- [ ] Image URLs remain unchanged
- [ ] No compilation errors
- [ ] Integration tests pass (if added)

---

## Notes

1. **DSharpPlus Property Name:** Confirm `MediaType` or `ContentType` is the correct property name in DSharpPlus 4.5.1. Adjust if necessary.

2. **System.Web Dependency:** The `HttpUtility.ParseQueryString` requires `System.Web` namespace. Verify it's available or use alternative query string manipulation.

3. **Null Handling:** MediaType is nullable to handle:
   - Existing records (before migration)
   - Potential Discord API changes
   - Unknown attachment types

4. **URL Format:** The Discord preview URL format `https://media.discordapp.net/.../file.mp4?params&format=webp` has been tested and confirmed working.
