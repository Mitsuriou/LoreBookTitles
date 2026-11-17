# LoreBookTitles — mod Vintage Story (côté client)

## Objectif
Afficher le vrai titre d'un livre d'histoire (lore book) dans l'inventaire et la hotbar,
au lieu du nom générique « Livre d'histoire vieilli » / « Livre d'histoire pourrissant ».

## Environnement
- Vintage Story 1.22.7, Windows 11, Visual Studio 2026 (pack C#), .NET 10 (`net10.0`).
- Jeu installé dans `%APPDATA%\Vintagestory` (contient `assets`, `Mods`, `Lib`). La variable `VINTAGE_STORY`
  n'est PAS définie sur la machine : le `.csproj` retombe sur `$(APPDATA)\Vintagestory` quand elle est vide.
- Projet basé sur le template officiel (`vsmodtemplate`), sans le projet CakeBuild.
- Build : `dotnet build` ou F5 dans VS (profil `Client` de `Properties/launchSettings.json`,
  qui lance le jeu avec `--addModPath bin/<Config>/Mods`).

## Ce qui a été vérifié dans le code du jeu (GitHub anegostudios)
- `ItemBook : ItemRollable`, namespace `Vintagestory.GameContent` (`vssurvivalmod`, `Systems/WritingSystem/ItemBook.cs`).
- `ItemBook.GetHeldItemName(ItemStack)` ne lit que l'attribut `title` du stack, sinon le nom de base.
- `ItemStack.GetName()` appelle `Collectible.GetHeldItemName(this)` (`vsapi`).
- `GuiDialogReadonlyBook` affiche `Lang.Get(stack.Attributes.GetString("titleCode"))` pour les livres à `textCodes`.
- `ModJournal.TryDiscoverLore` (`Systems/Journal.cs`) écrit `discoveryCode`, `chapterIds`, `textCodes` et
  `titleCode` sur le livre **uniquement à la première découverte** (première lecture), pas à la génération
  ni au ramassage.

## Vérifié dans les DLL locales (IL lu avec Mono.Cecil, 1.22.7)
- Les livres d'histoire sont `item-lore-book-<color>` (`assets/survival/itemtypes/lore/lore.json`), classe
  `ItemRandomLore : ItemBook`, qui ne redéfinit pas `GetHeldItemName` : le patch s'y applique (aussi aux
  variantes `lore-paper` / `lore-scroll`). `book.json` (classe `ItemBook`) concerne les livres écrits par le joueur.
- Hotbar : `Vintagestory.Client.NoObf.HudHotbar.RecomposeActiveSlotHoverText` (`VintagestoryLib.dll`) → `ItemStack.GetName()`.
- Infobulle d'inventaire : `GuiElementItemstackInfo.AsyncRecompose` → `ItemSlot.GetStackName()` → `ItemStack.GetName()`.
- `ItemStack.GetName()` → `CollectibleObject.GetHeldItemName` (virtuel) → `ItemBook.GetHeldItemName`.

## Fonctionnement du mod
Un patch Harmony (postfix) sur `ItemBook.GetHeldItemName` remplace le nom par `Lang.Get(titleCode)`
quand `titleCode` est présent et traduit. `modinfo.json` : `"side": "Client"`.
Il ajoute ` [n/total]` : `n` = `chapterIds[0] + 1` (attribut du livre ; un seul chapitre par livre, cf.
`ModJournal.getNextUndiscoveredChapter`), `total` = nombre de `pieces` de l'histoire `discoveryCode`, lu au
démarrage client dans `config/lore/` (`JournalAsset`, public ; catégorie `config` = `Universal`, donc dispo côté client).

## État / limites connues
- `dotnet build` passe (0 avertissement, 0 erreur) ; chemins de DLL confirmés. Sortie : `bin/<Config>/Mods/mod/`.
- Le build ne copie rien dans le jeu : un lancement normal ne charge pas le mod, il faut F5 (profil `Client`).
- Testé en jeu le 2026-11-18, suffixe ` [n/total]` compris : le titre s'affiche dans l'inventaire, sur la hotbar
  et dans le panneau du haut quand on pointe le livre dans une bibliothèque. Validé aussi en LAN, en rejoignant
  la partie d'un autre joueur.
