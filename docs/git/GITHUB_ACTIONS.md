# GitHub Actions

GitHub Actions is the automation system built into GitHub. In this repository it is used as a quality gate before Railway deploys the app.

## Current Workflow

The workflow lives at:

```text
.github/workflows/ci.yml
```

It runs when:

- code is pushed to `master`;
- a pull request targets `master`;
- someone starts it manually with `workflow_dispatch`.

## What It Does

1. Checks out the repository.
2. Installs the .NET 8 SDK.
3. Restores NuGet dependencies.
4. Builds `RescateAcademico.sln` in `Release` mode.

If the build fails, GitHub marks the commit or pull request as failing. Fix the code before merging or deploying.

## How To Read It In GitHub

1. Open the repository on GitHub.
2. Click the **Actions** tab.
3. Select **CI**.
4. Open the latest run.
5. Expand **Build ASP.NET Core app** to see each step.

Green means the app builds. Red means GitHub found a build problem.

## Relationship With Railway

GitHub Actions does not deploy the app. Railway deploys from `master`.

The intended flow is:

```text
commit -> GitHub Actions build -> Railway deploy from master
```

GitHub Actions catches build failures earlier, so Railway should receive cleaner commits.

## Future Improvements

- Add automated tests once the test project exists.
- Add formatting checks.
- Add security/dependency scanning.
- Require the CI workflow before pull requests can merge.
