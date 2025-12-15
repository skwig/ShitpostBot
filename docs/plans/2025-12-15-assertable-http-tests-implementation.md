# Assertable HTTP Tests Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Rewrite E2E HTTP tests with JavaScript assertions, variables for messageId passing, and split into per-scenario files with a top-level runner.

**Architecture:** Split monolithic e2e-repost-detection.http into 3 scenario-specific files (scenario-1-high-similarity-png-jpg.http, scenario-2-low-similarity-unrelated.http, scenario-3-high-similarity-webp.http) plus a top-level runner (e2e-repost-detection.http) that uses `< run` to execute each scenario. Add JavaScript assertions for status codes, response structure, and emoji verification.

**Tech Stack:** JetBrains HTTP Client format, JavaScript assertions, ijhttp CLI compatibility

---

## Task 1: Create Scenario 1 HTTP Test File (High Similarity PNG→JPG)

**Files:**
- Create: `test/scenario-1-high-similarity-png-jpg.http`

**Step 1: Create scenario file with assertions**

Create the file with:

```http
@host = http://localhost:8080

### ============================================
### SCENARIO 1: High Similarity - PNG to JPG Repost Detection
### ============================================
### This scenario tests that the same image in different formats (PNG → JPG)
### is correctly detected as a repost with 2 emoji reactions.

### 1a. Post original image (PNG)
POST {{host}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "https://media.discordapp.net/attachments/1319991503891861534/1449832200702001183/frenchcat.png?ex=694054f5&is=693f0375&hm=987e474fcc62613908de83ee2db12365472ec3bde4a27525185bbb80184e44e5&=&format=webp&quality=lossless&width=918&height=1155"
}

> {%
    client.test("POST returns 200", function() {
        client.assert(response.status === 200, "Expected status 200");
    });
    
    client.test("Response has messageId", function() {
        client.assert(response.body.messageId !== undefined, "messageId should be present");
        client.assert(typeof response.body.messageId === "number", "messageId should be a number");
    });
    
    client.test("Response has tracked=true", function() {
        client.assert(response.body.tracked === true, "tracked should be true");
    });
    
    client.global.set("originalMessageId", response.body.messageId);
%}

### 1b. Post repost (same image, JPG format)
POST {{host}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "https://media.discordapp.net/attachments/1319991503891861534/1449832201049870376/frenchcat.jpg?ex=694054f5&is=693f0375&hm=b8b5a37d8b72b4261dd61781d1ba828285b3ed9e5e00f1e044c69a274e3b05af&=&format=webp&width=918&height=1155"
}

> {%
    client.test("POST returns 200", function() {
        client.assert(response.status === 200, "Expected status 200");
    });
    
    client.test("Response has messageId", function() {
        client.assert(response.body.messageId !== undefined, "messageId should be present");
        client.assert(typeof response.body.messageId === "number", "messageId should be a number");
    });
    
    client.test("Response has tracked=true", function() {
        client.assert(response.body.tracked === true, "tracked should be true");
    });
    
    client.global.set("repostMessageId", response.body.messageId);
%}

### 1c. Query actions - expect 2 reactions in correct order
GET {{host}}/test/actions/{{repostMessageId}}?expectedCount=2&timeout=10000

> {%
    client.test("GET returns 200", function() {
        client.assert(response.status === 200, "Expected status 200");
    });
    
    client.test("Response has messageId matching request", function() {
        const requestedId = parseInt(client.global.get("repostMessageId"));
        client.assert(response.body.messageId === requestedId, "messageId should match requested ID");
    });
    
    client.test("Response has exactly 2 actions", function() {
        client.assert(response.body.actions.length === 2, "Should have exactly 2 actions");
    });
    
    client.test("Both actions are reactions", function() {
        const actions = response.body.actions;
        client.assert(actions[0].type === "reaction", "First action should be reaction");
        client.assert(actions[1].type === "reaction", "Second action should be reaction");
    });
    
    client.test("First reaction is police_car emoji", function() {
        const firstAction = response.body.actions[0];
        const data = JSON.parse(firstAction.data);
        client.assert(data.emoji === ":police_car:", "First emoji should be :police_car:");
    });
    
    client.test("Second reaction is rotating_light emoji", function() {
        const secondAction = response.body.actions[1];
        const data = JSON.parse(secondAction.data);
        client.assert(data.emoji === ":rotating_light:", "Second emoji should be :rotating_light:");
    });
    
    client.test("Response has waitedMs field", function() {
        client.assert(response.body.waitedMs !== undefined, "waitedMs should be present");
        client.assert(typeof response.body.waitedMs === "number", "waitedMs should be a number");
    });
%}
```

