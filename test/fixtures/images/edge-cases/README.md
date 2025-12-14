# Edge Case Test Fixtures

This directory contains images that test boundary conditions and error handling.

## Categories

- **too-small.jpg**: Image smaller than 299x299 (should be rejected/not tracked)
- **corrupted.jpg**: Corrupted image file (should fail gracefully)

## Expected Behavior

These images test error handling:
- Too small images should be filtered by `Image.CreateOrDefault()` returning null
- Corrupted images should fail gracefully with appropriate error logging

## Adding New Edge Cases

Add images that test specific boundary conditions with clear naming.