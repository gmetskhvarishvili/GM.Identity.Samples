# Contributing

Thanks for taking a look! This is a **sample** repository that demonstrates the **GM.\***
ecosystem — [GM.Identity](https://github.com/gmetskhvarishvili/GM.Identity),
[GM.API](https://github.com/gmetskhvarishvili/GM.API),
[GM.Mediator](https://github.com/gmetskhvarishvili/GM.Mediator),
[GM.EntityFramework](https://github.com/gmetskhvarishvili/GM.EntityFramework),
[GM.Messaging](https://github.com/gmetskhvarishvili/GM.Messaging),
[GM.HttpClient](https://github.com/gmetskhvarishvili/GM.HttpClient) and
[GM.Exceptions](https://github.com/gmetskhvarishvili/GM.Exceptions). It isn't a published
package, so there's no versioning or release process to worry about.

## Prerequisites

- **.NET 10 SDK**
- **PostgreSQL** to run the APIs (the tests need no database).

```bash
dotnet build -c Release
dotnet test  -c Release
```

## Workflow

1. Branch off `master`: `git switch -c fix/something`.
2. Make your change; keep the layering intact (Domain has no infrastructure dependencies,
   Persistence/Infrastructure implement outward concerns, the APIs are the composition roots).
3. Add or update tests under `tests/GM.Identity.Sample.Tests` where it makes sense.
4. Open a pull request into `master`. CI (`build` + tests) must pass.

## Secrets

Never commit real credentials. The OAuth / OpenIddict values in `appsettings.json` and
`Program.cs` are **placeholders** for illustration — use user secrets or environment variables
for anything real.

## Commit messages

[Conventional Commits](https://www.conventionalcommits.org/) are appreciated for readable
history (`feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`), though this repo doesn't
release packages, so they don't drive any automation.

## Code style

Enforced by [`.editorconfig`](.editorconfig). Run `dotnet format` before pushing if unsure.
