# LoreBookTitles — Vintage Story mod (client-side)

## Goal
Show the real title of a lore book, with its chapter number, in the inventory, the hotbar and on bookshelves,
instead of the generic "Aged lore book" / "Rotten lore book" name.

## Environment
- Vintage Story 1.22.7, Windows 11, Visual Studio 2026 (C# workload), .NET 10 (`net10.0`).
- Game installed in `%APPDATA%\Vintagestory` (contains `assets`, `Mods`, `Lib`). The `VINTAGE_STORY` variable
  is NOT set on the machine: the `.csproj` falls back to `$(APPDATA)\Vintagestory` when it is empty.
- Project based on the official template (`vsmodtemplate`), without the CakeBuild project.
- Build: `dotnet build` or F5 in VS (`Client` profile in `Properties/launchSettings.json`,
  which starts the game with `--addModPath bin/<Config>/Mods`).

## Verified in the game code (anegostudios GitHub)
- `ItemBook : ItemRollable`, namespace `Vintagestory.GameContent` (`vssurvivalmod`, `Systems/WritingSystem/ItemBook.cs`).
- `ItemBook.GetHeldItemName(ItemStack)` only reads the stack's `title` attribute, otherwise the base name.
- `ItemStack.GetName()` calls `Collectible.GetHeldItemName(this)` (`vsapi`).
- `GuiDialogReadonlyBook` displays `Lang.Get(stack.Attributes.GetString("titleCode"))` for books with `textCodes`.
- `ModJournal.TryDiscoverLore` (`Systems/Journal.cs`) writes `discoveryCode`, `chapterIds`, `textCodes` and
  `titleCode` on the book **only on first discovery** (first read), not when it is generated or picked up.

## Verified in the local DLLs (IL read with Mono.Cecil, 1.22.7)
- Lore books are `item-lore-book-<color>` (`assets/survival/itemtypes/lore/lore.json`), class
  `ItemRandomLore : ItemBook`, which does not override `GetHeldItemName`: the patch applies to it (and to the
  `lore-paper` / `lore-scroll` variants). `book.json` (class `ItemBook`) is for player-written books.
- Hotbar: `Vintagestory.Client.NoObf.HudHotbar.RecomposeActiveSlotHoverText` (`VintagestoryLib.dll`) → `ItemStack.GetName()`.
- Inventory tooltip: `GuiElementItemstackInfo.AsyncRecompose` → `ItemSlot.GetStackName()` → `ItemStack.GetName()`.
- `ItemStack.GetName()` → `CollectibleObject.GetHeldItemName` (virtual) → `ItemBook.GetHeldItemName`.

## How the mod works
A Harmony postfix on `ItemBook.GetHeldItemName` replaces the name with `Lang.Get(titleCode)`
when `titleCode` is present and translated. `modinfo.json`: `"side": "Client"`.
It appends ` [n/total]`: `n` = `chapterIds[0] + 1` (book attribute; one chapter per book, see
`ModJournal.getNextUndiscoveredChapter`), `total` = number of `pieces` of the `discoveryCode` story, read at
client startup from `config/lore/` (`JournalAsset`, public; the `config` category is `Universal`, so available client-side).

## Status / known limits
- `dotnet build` passes (0 warnings, 0 errors); DLL paths confirmed. Output: `bin/<Config>/Mods/mod/`.
- The build copies nothing into the game: a normal game launch does not load the mod, use F5 (`Client` profile).
- Tested in game on 2026-11-18, ` [n/total]` suffix included: the title shows in the inventory, on the hotbar
  and in the top info panel when looking at the book on a bookshelf. Also validated on LAN, joining another
  player's game.
- Works in every game language: the title comes from the game's translations (`Lang.Get(titleCode)`).
- A book that was never read keeps the generic name (no `titleCode` yet). The title only appears after the first read.

## Roadmap
**1.0.0 published on 2026-11-18**: ModDB https://mods.vintagestory.at/lorebooktitles and GitHub release
https://github.com/Mitsuriou/LoreBookTitles/releases/tag/1.0.0 (tag `1.0.0`, no `v` prefix, on commit `0ec7f17`).

Next releases: bump `version` in `modinfo.json`, Release build, upload the new zip to the ModDB page and to a
GitHub release whose tag is the bare version (same format as `1.0.0`).

1. **DONE: prepare the release on https://mods.vintagestory.at/**
   - A Release build (`dotnet build -c Release` or Release in VS) creates `bin/Release/LoreBookTitles_<version>.zip`
     through the `PackageMod` target in the `.csproj`. The zip has `modinfo.json`, the DLL, the pdb and deps.json at
     its root, and its name uses the version from `modinfo.json`. Only bump that version for releases AFTER 1.0.0.
   - No existing mod with this name, modid or feature ("lore" and "book" searches on the ModDB, 2026-11-18).
   - ModDB rule (Terms page): an English description is required, clearly explaining what the mod does.
     Nothing found about required zip contents or the logo (it seems optional).
2. **DONE: GitHub repo https://github.com/Mitsuriou/LoreBookTitles** (public, `main` branch, SSH remote `origin`).
   Code pushed (commit `0ec7f17`, on top of the MIT `LICENSE` commit created by GitHub). Standard Visual Studio
   `.gitignore`, English `README.md` with the AI transparency note and the ModDB / GitHub releases links.
3. **DONE: GitHub link in the `website` field of `modinfo.json`** (still 1.0.0), Release zip rebuilt.
4. **DONE: 1.0.0 published on the ModDB and as a GitHub release** (by the user). To test a zip outside VS, copy it
   into `%APPDATA%\VintagestoryData\Mods` (not together with F5, or the modid is loaded twice).
   Still open: screenshots for the ModDB page. Optional: `modicon.png` at the mod root (mod manager icon).
- DONE: AI transparency on the ModDB page and in the README: all of the code was generated by Claude Opus 5.5
  (Claude Code, xhigh effort) based on human ideas, and the final code was reviewed by a human.
- DONE: real title when looking at a book stored on a bookshelf. No extra code:
  `BlockEntityBookshelf.GetBlockInfo` (top centered info panel) shows `ItemStack.GetName()` of the targeted slot.
  Same for `BlockEntityScrollRack.GetBlockInfo` (scrolls). Rejected alternative: a floating popup that follows
  the camera (custom HUD reprojecting the slot position every frame), only if the panel is not enough.

## Suggested test
1. Start the game with F5, check that "Lore Book Titles" shows up in the Mod Manager.
2. Get a lore book, read it once, then check its name in the inventory and on the hotbar.
   Then put it on a bookshelf and look at it: the top panel must show its title.
   The name must end with ` [n/total]`, e.g. ` [1/8]` for the first book of "blindbatrickhart".
3. Check `VintagestoryData/Logs/client-main.log` if the mod fails to load or the Harmony patch fails.
