# Post-Reevaluator Environment Variables Refactor Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Refactor post-reevaluator Helm chart to use direct `env` configuration like migrator instead of ConfigMap with `envFrom`.

**Architecture:** Remove the ConfigMap-based environment variable injection and replace with direct `env` array matching the migrator pattern. This uses `.Values.config` array for plain env vars and `.Values.secrets` array for secret references, both injected directly into the container spec.

**Tech Stack:** Helm charts, Kubernetes Jobs, YAML templates

---

## Task 1: Update Job Template to Use Direct Env Injection

**Files:**
- Modify: `charts/shitpostbot/charts/post-reevaluator/templates/job.yaml:42-45`

**Step 1: Replace envFrom with env block**

In `charts/shitpostbot/charts/post-reevaluator/templates/job.yaml`, replace lines 42-45:

```yaml
# OLD (lines 42-45):
          envFrom:
            - configMapRef:
                name: {{ include "post-reevaluator.configMapName" . }}

# NEW (replace with lines 42-59 from migrator pattern):
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
```

**Verification:** Check that the template syntax is correct
```bash
cat charts/shitpostbot/charts/post-reevaluator/templates/job.yaml | grep -A 20 "image:"
```
Expected: Shows `env:` block instead of `envFrom:` block

**Step 2: Commit the change**

```bash
git add charts/shitpostbot/charts/post-reevaluator/templates/job.yaml
git commit -m "refactor(helm): replace envFrom ConfigMap with direct env in post-reevaluator"
```

**Verification:** Commit created
```bash
git log -1 --oneline
```
Expected: Shows commit message

---

## Task 2: Delete ConfigMap Template

**Files:**
- Delete: `charts/shitpostbot/charts/post-reevaluator/templates/configmap.yaml`

**Step 1: Remove the configmap template file**

```bash
rm charts/shitpostbot/charts/post-reevaluator/templates/configmap.yaml
```

**Verification:** File deleted
```bash
ls charts/shitpostbot/charts/post-reevaluator/templates/configmap.yaml
```
Expected: `ls: cannot access ... No such file or directory`

**Step 2: Verify no other templates reference the configmap**

```bash
grep -r "configMapName" charts/shitpostbot/charts/post-reevaluator/templates/
```

**Verification:** Should only find reference in _helpers.tpl (helper function definition)
Expected: Only `_helpers.tpl` appears (the helper definition can stay for backwards compatibility)

**Step 3: Commit the deletion**

```bash
git add charts/shitpostbot/charts/post-reevaluator/templates/configmap.yaml
git commit -m "refactor(helm): remove unused ConfigMap template from post-reevaluator"
```

**Verification:** Commit created
```bash
git log -1 --oneline
```
Expected: Shows commit message

---

## Task 3: Update Values Schema

**Files:**
- Modify: `charts/shitpostbot/charts/post-reevaluator/values.yaml:1-68`
- Modify: `charts/shitpostbot/values.yaml:269-299`

**Step 1: Update subchart values.yaml**

In `charts/shitpostbot/charts/post-reevaluator/values.yaml`, replace the `config:` section:

```yaml
# OLD (around lines 15-18):
config:
  create: false
  name: ""
  data: {}

# NEW (replace with migrator pattern):
# Array of key:values to inject as environment variables
# Example:
# config:
#   - name: Logging__LogLevel__Default
#     value: Information
config: []

# Array of secrets to inject as environment variables
# Example:
# secrets:
#   - envName: DATABASE_PASSWORD
#     name: db-secret
#     key: password
#   - envName: API_KEY
#     name: api-secret
#     key: key
secrets: []
```

**Verification:** Values file updated
```bash
grep -A 5 "config:" charts/shitpostbot/charts/post-reevaluator/values.yaml
```
Expected: Shows `config: []` instead of nested object

**Step 2: Update parent chart values.yaml**

In `charts/shitpostbot/values.yaml`, replace the post-reevaluator config section (lines 281-284):

```yaml
# OLD (lines 281-284):
  config:
    create: true
    name: ""
    data: {}

# NEW (replace with array pattern):
  # Array of key:values to inject as environment variables
  # Example:
  # config:
  #   - name: Logging__LogLevel__Default
  #     value: Information
  config: []

  # Array of secrets to inject as environment variables
  # Example:
  # secrets:
  #   - envName: DATABASE_PASSWORD
  #     name: db-secret
  #     key: password
  #   - envName: API_KEY
  #     name: api-secret
  #     key: key
  secrets: []
```

**Verification:** Parent values updated
```bash
grep -A 5 "post-reevaluator:" charts/shitpostbot/values.yaml | grep -A 3 "config:"
```
Expected: Shows `config: []`

**Step 3: Commit the values changes**

```bash
git add charts/shitpostbot/charts/post-reevaluator/values.yaml charts/shitpostbot/values.yaml
git commit -m "refactor(helm): update post-reevaluator values to use env arrays"
```

