# SideNotes

A tiny always-on-screen todo strip for Windows. It lives as a thin amber stripe
on the edge of your screen — click it and the panel slides in, jot your todos,
click anywhere else and it slides back out. Zero dependencies, one small `.exe`.

## Features

- **Zero screen space** — only a thin stripe stays visible at the screen edge.
- **Click the stripe** to slide the panel in; click away (or press Esc) and it
  slides back out on its own.
- **Quick add** — type and press Enter. Check a todo and it moves to the Done
  section (uncheck to bring it back). Hover a row to delete it.
- **Never loses anything** — every change is saved instantly to
  `%APPDATA%\SideNotes\notes.txt`. Shutdown, restart, crash — your list is
  exactly where you left it.
- **Tracks dates** — every todo remembers when it was added and when it was
  done. Hover a row to see them; they're included in copy/export too.
- **Copy / Export** — copy all, pending, or done to the clipboard as markdown,
  or export them to a `.md` file.
- **Drag to either side** — drag the stripe to the left or right edge; the
  side is remembered.
- **startup** toggle in the footer makes it launch with Windows.
- Single instance — launching it twice does nothing.

## Build

No SDK or downloads needed — it compiles with the C# compiler that ships with
Windows:

```
build.bat
```

Then run `SideNotes.exe`. That's it.

## Controls

| Action | How |
|---|---|
| Open / close panel | click the stripe (or Esc to close) |
| Add todo | type, press Enter |
| Mark done / undone | click the checkbox |
| Delete | hover the row, click ✕ |
| See added/done dates | hover the row |
| Copy / Export | footer menus |
| Move to other edge | drag the stripe |
| Launch with Windows | footer → startup |
| Quit | footer → quit |

## Data

Everything lives in `%APPDATA%\SideNotes\`:

- `notes.txt` — your todos (one per line, tab-separated, human-readable)
- `config.txt` — which edge the panel docks to
