# End-to-End Test Suite

## Overview

This directory contains the complete E2E test suite for ShitpostBot's image repost detection functionality. The tests validate the entire system end-to-end, from image posting through ML-based feature extraction to bot reactions. **All tests run completely offline using local test images.**

## Quick Start

**CRITICAL: Always run from repository root:**

```bash
./test/e2e/run-e2e-tests.sh
```

## Architecture

### Test Components

1. **`run-e2e-tests.sh`** - Orchestration script
   - Spins up local docker compose environment (with test data volume mounted)
   - Waits for services to be ready
   - Executes all test scenarios via `ijhttp`
   - Reports pass/fail results
   - Cleans up containers on exit

2. **`e2e-tests.http`** - Master test file
   - Defines test host and runs all scenarios
   - Uses JetBrains HTTP Client format

3. **`scenarios/*.http`** - Individual test scenarios
   - Self-contained test cases with JavaScript assertions
   - Machine-assertable: Scripts verify exact bot behavior

### Test Flow

```
User runs script
  ↓
Docker compose up (webapi + worker + ml-service + db)
  - ML service mounts test images from test/sample-data/
  ↓
Health checks (wait for /health endpoints)
  ↓
ijhttp executes test scenarios
  ↓
Each scenario:
  - POST image message → Get messageId
  - Query /test/actions/{messageId} → Get bot actions
  - JavaScript assertions verify behavior
  ↓
Report results (pass/fail)
  ↓
Docker compose down (cleanup)
```

## Test Scenarios

### Scenario 1: Posting Unrelated Images
**File:** `scenarios/scenario-1-posting-unrelated-images.http`

**Purpose:** Verify that posting completely different images does NOT trigger false positive repost detection.

**Test Steps:**
1. POST first image (family_guy_bill_gates.webp)
2. Wait for processing (no reactions expected)
3. POST second unrelated image (french_snails.webp)
4. Wait for processing (no reactions expected)

**Expected Behavior:**
- Both messages return 200 OK with messageId
- No bot actions/reactions on either message
- System correctly identifies images as different

### Scenario 2: Reposting Downscaled Images
**File:** `scenarios/scenario-2-reposting-downscaled.http`

**Purpose:** Verify that reposting a downscaled version of an image triggers repost detection.

**Test Steps:**
1. POST original image (obsidianslop.webp)
2. Wait for processing (no reactions expected)
3. POST downscaled version (obsidianslop_50.webp - 50% smaller)
4. Wait for bot reactions

**Expected Behavior:**
- Both messages return 200 OK with messageId
- First message: no reactions
- Second message: exactly 2 reactions in order:
  1. `:police_car:` (repost detected)
  2. `:rotating_light:` (alert/warning)

### Scenario 3: Reposting in Different Formats
**File:** `scenarios/scenario-3-reposting-in-different-formats.http`

**Purpose:** Verify that reposting the same image in different formats (PNG, JPG, WebP) triggers repost detection.

**Test Steps:**
1. POST PNG version (frenchcat.png)
2. Wait for processing (no reactions expected)
3. POST JPG version (frenchcat.jpg)
4. Wait for bot reactions
5. POST WebP version (frenchcat.webp)
6. Wait for bot reactions

**Expected Behavior:**
- All messages return 200 OK with messageId
- First message (PNG): no reactions
- Second message (JPG): exactly 2 reactions in order (`:police_car:`, `:rotating_light:`)
- Third message (WebP): exactly 2 reactions in order (`:police_car:`, `:rotating_light:`)

### Scenario 4: Bot Command Execution
**File:** `scenarios/scenario-4-bot-command-about.http`

**Purpose:** Verify that bot commands execute correctly and return expected content.

**Test Steps:**
1. Execute "about" command via /test/bot-command
2. Wait for bot response message

**Expected Behavior:**
- Command returns 200 OK with messageId
- Bot sends message containing system information
- Message includes expected content fields (version, uptime, etc.)

