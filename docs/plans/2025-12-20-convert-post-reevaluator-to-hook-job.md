# Convert Post-Reevaluator from CronJob to Helm Hook Job Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Convert post-reevaluator from a suspended CronJob to a post-install/post-upgrade Helm hook job that runs automatically after deployment, making model migrations hands-off.

**Architecture:** Replace the CronJob template with a Job template similar to the migrator chart. The job will use Helm hooks (`post-install`, `post-upgrade`) to trigger automatically after helm operations. The post-reevaluator application already handles graceful exits when there's nothing to reevaluate, so no application code changes are needed.

**Tech Stack:** Kubernetes Jobs, Helm hooks, YAML templates

---

## Task 1: Replace CronJob with Job Template

**Files:**
- Delete: `charts/shitpostbot/charts/post-reevaluator/templates/cronjob.yaml`
- Create: `charts/shitpostbot/charts/post-reevaluator/templates/job.yaml`

**Step 1: Delete the old CronJob template**

```bash
rm charts/shitpostbot/charts/post-reevaluator/templates/cronjob.yaml
```

**Verification:** File should no longer exist
```bash
ls charts/shitpostbot/charts/post-reevaluator/templates/cronjob.yaml
```
Expected: `ls: cannot access ... No such file or directory`

**Step 2: Create new Job template based on migrator**

Create `charts/shitpostbot/charts/post-reevaluator/templates/job.yaml`:

```yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: {{ include "post-reevaluator.fullname" . }}
  labels:
    {{- include "post-reevaluator.labels" . | nindent 4 }}
  annotations:
    "helm.sh/hook": post-upgrade,post-install
    "helm.sh/hook-weight": "0"
    "helm.sh/hook-delete-policy": before-hook-creation
spec:
  backoffLimit: {{ default 1 .Values.backoffLimit }}
  template:
    metadata:
      {{- with .Values.podAnnotations }}
      annotations:
        {{- toYaml . | nindent 8 }}
      {{- end }}
      labels:
        {{- include "post-reevaluator.labels" . | nindent 8 }}
        {{- with .Values.podLabels }}
        {{- toYaml . | nindent 8 }}
        {{- end }}
    spec:
      {{- with .Values.imagePullSecrets }}
      imagePullSecrets:
        {{- toYaml . | nindent 8 }}
      {{- end }}
      serviceAccountName: {{ include "post-reevaluator.serviceAccountName" . }}
      {{- with .Values.podSecurityContext }}
      securityContext:
        {{- toYaml . | nindent 8 }}
      {{- end }}
      restartPolicy: Never
      containers:
        - name: {{ .Chart.Name }}
          {{- with .Values.securityContext }}
          securityContext:
            {{- toYaml . | nindent 12 }}
          {{- end }}
          image: "{{ .Values.image.repository }}:{{ .Values.image.tag | default .Values.global.imageTag | default .Chart.AppVersion }}"
          imagePullPolicy: {{ .Values.image.pullPolicy }}
          envFrom:
            - configMapRef:
                name: {{ include "post-reevaluator.configMapName" . }}
          {{- with .Values.resources }}
          resources:
            {{- toYaml . | nindent 12 }}
          {{- end }}
      {{- with .Values.nodeSelector }}
      nodeSelector:
        {{- toYaml . | nindent 8 }}
      {{- end }}
      {{- with .Values.affinity }}
      affinity:
        {{- toYaml . | nindent 8 }}
      {{- end }}
      {{- with .Values.tolerations }}
      tolerations:
        {{- toYaml . | nindent 8 }}
      {{- end }}
```

**Verification:** Template should exist and be valid YAML
```bash
cat charts/shitpostbot/charts/post-reevaluator/templates/job.yaml
```
Expected: File content matches above

**Step 3: Commit the template change**

```bash
git add charts/shitpostbot/charts/post-reevaluator/templates/
git commit -m "refactor(helm): convert post-reevaluator from CronJob to hook Job"
```

**Verification:** Commit created successfully
```bash
git log -1 --oneline
```
Expected: Shows the commit message

---

## Task 2: Update ConfigMap Template

**Files:**
- Modify: `charts/shitpostbot/charts/post-reevaluator/templates/configmap.yaml:5`

**Context:** The configmap currently has a bug - it references `worker.fullname` instead of `post-reevaluator.fullname`.

**Step 1: Fix the fullname reference in configmap**

In `charts/shitpostbot/charts/post-reevaluator/templates/configmap.yaml`, change line 5:

```yaml
# OLD:
  name: {{ include "worker.fullname" . }}

# NEW:
  name: {{ include "post-reevaluator.fullname" . }}
```

**Verification:** Template references correct helper
```bash
grep "post-reevaluator.fullname" charts/shitpostbot/charts/post-reevaluator/templates/configmap.yaml
```
Expected: Line 5 contains `{{ include "post-reevaluator.fullname" . }}`

**Step 2: Commit the fix**

```bash
git add charts/shitpostbot/charts/post-reevaluator/templates/configmap.yaml
git commit -m "fix(helm): correct configmap name reference in post-reevaluator"
```

**Verification:** Commit created successfully
```bash
git log -1 --oneline
```
Expected: Shows the commit message

---

## Task 3: Test Helm Template Rendering

**Files:**
- None (verification only)

**Step 1: Render the post-reevaluator templates**

```bash
helm template test-release charts/shitpostbot/charts/post-reevaluator \
  --set global.imageTag=test \
  --set config.create=true \
  --set config.data.TEST_VAR=value
```

**Verification:** Template renders without errors and produces:
- A Job resource (not CronJob)
- Helm hook annotations: `post-upgrade,post-install`
- Hook weight: `0`
- Hook delete policy: `before-hook-creation`
- RestartPolicy: `Never`
- ConfigMap with correct name
- ServiceAccount resource