- Fonctionne dans toutes les langues du jeu : le titre vient des traductions du jeu (`Lang.Get(titleCode)`).
- Un livre jamais lu garde le nom générique (pas encore de `titleCode`). Le titre n'apparaît qu'après la première lecture.

## Roadmap
Rien n'est encore publié. La première release publique sera la **1.0.0**, avec le lien GitHub déjà dans `modinfo.json`.
Ordre : 1) préparation (faite), 2) repo GitHub, 3) lien GitHub dans le mod, 4) publication sur le ModDB.

1. **FAIT : préparation de la publication sur https://mods.vintagestory.at/**
   - Un build Release (`dotnet build -c Release` ou Release dans VS) crée `bin/Release/LoreBookTitles_<version>.zip`
     via la cible `PackageMod` du `.csproj`. Le zip contient `modinfo.json`, la DLL, le pdb et deps.json à la racine,
     et son nom reprend la version de `modinfo.json`. Augmenter cette version seulement pour les releases APRÈS la 1.0.0.
   - Aucun mod existant avec ce nom, ce modid ou cette fonction (recherches « lore » et « book » sur le ModDB, 2026-11-18).
   - Règle du ModDB (page Terms) : description en anglais obligatoire, qui explique clairement ce que fait le mod.
     Rien trouvé sur le contenu exigé du zip ni sur le logo (il semble optionnel).
2. **Repo GitHub : https://github.com/Mitsuriou/LoreBookTitles** (public, branche `main`). GitHub l'a créé avec un
   seul commit, le `LICENSE` MIT (copyright Mitsuriou) : partir de ce commit avant de pousser le code local.
   `.gitignore` (`bin/`, `obj/`, `.vs/`, `*.user`) et `README.md` (en anglais, avec la mention de transparence IA) sont prêts.
   À FAIRE : premier commit et push.
3. **FAIT : lien GitHub dans le champ `website` de `modinfo.json`** (toujours en 1.0.0), zip Release refait.
4. **Publier la 1.0.0 sur le ModDB** (par l'utilisateur, avec son compte) : tester le zip seul en le copiant dans
   `%APPDATA%\VintagestoryData\Mods` (pas en même temps que F5, sinon le modid est chargé deux fois), créer la page
   du mod (Code Mod, Client side, 1.22.x) avec le lien GitHub, puis uploader le zip.
   Optionnel : `modicon.png` à la racine du mod (icône du gestionnaire de mods), captures d'écran pour la page.
- **Transparence IA (page ModDB ; déjà dans le README)** : indiquer que tout le code a été généré par Claude Opus 5.5
  (Claude Code, effort xhigh) à partir d'idées humaines, et que le code final a été contrôlé par un humain.
- FAIT : titre réel quand on pointe un livre rangé dans une bibliothèque. Aucun code en plus :
  `BlockEntityBookshelf.GetBlockInfo` (panneau d'info centré en haut) affiche `ItemStack.GetName()` du slot pointé.
  Même chose pour `BlockEntityScrollRack.GetBlockInfo` (parchemins). Alternative écartée : un popup flottant qui
  suit la caméra (HUD maison qui reprojette la position du slot à chaque image), seulement si le panneau ne suffit pas.

## Test conseillé
1. Lancer le jeu via F5, vérifier que « Lore Book Titles » apparaît dans le Mod Manager.
2. Obtenir un livre d'histoire, le lire une fois, puis regarder son nom dans l'inventaire et sur la hotbar.
   Le ranger ensuite dans une bibliothèque et le pointer : le panneau du haut doit afficher son titre.
   Le nom doit finir par ` [n/total]`, par ex. ` [1/8]` pour le premier livre de « blindbatrickhart ».
3. Consulter `VintagestoryData/Logs/client-main.log` en cas d'erreur de chargement ou de patch Harmony.
