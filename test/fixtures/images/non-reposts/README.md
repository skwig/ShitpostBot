# Non-Repost Test Fixtures

This directory contains unique images that should NOT be detected as reposts.

## Expected Behavior

When any two images from this directory are posted in sequence, they should:
1. Both be tracked in the database
2. Have feature vectors extracted by the ML service
3. NOT be detected as similar (cosine similarity < threshold)
4. NOT trigger repost reactions

## Adding New Fixtures

To add new unique images:
1. Add images with descriptive names
2. Ensure they are >= 299x299 pixels
3. Ensure they are visually distinct from other fixtures