### Scenario 5: Semantic Search
**File:** `scenarios/scenario-5-semantic-search.http`

**Purpose:** Verify that semantic search using natural language queries finds relevant images and handles edge cases correctly.

**Test Steps:**
1. POST cat image (frenchcat.jpg)
2. POST obsidian image (obsidianslop.webp)
3. Wait for ML processing to complete
4. Execute "search cat" command
5. Verify search results contain matches with similarity scores
6. Execute "search   " (empty query)
7. Verify error message for empty query
8. Execute "search completely unrelated xyz123 gibberish"
9. Verify low confidence results (optional indicator)

**Expected Behavior:**
- All POST requests return 200 OK with messageId
- Search command with "cat" returns:
  - Progress message: `Searching for: "cat"`
  - Results message with similarity scores (e.g., `Match of 0.87654321`)
  - "Higher is a closer match" ordering hint
- Empty query returns error: "search query cannot be empty"
- Unrelated query returns results (may show "low confidence" indicator if scores < 0.8)

## Machine-Assertable Testing

### Test Action Logger System

The test infrastructure uses a custom action logging system that allows tests to query what actions the bot took in response to messages.

**Query Endpoint:**
```
GET /test/actions/{messageId}?expectedCount=N&timeout=MS
```

**Parameters:**
- `messageId` - The Discord message ID to query actions for
- `expectedCount` - Number of actions to wait for (optional)
- `timeout` - Max wait time in milliseconds (optional, default: 30000)

**Response Format:**
```json
{
  "messageId": "123456",
  "actions": [
    {
      "type": "Reaction",
      "emoji": "🚓",
      "timestamp": "2024-12-15T10:30:00Z"
    },
    {
      "type": "Reaction", 
      "emoji": "🚨",
      "timestamp": "2024-12-15T10:30:01Z"
    }
  ]
}
```

### JavaScript Assertions

Each scenario includes JavaScript test blocks that verify:

1. **Status Codes** - All requests return 200 OK
2. **Response Structure** - messageId is present in responses
3. **Action Counts** - Correct number of bot actions
4. **Action Types** - Actions are of expected type (Reaction, Message, etc.)
5. **Action Order** - Actions occur in the correct sequence
6. **Action Content** - Emojis match expected values

**Example Assertion:**
```javascript
client.test("Second message triggers repost detection", function() {
    client.assert(response.status === 200, "Response status is 200");
    
    const data = response.body;
    client.assert(data.actions.length === 2, "Expected 2 actions");
    client.assert(data.actions[0].emoji === "🚓", "First reaction is police car");
    client.assert(data.actions[1].emoji === "🚨", "Second reaction is rotating light");
});
```

## Requirements

### Tools
- **ijhttp** - JetBrains HTTP Client CLI
  - Available automatically via Nix flake (no manual install needed)
  - Alternative: Install via `nix profile install nixpkgs#ijhttp`

### Services
The script automatically handles service orchestration, but for manual testing you need:
- **WebApi** - REST API on port 8080
- **Worker** - Discord bot worker
- **ML Service** - TensorFlow feature extraction service
- **PostgreSQL** - Database with pgvector extension

### Sample Data
Test images are located in `test/sample-data/` and mounted into the ML service container at `/test-data/`:
- `family_guy_bill_gates.webp` - Unrelated image 1
- `french_snails.webp` - Unrelated image 2
- `obsidianslop.webp` - Original full-size image
- `obsidianslop_50.webp` - Downscaled to 50%
- `frenchcat.png` - PNG format
- `frenchcat.jpg` - JPG format (same image)
- `frenchcat.webp` - WebP format (same image)

Tests reference images using `file:///test-data/<filename>` URIs for fully offline operation.

## Usage

### Automated Run (Recommended)

From repository root:
```bash
./test/e2e/run-e2e-tests.sh
```

This handles everything: service startup, health checks, test execution, cleanup.

### Manual Run