**Verification:** Commit created
```bash
git log -1 --oneline
```
Expected: Shows commit message

---

## Task 4: Remove ConfigMap Helper from _helpers.tpl (Optional Cleanup)

**Files:**
- Modify: `charts/shitpostbot/charts/post-reevaluator/templates/_helpers.tpl:64-70`

**Context:** The `post-reevaluator.configMapName` helper is no longer used and can be removed for cleanliness.

**Step 1: Remove the configMapName helper**

In `charts/shitpostbot/charts/post-reevaluator/templates/_helpers.tpl`, delete lines 64-70:

```yaml
# DELETE these lines:
{{- define "post-reevaluator.configMapName" -}}
{{- if .Values.config.name -}}
{{ .Values.config.name }}
{{- else -}}
{{ include "post-reevaluator.fullname" . }}
{{- end -}}
{{- end -}}
```

**Verification:** Helper removed
```bash
grep "configMapName" charts/shitpostbot/charts/post-reevaluator/templates/_helpers.tpl
```
Expected: No output (helper is gone)

**Step 2: Commit the cleanup**

```bash
git add charts/shitpostbot/charts/post-reevaluator/templates/_helpers.tpl
git commit -m "refactor(helm): remove unused configMapName helper from post-reevaluator"
```

**Verification:** Commit created
```bash
git log -1 --oneline
```
Expected: Shows commit message

---

## Task 5: Remove ConfigMap Validation (Optional Cleanup)

**Files:**
- Modify: `charts/shitpostbot/charts/post-reevaluator/templates/_validate.tpl:1-3`

**Context:** The validation checks for `config.create` and `config.name` which no longer exist.

**Step 1: Remove the validation rule**

In `charts/shitpostbot/charts/post-reevaluator/templates/_validate.tpl`, delete all content:

```yaml
# DELETE entire file content (lines 1-3):
{{- if and (not .Values.config.create) (not .Values.config.name) }}
{{- fail "config.create=false requires config.name to be set" }}
{{- end }}
```

Leave the file empty or delete it entirely.

**Verification:** Validation removed
```bash
cat charts/shitpostbot/charts/post-reevaluator/templates/_validate.tpl
```
Expected: Empty file or file doesn't exist

**Step 2: Optionally delete the entire _validate.tpl file**

```bash
rm charts/shitpostbot/charts/post-reevaluator/templates/_validate.tpl
```

**Step 3: Commit the cleanup**

```bash
git add charts/shitpostbot/charts/post-reevaluator/templates/_validate.tpl
git commit -m "refactor(helm): remove unused config validation from post-reevaluator"
```

**Verification:** Commit created
```bash
git log -1 --oneline
```
Expected: Shows commit message

---

## Task 6: Test Template Rendering

**Files:**
- None (verification only)

**Step 1: Test with empty config (default)**

```bash
helm template test-release charts/shitpostbot/charts/post-reevaluator \
  --set global.imageTag=test \
  --set image.repository=test-repo \
  --set serviceAccount.create=false
```

**Verification:** Should render successfully with NO env block (since config and secrets are empty)
Expected: Job template renders, no `env:` section in container spec

**Step 2: Test with config values**

```bash
helm template test-release charts/shitpostbot/charts/post-reevaluator \
  --set global.imageTag=test \
  --set image.repository=test-repo \
  --set serviceAccount.create=false \
  --set config[0].name=LOG_LEVEL \
  --set config[0].value=Debug
```

**Verification:** Should render with env block containing LOG_LEVEL
Expected output should include:
```yaml
env:
- name: LOG_LEVEL
  value: "Debug"
```

**Step 3: Test with secrets**

```bash
helm template test-release charts/shitpostbot/charts/post-reevaluator \
  --set global.imageTag=test \
  --set image.repository=test-repo \
  --set serviceAccount.create=false \
  --set secrets[0].envName=DB_PASSWORD \
  --set secrets[0].name=db-secret \
  --set secrets[0].key=password
```

**Verification:** Should render with env block containing secret reference
Expected output should include:
```yaml
env:
- name: DB_PASSWORD
  valueFrom:
    secretKeyRef:
      name: db-secret
      key: password
```

**Step 4: Verify no ConfigMap is rendered**

```bash
helm template test-release charts/shitpostbot/charts/post-reevaluator \
  --set global.imageTag=test \
  --set config[0].name=TEST \
  --set config[0].value=value | grep -i configmap
```

**Verification:** Should return no results
Expected: No output (no ConfigMap in rendered templates)

---

## Task 7: Test Parent Chart Rendering

**Files:**
- None (verification only)

**Step 1: Render parent chart with default values**

```bash
helm template test-release charts/shitpostbot --set global.imageTag=test
```

**Verification:** Should render successfully
Expected: No errors, post-reevaluator Job included

**Step 2: Render with post-reevaluator config**

