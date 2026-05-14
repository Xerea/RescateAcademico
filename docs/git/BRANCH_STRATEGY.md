# Branch Strategy

This repository uses a deliberately simple branch model.

## Permanent Branch

- `master` is the only permanent branch.
- Railway deploys from `master`.
- GitHub Actions validates `master` on every push.

## Temporary Branches

Use short-lived branches for work in progress:

- `feature/<short-name>` for new functionality.
- `fix/<short-name>` for bug fixes.
- `docs/<short-name>` for documentation-only work.
- `codex/<short-name>` for AI-assisted implementation branches.

Merge temporary branches into `master` with a pull request, then delete the branch after merge.

## Daily Workflow

```bash
git switch master
git pull origin master
git switch -c fix/login-redirect

# make changes
dotnet build .\RescateAcademico.sln

git add .
git commit -m "Fix login redirect"
git push -u origin fix/login-redirect
```

Open a pull request into `master`. When CI is green and the change is reviewed, merge it and delete the branch.

## What Not To Do

- Do not keep both `main` and `master`.
- Do not push long-lived experiment branches.
- Do not commit IDE folders such as `.idea/` or `.vs/`.
- Do not commit local database files, build outputs, or uploads.
