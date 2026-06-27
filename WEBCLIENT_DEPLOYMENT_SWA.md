# TrueAltitude WebClient Deployment Guide (SWA CLI)

This document captures the exact working deployment flow for the Angular web client to Azure Static Web Apps using a deployment token.

## Scope

- Project: `TrueAltitueWebClient`
- Hosting: Azure Static Web Apps
- Deployment source: `SwaCli`

## Prerequisites

1. Node.js and npm installed
2. Angular project dependencies installed
3. Valid Azure Static Web Apps deployment token
4. PowerShell available (recommended on Windows)

## Important Build Output Note

Angular build output is nested. The deployable folder is:

`dist/true-altitue-web-client/browser`

Do not deploy from `dist/true-altitue-web-client` root, because `index.html` is inside the `browser` folder.

## One-Time: Ensure SWA CLI Deploy Binary Exists

You can use either method:

1. Install SWA CLI globally:

```powershell
npm install -g @azure/static-web-apps-cli
```

2. Or run via npx on demand:

```powershell
npx --yes @azure/static-web-apps-cli --version
```

## Standard Deployment Steps

Run from repository root or anywhere (paths are explicit below).

### 1) Build the web client

```powershell
Set-Location 'C:\AthulB\TrueAltitude\TrueAltitueWebClient'
npm run build
```

### 2) Deploy using StaticSitesClient directly (most reliable on this machine)

```powershell
Set-Location 'C:\AthulB\TrueAltitude\TrueAltitueWebClient'

& 'C:\Users\Mithun.S\.swa\deploy\689a6c1fe8fc32f40348cc41223a7e9d83dd43d2\StaticSitesClient.exe' upload `
  --verbose `
  --workdir 'C:\AthulB\TrueAltitude\TrueAltitueWebClient' `
  --app 'dist/true-altitue-web-client/browser' `
  --configFileLocation 'dist/true-altitue-web-client/browser' `
  --apiToken '<PASTE_YOUR_STATIC_WEB_APPS_DEPLOYMENT_TOKEN>' `
  --deploymentProvider 'SwaCli' `
  --skipApiBuild true `
  --skipAppBuild true
```

### 3) Confirm success

Look for:

- `Status: Succeeded`
- `Deployment Complete :)`
- A URL like: `https://orange-bush-04c95f000.7.azurestaticapps.net`

## Fast Re-Deploy Checklist

1. Pull latest code (if needed)
2. `npm run build`
3. Deploy command above with token
4. Open site and hard refresh
5. Verify custom domain `https://www.truealtitude.in`

## Common Errors and Fixes

### Error: Failed to find a default file in app artifacts folder

Cause: Wrong deploy folder.

Fix: Deploy from `dist/true-altitue-web-client/browser` (contains `index.html`).

### Error: SWA CLI exits with generic code 1

Cause: Wrapper gives limited diagnostics.

Fix: Use `StaticSitesClient.exe upload` directly (command above) for detailed logs and reliable execution.

### Error: cmd.exe command appears to do nothing from Git Bash

Cause: Git Bash can mangle `cmd /c` argument parsing.

Fix: Use PowerShell command form exactly as shown in this guide.

### Error: 404 or 405 when posting to custom domain deploy endpoints

Cause: Custom domain is not the deployment API endpoint.

Fix: Use SWA deploy token with SWA tooling, not raw POST to website URL.

## Optional: Save as a Reusable PowerShell Script

Create a script later (example name: `deploy-webclient.ps1`) and keep token in an environment variable for safer usage.

Example token env var:

```powershell
$env:SWA_DEPLOY_TOKEN = '<TOKEN>'
```

Then replace `--apiToken` value with `$env:SWA_DEPLOY_TOKEN`.
