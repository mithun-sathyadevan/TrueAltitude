# TrueAltitude Agent Context

This file provides high-signal context for coding agents working in this workspace.

## Workspace Layout

- `TrueAltitudBackEnd/` - ASP.NET Core backend (`net9.0`)
- `TrueAltitueWebClient/` - Angular frontend (`http://localhost:4200`)

## Product Context

TrueAltitude includes:

- Learning content driven by database entities (subjects, topics, questions)
- Real-time quiz flow with subject selection and timed exam
- User authentication with OTP email verification

## Backend Architecture

Backend follows layered structure:

- `TrueAltitude.Domain` - Entities and domain models
- `TrueAltitude.Application` - DTOs, services, business logic
- `TrueAltitude.Infrastructure` - Repository interfaces and implementations
- `TrueAltitude.Persistence` - EF Core and database context
- `TrueAltitude.API` - Controllers and app host

## Key Learning/Exam Behavior (Current)

Implemented behavior expected to remain intact:

1. Subjects/topics/questions come from database, not hardcoded data.
2. Question-to-topic supports many-to-many via junction table.
3. Real-time exam flow asks user to select subjects before starting.
4. Exam pulls 10 random questions (configurable server-side by count parameter).
5. Exam timer is 60 minutes.
6. During exam:
   - Do not show correct/incorrect while answering.
   - Do not expose correct answer in question fetch response.
7. At submit/end:
   - Evaluate all questions, including unanswered.
   - Show score and percentage.
   - Show correct answer text and explanation for each question.

## Learning API Endpoints (Current)

In `TrueAltitudBackEnd/TrueAltitude.API/Controllers/LearningController.cs`:

- `GET /api/learning/subjects`
- `GET /api/learning/subjects/{subjectCode}`
- `POST /api/learning/exam/questions`
- `POST /api/learning/exam/evaluate`

Contract expectations:

- Questions endpoint must not return correctness metadata.
- Evaluate endpoint is allowed to return correctness and explanations.

## Frontend Routing Notes

- Dashboard route and related components were removed intentionally.
- Learning shell navigation should go to home (`/`) instead of dashboard.

## Email Delivery Notes (Important)

OTP mail uses Titan SMTP settings currently configured with:

- Host: `smtpout.secureserver.net`
- Port: `465`
- SSL: implicit SSL required

Critical implementation note:

- `System.Net.Mail.SmtpClient` is not reliable for implicit SSL (`465`) in this setup.
- Mail sending has been migrated to MailKit (`MailKit.Net.Smtp` + `SecureSocketOptions.SslOnConnect`).

If mail breaks again, verify:

1. `MailKit` package exists in `TrueAltitude.Application.csproj`.
2. Backend was rebuilt after package changes.
3. Runtime uses newly built binaries (restart API process).

## Run Commands

Backend:

```bash
cd /c/AthulB/TrueAltitude/TrueAltitudBackEnd
dotnet build TrueAltitude.sln
dotnet run --project TrueAltitude.API/TrueAltitude.API.csproj
```

Frontend:

```bash
cd /c/AthulB/TrueAltitude/TrueAltitueWebClient
npm start
```

## Known Operational Pitfalls

- Do not assume dashboard components exist; they were deleted.
- If backend throws `FileNotFoundException` for `MailKit`, rebuild solution and restart API.
- Some past build failures were caused by transient `obj` cache issues; retrying targeted project builds can resolve.

## Suggested Agent Workflow For New Tasks

1. Validate current routes and endpoint contracts before changing UI behavior.
2. For exam changes, keep confidentiality boundary:
   - question fetch endpoint stays answer-safe
   - evaluation endpoint handles answer revelation
3. Build backend after service/package edits.
4. Smoke-test key flows:
   - user registration + OTP email
   - subject selection + exam start
   - submit + final review display

## Pending/Requested Item To Remember

User requested SQL insert query templates for learning tables (without seeding via code). If not already provided in future sessions, prepare insert templates for:

- subjects
- topics
- questions
- topic-question junction
- question options