Expected output should include:
```yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: test-release-post-reevaluator
  annotations:
    "helm.sh/hook": post-upgrade,post-install
    "helm.sh/hook-weight": "0"
    "helm.sh/hook-delete-policy": before-hook-creation
spec:
  template:
    spec:
      restartPolicy: Never
```

**Step 2: Verify no references to CronJob remain**

```bash
grep -r "CronJob" charts/shitpostbot/charts/post-reevaluator/
```

**Verification:** Should return no results
Expected: No output (grep finds nothing)

**Step 3: Verify hook annotations are correct**

```bash
helm template test-release charts/shitpostbot/charts/post-reevaluator \
  --set global.imageTag=test \
  --set config.create=true | grep "helm.sh/hook"
```

**Verification:** Should show post-install and post-upgrade hooks
Expected:
```
    "helm.sh/hook": post-upgrade,post-install
    "helm.sh/hook-weight": "0"
    "helm.sh/hook-delete-policy": before-hook-creation
```

---

## Task 4: Test Full Chart Template Rendering

**Files:**
- None (verification only)

**Step 1: Render the parent shitpostbot chart**

```bash
helm template test-release charts/shitpostbot \
  --set global.imageTag=test \
  --set post-reevaluator.config.create=true \
  --set post-reevaluator.config.data.CONNECTION_STRING=test
```

**Verification:** Should render successfully with post-reevaluator subchart as a Job
Expected: No errors, output includes post-reevaluator Job with hooks

**Step 2: Verify hook execution order relative to migrator**

```bash
helm template test-release charts/shitpostbot \
  --set global.imageTag=test | grep -A2 "helm.sh/hook-weight"
```

**Verification:** Migrator should have weight "-1" (runs first), post-reevaluator should have weight "0" (runs after)
Expected output includes:
```
    "helm.sh/hook-weight": "-1"  # migrator
    "helm.sh/hook-weight": "0"   # post-reevaluator
```

---

## Task 5: Validate Helm Chart

**Files:**
- None (validation only)

**Step 1: Lint the post-reevaluator subchart**

```bash
helm lint charts/shitpostbot/charts/post-reevaluator \
  --set global.imageTag=test \
  --set config.create=true
```

**Verification:** Lint should pass with no errors
Expected: `1 chart(s) linted, 0 chart(s) failed`

**Step 2: Lint the parent shitpostbot chart**

```bash
helm lint charts/shitpostbot --set global.imageTag=test
```

**Verification:** Lint should pass with no errors
Expected: `1 chart(s) linted, 0 chart(s) failed`

**Step 3: Commit validation success**

```bash
git add -A
git commit -m "chore: validate post-reevaluator helm chart changes" --allow-empty
```

---

## Task 6: Document the Change

**Files:**
- Modify: `charts/shitpostbot/charts/post-reevaluator/Chart.yaml:3`

**Step 1: Update chart description**

In `charts/shitpostbot/charts/post-reevaluator/Chart.yaml`, update the description:

```yaml
# OLD:
description: Post re-evaluation job for migrating embeddings to new ML models

# NEW:
description: Helm hook job for re-evaluating posts after model migrations (post-install/post-upgrade)
```

**Verification:** Description updated
```bash
grep "description:" charts/shitpostbot/charts/post-reevaluator/Chart.yaml
```
Expected: Shows new description

**Step 2: Bump the chart version**

In `charts/shitpostbot/charts/post-reevaluator/Chart.yaml`, update version:

```yaml
# OLD:
version: 0.1.0

# NEW:
version: 0.2.0
```

**Verification:** Version bumped
```bash
grep "version:" charts/shitpostbot/charts/post-reevaluator/Chart.yaml
```
Expected: `version: 0.2.0`

**Step 3: Commit the documentation updates**

```bash
git add charts/shitpostbot/charts/post-reevaluator/Chart.yaml
git commit -m "docs(helm): update post-reevaluator chart description and version"
```

**Verification:** Commit created
```bash
git log -1 --oneline
```
Expected: Shows commit message

---

## Task 7: Final Verification

**Files:**
- None (final checks)

**Step 1: Review all changes**

```bash
git log --oneline -5
```

**Verification:** Should show all commits made during this implementation
Expected: Shows commits for:
1. Template conversion
2. ConfigMap fix
3. Documentation updates

**Step 2: Verify final template output**

```bash
helm template final-test charts/shitpostbot/charts/post-reevaluator \
  --set global.imageTag=latest \
  --set config.create=true \
  --set config.data.TEST=value \
  --debug
```

**Verification:** Clean template output with no warnings
Expected: Valid Kubernetes manifests for Job, ConfigMap, ServiceAccount

**Step 3: Confirm behavior change**

Review the key differences:
- **Before:** CronJob with `suspend: true`, schedule `0 0 31 2 *` (never runs)
- **After:** Job with `helm.sh/hook: post-upgrade,post-install` (runs automatically)
- **RestartPolicy:** Changed from `OnFailure` to `Never` (matches migrator pattern)
- **Hook weight:** `0` (runs after migrator at `-1`)

---

## Summary

This plan converts the post-reevaluator from a manually-triggered suspended CronJob to an automatically-executing Helm hook Job that runs after every install and upgrade. The job will:

1. **Run automatically** after `helm install` or `helm upgrade`
2. **Run after the migrator** (due to hook weight ordering)
3. **Exit gracefully** if there's nothing to reevaluate (application already handles this)
4. **Be deleted before recreation** (via `before-hook-creation` policy)
5. **Never restart on failure** (matches migrator pattern for hook jobs)

This makes model migrations completely hands-off - deploy a new model version, and both the migrator and post-reevaluator will run automatically in the correct order.
