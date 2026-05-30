# TrueAltitude Release Checklist

This checklist is intended for pre-release verification across backend and frontend.

## 1. Security and Auth

- [ ] JWT validation is strict in production (`Authentication:RelaxJwtValidationInDevelopment=false`).
- [ ] Request-time header enforcement is enabled in production (`SecurityHeader:EnforceRequestTimeHeader=true`).
- [ ] Request-time shared key is set via secure config/secret store in deployed environments.
- [ ] API and WebClient both use the same request-time shared key value.
- [ ] Admin endpoints reject missing/invalid admin role tokens.

### Test Cases

- [ ] Missing bearer token on admin API returns 401.
- [ ] Valid non-admin token on admin API returns 403.
- [ ] Expired token is rejected in production configuration.
- [ ] Invalid/missing request-time header is rejected when enforcement is on.

## 2. Premium Gating and Data Exposure

- [ ] Topic questions for premium topics are not returned to non-premium users.
- [ ] Real-time exam question fetch/evaluate endpoints block non-premium users with 403.
- [ ] Premium prompt appears in UI for blocked flows.
- [ ] Question fetch response does not expose correctness metadata.

### Test Cases

- [ ] Non-premium user attempts premium topic fetch and receives 403 or masked response per contract.
- [ ] Non-premium user attempts real-time exam start and receives 403.
- [ ] Premium user can complete real-time exam end-to-end.

## 3. Workbook/Bulk Import Safety

- [ ] Workbook import uses transaction scope and rolls back on any failure.
- [ ] Bulk topic upload uses transaction scope and rolls back on any failure.
- [ ] API returns actionable error reason in response payload on import failure.

### Test Cases

- [ ] Upload workbook with one invalid sheet and verify no partial inserts remain.
- [ ] Upload valid workbook and verify full insert set exists.
- [ ] Trigger malformed question row and verify rollback + clear API error message.

## 4. Money Contract (Paise End-to-End)

- [ ] DB values are stored as paise.
- [ ] API payload uses `priceInPaise` and `amountInPaise` consistently.
- [ ] Admin plan editor labels and validates amount as paise.
- [ ] Subscription page displays amount as paise without hidden conversion.

### Test Cases

- [ ] Configure plan `priceInPaise=49900`; verify create-order payload uses `amountInPaise=49900`.
- [ ] Complete payment flow and verify purchase/payment records preserve paise values.
- [ ] UI labels show paise explicitly on admin and subscription screens.

## 5. Learning UI and Navigation

- [ ] Learning resources (video/blog) are controlled by environment feature flags, not commented markup.
- [ ] Next-topic and next-section controls are visible at bottom of quiz screen.
- [ ] Next navigation scrolls to top of the page after selection.

### Test Cases

- [ ] Toggle `showLearningVideoCourse` and `showLearningChapterBlogs` in env files and verify conditional rendering.
- [ ] Complete section and verify `Next Section` behavior.
- [ ] On final section verify disabled `End Of Sections` state.

## 6. Build and Regression

- [ ] Backend solution builds cleanly.
- [ ] Frontend build passes with no template/type errors.
- [ ] Smoke test registration + OTP + login + premium purchase + learning exam flow.

### Suggested Commands

Backend:

```bash
cd /c/AthulB/TrueAltitude/TrueAltitudBackEnd
dotnet build TrueAltitude.sln
```

Frontend:

```bash
cd /c/AthulB/TrueAltitude/TrueAltitueWebClient
npm run build
```
