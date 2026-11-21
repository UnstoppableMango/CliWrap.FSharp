# Issue tracker: GitHub

Issues and specs for this repo live as GitHub issues. Use the `gh` CLI for all operations.

## Conventions

- **Create an issue**: `gh issue create --title "..." --body "..."`. Use a heredoc for multi-line bodies.
- **Read an issue**: `gh issue view <number> --json number,title,body,labels,comments --jq '{number, title, body, labels: [.labels[].name], comments: [.comments[].body]}'`. Plain `--comments` output is for humans and prints only comments when piped, so always use `--json`.
- **List issues**: `gh issue list --state open --limit 200 --json number,title,body,labels,comments --jq '[.[] | {number, title, body, labels: [.labels[].name], comments: [.comments[].body]}]'` with appropriate `--label` and `--state` filters. The default limit is 30; if the result count equals `--limit`, raise it and run again so nothing is silently dropped.
- **Comment on an issue**: `gh issue comment <number> --body "..."`
- **Apply / remove labels**: `gh issue edit <number> --add-label "..."` / `--remove-label "..."`
- **Close**: post the reason first with `gh issue comment <number> --body "..."`, then `gh issue close <number>`. (`gh issue close --comment` can succeed without posting when the issue is already closed.)

Infer the repo from `git remote -v`; `gh` does this automatically when run inside a clone.

## Pull requests as a triage surface

**PRs as a request surface: no.** _(Set to `yes` if this repo treats external PRs as feature requests; `/triage` reads this flag.)_

When set to `yes`, PRs run through the same labels and states as issues, using the `gh pr` equivalents:

- **Read a PR**: `gh pr view <number> --json number,title,body,labels,comments,reviews` and `gh pr diff <number>` for the diff.
- **List external PRs for triage**: `gh pr list` doesn't expose author association, so use the REST API: `gh api --paginate 'repos/{owner}/{repo}/pulls?state=open&per_page=100' --jq '.[] | select(.author_association | IN("OWNER", "MEMBER", "COLLABORATOR") | not) | {number, title, body, author: .user.login, author_association, labels: [.labels[].name]}'` (drops `OWNER`/`MEMBER`/`COLLABORATOR`). It prints one JSON object per line across all pages; `--jq` runs per page, so don't wrap the filter in `[...]`. Then read each kept PR with the command above.
- **Comment / label / close**: `gh pr comment`, `gh pr edit --add-label`/`--remove-label`, `gh pr close`.

GitHub shares one number space across issues and PRs, so a bare `#42` may be either: resolve with `gh pr view 42` and fall back to `gh issue view 42`.

## When a skill says "publish to the issue tracker"

Create a GitHub issue.

## When a skill says "fetch the relevant ticket"

Run `gh issue view <number> --json number,title,body,labels,comments --jq '{number, title, body, labels: [.labels[].name], comments: [.comments[].body]}'`.

## Wayfinding operations

Used by `/wayfinder`. The **map** is a single issue with **child** issues as tickets.

The `wayfinder:*` labels must exist before the first `gh issue create --label`. Create any that are missing. Existing labels are left alone, and any other `gh` error is printed:

```sh
if existing=$(gh label list --limit 1000 --json name --jq '.[].name'); then
  for l in map research prototype grilling task; do
    printf '%s\n' "$existing" | grep -qxF "wayfinder:$l" || gh label create "wayfinder:$l"
  done
fi
```

- **Map**: a single issue labelled `wayfinder:map`, holding the Notes / Decisions-so-far / Fog body. `gh issue create --label wayfinder:map`.
- **Child ticket**: an issue linked to the map as a GitHub sub-issue (`gh api` on the sub-issues endpoint). Where sub-issues aren't enabled, add the child to a task list in the map body and put `Part of #<map>` at the top of the child body. Labels: `wayfinder:<type>` (`research`/`prototype`/`grilling`/`task`). Once claimed, the ticket is assigned to the driving dev.
- **Blocking**: GitHub's **native issue dependencies**, the canonical, UI-visible representation. Add an edge with `gh api --method POST repos/<owner>/<repo>/issues/<child>/dependencies/blocked_by -F issue_id=<blocker-db-id>`, where `<blocker-db-id>` is the blocker's numeric **database id** (`gh api repos/<owner>/<repo>/issues/<n> --jq .id`, _not_ the `#number` or `node_id`). GitHub reports `issue_dependencies_summary.blocked_by` (open blockers only, the live gate). Where dependencies aren't available, fall back to a `Blocked by: #<n>, #<n>` line at the top of the child body. A ticket is unblocked when every blocker is closed.
- **Frontier query**: take the map's children in map order (`gh issue view <map> --json subIssues`, or the task list where sub-issues aren't enabled). For each open child, read its open-blocker count and assignees with `gh api repos/{owner}/{repo}/issues/<n> --jq '{state, blocked_by: .issue_dependencies_summary.blocked_by, assignees: [.assignees[].login]}'`. Drop it if `blocked_by > 0`, if an issue in its `Blocked by` line is still open, or if it has an assignee. The first one left wins.
- **Claim**: `gh issue edit <n> --add-assignee @me`, the session's first write.
- **Resolve**: `gh issue comment <n> --body "<answer>"`, then `gh issue close <n>`, then append a context pointer (gist + link) to the map's Decisions-so-far.