1. Start services:
```bash
docker compose up --build
```

2. Wait for health checks to pass:
```bash
curl http://localhost:8080/health
```

3. Run tests:
```bash
ijhttp test/e2e/e2e-tests.http
```

4. Clean up:
```bash
docker compose down
```

## When to Run Tests

Run E2E tests after:
- **Major changes** to repost detection logic
- **ML service updates** (model, feature extraction)
- **Infrastructure changes** (database, messaging, API)
- **Test infrastructure changes** (action logger, endpoints)
- **Before merging** significant feature branches

## Offline Operation

The E2E test suite is designed to run completely offline without internet connectivity:

- **Test images** are stored locally in `test/sample-data/`
- **ML service** mounts test images via docker volume at `/test-data/`
- **Test scenarios** use `file://` URIs instead of HTTP URLs
- **No external dependencies** during test execution

This design ensures:
- ✅ Tests work without network access
- ✅ Tests run consistently regardless of external service availability
- ✅ Faster test execution (no download latency)
- ✅ Better security (no external data fetching)

## Troubleshooting

### Tests Fail with "Connection refused"
**Problem:** Services not ready yet
**Solution:** Script includes health checks, but you can verify manually:
```bash
curl http://localhost:8080/health
```

### Tests Fail with "Timeout waiting for actions"
**Problem:** Bot worker or ML service not processing messages
**Solution:** Check service logs:
```bash
docker compose logs worker
docker compose logs ml-service
```

### Tests Fail with Wrong Reactions
**Problem:** Repost detection thresholds or logic changed
**Solution:** Review changes to:
- `ShitpostBot.Worker` repost detection logic
- `ShitpostBot.MlService` similarity calculations
- Expected test behavior may need updating

### `ijhttp` Command Not Found
**Problem:** Tool not installed
**Solution:** 
- Nix users: Run `direnv allow` in repo root
- Manual install: `nix profile install nixpkgs#ijhttp`

## CI/CD Integration

**Current Status:** E2E tests are local-only, NOT run in CI/CD.

**Reason:** Heavy ML service with TensorFlow model (~500MB) makes CI/CD runs expensive and slow.

**Future Consideration:** Could be added to CI/CD with:
- Smaller test-specific ML model
- Dedicated ML service runner
- Longer timeout configurations
- Optional/manual trigger (not on every PR)

## Architecture Decisions

### Why Action Logging Instead of SSE?
- **Machine-assertable** - Tests can query exact actions programmatically
- **Simpler infrastructure** - No SSE endpoints, event publishers, or websocket complexity
- **Better for CI/CD** - Polling-based approach works in any environment
- **Explicit verification** - Tests explicitly state what they expect

### Why Local-Only Testing?
- **ML service size** - TensorFlow model is large (~500MB)
- **Resource requirements** - ML inference needs CPU/GPU resources
- **Test speed** - Local runs are faster than CI/CD with cold starts
- **Cost** - Avoid expensive CI/CD runner minutes for heavy workloads

### Why JetBrains HTTP Format?
- **JavaScript assertions** - Rich assertion capabilities
- **Variable passing** - Chain requests with client.global state
- **IDE integration** - Excellent support in JetBrains IDEs
- **CLI tool available** - `ijhttp` allows automated execution

## File Reference

```
test/e2e/
├── README.md (this file)
├── run-e2e-tests.sh              # Main orchestration script
├── e2e-tests.http                # Master test file
└── scenarios/
    ├── scenario-1-posting-unrelated-images-and-asking-bot.http
    ├── scenario-2-reposting-downscaled.http
    ├── scenario-3-reposting-in-different-formats.http
    ├── scenario-4-bot-command-about.http
    └── scenario-5-semantic-search.http
```

## Related Documentation

- **Root AGENTS.md** - Agent guidelines with E2E testing section
- **README.md** - Project overview with E2E testing section
- **src/ShitpostBot/AGENTS.md** - C# agent guidelines with E2E testing reference