**Step 2: Commit**

```bash
git add test/scenario-1-high-similarity-png-jpg.http
git commit -m "feat: add scenario 1 HTTP test with assertions (PNG→JPG repost)"
```

---

## Task 2: Create Scenario 2 HTTP Test File (Low Similarity - Unrelated Images)

**Files:**
- Create: `test/scenario-2-low-similarity-unrelated.http`

**Step 1: Create scenario file with assertions**

Create the file with:

```http
@host = http://localhost:8080

### ============================================
### SCENARIO 2: Low Similarity - Unrelated Images (No Repost)
### ============================================
### This scenario tests that unrelated images are NOT detected as reposts
### and result in 0 emoji reactions.

### 2a. Post first image (French snails)
POST {{host}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "https://media.discordapp.net/attachments/1319991503891861534/1449824813462851675/RDT_20251203_1901397855856549389737722.jpg?ex=69404e13&is=693efc93&hm=9295add85964d52e714ff95792e0b6123bdbaa6149982afa93e3ec0d287c69aa&=&format=webp&width=854&height=1155"
}

> {%
    client.test("POST returns 200", function() {
        client.assert(response.status === 200, "Expected status 200");
    });
    
    client.test("Response has messageId", function() {
        client.assert(response.body.messageId !== undefined, "messageId should be present");
        client.assert(typeof response.body.messageId === "number", "messageId should be a number");
    });
    
    client.test("Response has tracked=true", function() {
        client.assert(response.body.tracked === true, "tracked should be true");
    });
    
    client.global.set("firstImageMessageId", response.body.messageId);
%}

### 2b. Post second unrelated image (Family Guy Bill Gates)
POST {{host}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "https://media.discordapp.net/attachments/1319991503891861534/1449825108611825915/RDT_20250125_1000203858268434480552263.jpg?ex=69404e5a&is=693efcda&hm=01d74376049da7f5b1bb16484c9b7dfa7990f7ef28b2155670f1941e69e827e3&=&format=webp&width=1329&height=1155"
}

> {%
    client.test("POST returns 200", function() {
        client.assert(response.status === 200, "Expected status 200");
    });
    
    client.test("Response has messageId", function() {
        client.assert(response.body.messageId !== undefined, "messageId should be present");
        client.assert(typeof response.body.messageId === "number", "messageId should be a number");
    });
    
    client.test("Response has tracked=true", function() {
        client.assert(response.body.tracked === true, "tracked should be true");
    });
    
    client.global.set("secondImageMessageId", response.body.messageId);
%}

### 2c. Query actions - expect 0 reactions (not a repost)
GET {{host}}/test/actions/{{secondImageMessageId}}?expectedCount=0&timeout=10000

> {%
    client.test("GET returns 200", function() {
        client.assert(response.status === 200, "Expected status 200");
    });
    
    client.test("Response has messageId matching request", function() {
        const requestedId = parseInt(client.global.get("secondImageMessageId"));
        client.assert(response.body.messageId === requestedId, "messageId should match requested ID");
    });
    
    client.test("Response has exactly 0 actions", function() {
        client.assert(response.body.actions.length === 0, "Should have exactly 0 actions (no repost)");
    });
    
    client.test("Actions array is empty", function() {
        client.assert(Array.isArray(response.body.actions), "actions should be an array");
        client.assert(response.body.actions.length === 0, "actions array should be empty");
    });
    
    client.test("Response has waitedMs field", function() {
        client.assert(response.body.waitedMs !== undefined, "waitedMs should be present");
        client.assert(typeof response.body.waitedMs === "number", "waitedMs should be a number");
    });
%}
```

