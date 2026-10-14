# API fixtures

WakaTime API responses with the same shape as the real ones (including fields the apps ignore),
shared by the test suites of every platform. If both apps parse these files and get the same
numbers, they agree on what the API means.

| File | Endpoint |
|---|---|
| `status_bar_today.json` | `GET /users/current/status_bar/today` |
| `summaries_last_7_days.json` | `GET /users/current/summaries?range=last_7_days` |

"Today" is Friday 2026-10-09. Expected values:

- Today: 11,520 s (3h 12m). Top project today: `waka-bar`, 7,200 s.
- Summaries say 10,000 s for today: they lag behind `status_bar`, so the apps show 11,520.
- Last 7 days, with today from `status_bar`: 92,520 s.
- Languages: Swift 54,600 s, TypeScript 27,300 s, Markdown 9,100 s. `Other` (30 s) is dropped.
- Projects: `waka-bar` 45,500 s, `marketplace` 36,400 s, `dotfiles` 9,100 s.
- Streak: 7 days.

Tests that use these files: `macos/Tests/APIContractTests.swift`.

When the API changes shape, update these files first: every platform's tests should then fail
until its parser is fixed.
