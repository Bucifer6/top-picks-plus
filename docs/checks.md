---
summary: Plain-English checks the owner can try; each maps to a feature in spec.md and gets an automatic test where possible.
links: [spec.md]
---

# Checks

| ID | Feature | Check |
|----|---------|-------|
| C1 | F1 | In Dashboard → Scheduled Tasks, "Refresh Top Picks" shows a weekly trigger for Sunday 05:00, and "Run now" works. |
| C2 | F5 | With last week's folders still in top-picks, run the task. The row shows 1, 2, 3 … 10 left to right, and each picture's number matches its position. (This is today's bug.) |
| C3 | F5 | Run the task twice in a row without deleting anything. The order is still 1–10 both times. |
| C4 | F2 | Pin 3 titles in a chosen order. After a run they are 1, 2, 3 in that order, and 4–10 are the most-watched titles. |
| C5 | F2 | Pin 10 titles. The row is exactly those 10, in your order. |
| C6 | F2/F6 | Watch something past halfway, then run the task. Its play is counted (shown in the last run report). Stopping after 2 minutes does not count. |
| C7 | F2 | A series with plays on several episodes counts as one title, with all those plays added up. |
| C8 | F4 | A title that drops out of the 10 has its folder removed from top-picks and disappears from the row. |
| C9 | F3 | Playing a movie or an episode from the row plays the real file. Subtitles are available. |
| C10 | F3 | A series in the row opens with its seasons and episodes, as today. |
| C11 | F8 | Put a test file in top-picks that the plugin didn't make. After a run, it is still there. |
| C12 | F8 | With the old Emby.TopPicks plugin still installed, the task refuses to run and the report says why. |
| C13 | F8 | If the task is stopped part-way (e.g. Emby restarted mid-run), the row still shows last week's complete list. |
| C14 | F7 | The last run report lists the 10 titles, the reason for each, and "order check passed". |
| C15 | F2 | Fewer than 10 watched titles in the week: the gaps are filled with the newest additions, and the run doesn't fail. |