**Step 2: Commit**

```bash
git add test/scenario-2-low-similarity-unrelated.http
git commit -m "feat: add scenario 2 HTTP test with assertions (unrelated images, no repost)"
```

---

## Task 3: Create Scenario 3 HTTP Test File (High Similarity WebP→WebP)

**Files:**
- Create: `test/scenario-3-high-similarity-webp.http`

**Step 1: Create scenario file with assertions**

Create the file with:

```http
@host = http://localhost:8080

### ============================================
### SCENARIO 3: High Similarity - WebP Formats
### ============================================
### This scenario tests that the same image in WebP format with different
### quality levels is correctly detected as a repost with 2 emoji reactions.

### 3a. Post WebP original
POST {{host}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "https://media.discordapp.net/attachments/1319991503891861534/1449832201821753567/frenchcat.webp?ex=694054f5&is=693f0375&hm=2d3c0b2ee955a4f22f4ac1f5733e295cd1284f7fd0ab96a3458c1ed601f59e44&=&format=webp&width=918&height=1155"
}

> {%
    client.test("POST returns 200", function() {
        client.assert(response.status === 200, "Expected status 200");
    });
    
    client.test("Response has messageId", function() {
        client.assert(response.body.messageId !== undefined, "messageId should be present");
        client.assert(typeof response.body.messageId === "number", "messageId should be a number");
    });
    
    client.test("Response has tracked=true", function() {
        client.assert(response.body.tracked === true, "tracked should be true");
    });
    
    client.global.set("webpOriginalMessageId", response.body.messageId);
%}

### 3b. Post WebP 50% quality
POST {{host}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "https://media.discordapp.net/attachments/1319991503891861534/1449832201385676992/frenchcat_50.webp?ex=694054f5&is=693f0375&hm=da1d9257e122b9b62b8990629a3ba1c5d02e8833443e090026365841f008021a&=&format=webp&width=900&height=1133"
}

> {%
    client.test("POST returns 200", function() {
        client.assert(response.status === 200, "Expected status 200");
    });
    
    client.test("Response has messageId", function() {
        client.assert(response.body.messageId !== undefined, "messageId should be present");
        client.assert(typeof response.body.messageId === "number", "messageId should be a number");
    });
    
    client.test("Response has tracked=true", function() {
        client.assert(response.body.tracked === true, "tracked should be true");
    });
    
    client.global.set("webpRepostMessageId", response.body.messageId);
%}

### 3c. Query actions - expect 2 reactions in correct order
GET {{host}}/test/actions/{{webpRepostMessageId}}?expectedCount=2&timeout=10000

> {%
    client.test("GET returns 200", function() {
        client.assert(response.status === 200, "Expected status 200");
    });
    
    client.test("Response has messageId matching request", function() {
        const requestedId = parseInt(client.global.get("webpRepostMessageId"));
        client.assert(response.body.messageId === requestedId, "messageId should match requested ID");
    });
    
    client.test("Response has exactly 2 actions", function() {
        client.assert(response.body.actions.length === 2, "Should have exactly 2 actions");
    });
    
    client.test("Both actions are reactions", function() {
        const actions = response.body.actions;
        client.assert(actions[0].type === "reaction", "First action should be reaction");
        client.assert(actions[1].type === "reaction", "Second action should be reaction");
    });
    
    client.test("First reaction is police_car emoji", function() {
        const firstAction = response.body.actions[0];
        const data = JSON.parse(firstAction.data);
        client.assert(data.emoji === ":police_car:", "First emoji should be :police_car:");
    });
    
    client.test("Second reaction is rotating_light emoji", function() {
        const secondAction = response.body.actions[1];
        const data = JSON.parse(secondAction.data);
        client.assert(data.emoji === ":rotating_light:", "Second emoji should be :rotating_light:");
    });
    
    client.test("Response has waitedMs field", function() {
        client.assert(response.body.waitedMs !== undefined, "waitedMs should be present");
        client.assert(typeof response.body.waitedMs === "number", "waitedMs should be a number");
    });
%}
```

