# Security Policy

## Supported versions

CliWrap.FSharp is pre-1.0.
Security fixes go into the next release, and only the latest published version of `UnMango.CliWrap.FSharp` is supported.
Older versions do not get backported fixes.

| Version        | Supported |
| -------------- | --------- |
| Latest release | Yes       |
| Anything older | No        |

## Reporting a vulnerability

**Please do not open a public issue, pull request, or discussion for a suspected vulnerability.**

Report it privately through GitHub's private vulnerability reporting:

1. Go to the [Security tab](https://github.com/UnstoppableMango/CliWrap.FSharp/security) of this repository.
2. Click **Report a vulnerability**, or open <https://github.com/UnstoppableMango/CliWrap.FSharp/security/advisories/new> directly.
3. Describe the issue, the affected versions, and how to reproduce it.
   A minimal F# snippet or failing test is the most helpful thing you can include.

This is the only reporting channel.
Reports stay private between you and the maintainers until a fix is published.

## What to expect

- **Acknowledgement within 14 days** of your report, usually much sooner.
- An assessment of whether the report is accepted, and if so a rough timeline for a fix.
  We aim to release fixes for accepted vulnerabilities within 60 days of the report.
- Coordinated disclosure: once a fix is released we publish a GitHub Security Advisory (requesting a CVE where appropriate) and credit you, unless you ask not to be credited.
- The fix is called out in the release notes under **Security**.

## Scope

In scope: code in this repository that ships in the `UnMango.CliWrap.FSharp` package (`src/CliWrap.FSharp`), and this repository's build and release pipeline.

Out of scope:

- Vulnerabilities in [CliWrap](https://github.com/Tyrrrz/CliWrap) itself.
  Please report those upstream.
  If a CliWrap issue is exposed in a new way by the F# surface, report it here as well.
- The test dummy program and other test-only code, which are not shipped.
- Misuse that the API documents against, such as building a single argument string from untrusted input with `Cli.arg` instead of passing a list to `Cli.args`.