```bash
helm template test-release charts/shitpostbot \
  --set global.imageTag=test \
  --set post-reevaluator.config[0].name=ASPNETCORE_ENVIRONMENT \
  --set post-reevaluator.config[0].value=Production
```

**Verification:** Post-reevaluator Job should have env var
Expected: Job template includes `ASPNETCORE_ENVIRONMENT: "Production"`

**Step 3: Verify no ConfigMap for post-reevaluator**

```bash
helm template test-release charts/shitpostbot \
  --set global.imageTag=test | grep -B2 "kind: ConfigMap"
```

**Verification:** Should NOT show a post-reevaluator ConfigMap
Expected: Only shows ConfigMaps for other components (if any), not post-reevaluator

---

## Task 8: Validate with Helm Lint

**Files:**
- None (validation only)

**Step 1: Lint the post-reevaluator subchart**

```bash
helm lint charts/shitpostbot/charts/post-reevaluator \
  --set global.imageTag=test
```

**Verification:** Lint should pass
Expected: `1 chart(s) linted, 0 chart(s) failed`

**Step 2: Lint the parent chart**

```bash
helm lint charts/shitpostbot --set global.imageTag=test
```

**Verification:** Lint should pass
Expected: `1 chart(s) linted, 0 chart(s) failed`

**Step 3: Commit validation success**

```bash
git add -A
git commit -m "chore: validate post-reevaluator env refactor" --allow-empty
```

**Verification:** Commit created
```bash
git log -1 --oneline
```
Expected: Shows commit message

---

## Task 9: Update Chart Version and Documentation

**Files:**
- Modify: `charts/shitpostbot/charts/post-reevaluator/Chart.yaml:3,5`

**Step 1: Update chart description**

In `charts/shitpostbot/charts/post-reevaluator/Chart.yaml`, update description to mention the env pattern:

```yaml
# OLD (line 3):
description: Helm hook job for re-evaluating posts after model migrations (post-install/post-upgrade)

# NEW:
description: Helm hook job for re-evaluating posts after model migrations using direct env injection
```

**Verification:** Description updated
```bash
grep "description:" charts/shitpostbot/charts/post-reevaluator/Chart.yaml
```
Expected: Shows new description

**Step 2: Optionally bump chart version**

Note: Project uses 0.0.0 for all subcharts, so version bump may not be needed. Skip this step unless you want to track the change.

**Step 3: Commit documentation update**

```bash
git add charts/shitpostbot/charts/post-reevaluator/Chart.yaml
git commit -m "docs(helm): update post-reevaluator chart description"
```

**Verification:** Commit created
```bash
git log -1 --oneline
```
Expected: Shows commit message

---

## Task 10: Final Verification

**Files:**
- None (final checks)

**Step 1: Review all changes**

```bash
git log --oneline -10
```

**Verification:** Should show all commits from this refactor
Expected: Shows commits for template changes, deletions, values updates, validation, docs

**Step 2: Verify pattern matches migrator**

Compare the two job templates:

```bash
diff -u \
  <(grep -A 20 "env:" charts/shitpostbot/charts/migrator/templates/job.yaml) \
  <(grep -A 20 "env:" charts/shitpostbot/charts/post-reevaluator/templates/job.yaml)
```

**Verification:** Should show identical or very similar env injection patterns
Expected: Minimal or no differences in the env block structure

**Step 3: Final template render check**

```bash
helm template final-test charts/shitpostbot \
  --set global.imageTag=latest \
  --set post-reevaluator.config[0].name=TEST_VAR \
  --set post-reevaluator.config[0].value=test_value \
  --debug | grep -A 10 "post-reevaluator"
```

**Verification:** Clean output, env var appears correctly
Expected: Shows TEST_VAR in env block, no ConfigMap references

**Step 4: Confirm files removed**

```bash
ls charts/shitpostbot/charts/post-reevaluator/templates/
```

**Verification:** Should NOT include configmap.yaml or _validate.tpl
Expected: Only shows `_helpers.tpl`, `job.yaml`, `serviceaccount.yaml`

---

## Summary

This refactor converts post-reevaluator from ConfigMap-based environment variable injection to direct `env` array injection, matching the migrator pattern. The changes:

1. **Template simplified**: Replaced `envFrom` with direct `env` block supporting both plain values and secret references
2. **ConfigMap removed**: Deleted unused ConfigMap template and related helpers/validation
3. **Values schema updated**: Changed from nested config object to arrays matching migrator
4. **Consistency improved**: Post-reevaluator now uses same env pattern as migrator
5. **More flexible**: Supports both plain env vars (`.Values.config`) and secret refs (`.Values.secrets`)

Benefits:
- Consistent pattern across hook jobs (migrator and post-reevaluator)
- Simpler values schema (array instead of nested object)
- No unnecessary ConfigMap resource created
- More flexible (can mix plain values and secret references)
- Easier to understand and maintain
