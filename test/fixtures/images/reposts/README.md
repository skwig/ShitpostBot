# Repost Test Fixtures

This directory contains pairs of images that should be detected as reposts by the ML service.

## Image Pairs

Each pair represents:
- **Original**: The first image posted
- **Repost**: A similar/duplicate image that should trigger repost detection

## Expected Behavior

When both images are posted in sequence via `/test/image-message`, the second image should:
1. Be tracked in the database
2. Have feature vectors extracted by the ML service
3. Be detected as similar to the first (cosine similarity >= threshold)
4. Trigger repost reactions (in Discord environment) or logs (in test environment)

## Adding New Fixtures

To add a new repost pair:
1. Add two images with clear naming (e.g., `cat-original.jpg`, `cat-repost.jpg`)
2. Ensure both are >= 299x299 pixels
3. Test locally to verify they're detected as similar