**Step 2: Commit**

```bash
git add test/scenario-3-high-similarity-webp.http
git commit -m "feat: add scenario 3 HTTP test with assertions (WebP repost)"
```

---

## Task 4: Create Top-Level Runner HTTP Test File

**Files:**
- Modify: `test/e2e-repost-detection.http`

**Step 1: Replace file contents with runner**

Replace the entire file with:

```http
### ============================================
### E2E Repost Detection Test Suite
### ============================================
### This is the top-level test runner that executes all repost detection scenarios.
### Each scenario is in a separate file for better organization and independent execution.
###
### Usage:
### - Run all scenarios: Execute this file with ijhttp CLI or JetBrains HTTP Client
### - Run single scenario: Execute individual scenario file directly
###
### Scenarios:
### 1. High Similarity (PNG → JPG) - Same image in different formats
### 2. Low Similarity (Unrelated Images) - Different images should not trigger repost
### 3. High Similarity (WebP → WebP) - Same image in WebP with different quality
###
### Expected Results:
### - Scenarios 1 & 3: 2 reactions (:police_car: and :rotating_light:)
### - Scenario 2: 0 reactions (no repost detected)

### Run Scenario 1: High Similarity PNG→JPG Repost Detection
< scenario-1-high-similarity-png-jpg.http

### Run Scenario 2: Low Similarity Unrelated Images (No Repost)
< scenario-2-low-similarity-unrelated.http

### Run Scenario 3: High Similarity WebP Formats
< scenario-3-high-similarity-webp.http
```

**Step 2: Commit**

```bash
git add test/e2e-repost-detection.http
git commit -m "refactor: convert e2e-repost-detection.http to scenario runner"
```

---

## Task 5: Manual Verification

**Files:**
- N/A (manual testing)

**Step 1: Start all services**

Run: `docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml up --build`
Expected: All services start (database, ml-service, webapi, worker)

**Step 2: Test Scenario 1 individually**

Run (if using ijhttp CLI):
```bash
ijhttp test/scenario-1-high-similarity-png-jpg.http
```

Or execute in JetBrains HTTP Client / VS Code REST Client

Expected output:
- All assertions pass
- 2 reactions detected with correct emojis in order

**Step 3: Test Scenario 2 individually**

Run:
```bash
ijhttp test/scenario-2-low-similarity-unrelated.http
```

Expected output:
- All assertions pass
- 0 reactions (no repost detected)

**Step 4: Test Scenario 3 individually**

Run:
```bash
ijhttp test/scenario-3-high-similarity-webp.http
```

Expected output:
- All assertions pass
- 2 reactions detected with correct emojis in order

**Step 5: Test top-level runner**

Run:
```bash
ijhttp test/e2e-repost-detection.http
```

Expected output:
- All 3 scenarios execute sequentially
- All assertions pass across all scenarios

**Step 6: Stop services**

Run: `docker compose down`

**Step 7: Document results**

If all scenarios pass with assertions verified, implementation is complete.

---

## Final Checklist

- ✅ Scenario 1 HTTP test file created with assertions
- ✅ Scenario 2 HTTP test file created with assertions
- ✅ Scenario 3 HTTP test file created with assertions
- ✅ Top-level runner file created with `< run` includes
- ✅ JavaScript assertions verify status codes
- ✅ JavaScript assertions verify response structure
- ✅ JavaScript assertions verify emoji order (:police_car: first, :rotating_light: second)
- ✅ Variables used to pass messageIds between requests
- ✅ Manual verification performed

---

## Next Steps

After implementation:
1. The E2E test suite is now fully machine-assertable with automated pass/fail
2. Tests can be run individually by scenario or all together via runner
3. Future: Integrate into CI/CD if needed (currently local-only due to ML service)
4. Future: Add more scenarios as edge cases are discovered
