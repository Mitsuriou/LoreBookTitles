using System.Collections.Generic;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace LoreBookTitles;

public class LoreBookTitlesModSystem : ModSystem
{
    private Harmony harmony;

    public override void StartClientSide(ICoreClientAPI api)
    {
        // The game only loads these on the server (ModJournal), but config assets are available client-side too
        var chapterCounts = new Dictionary<string, int>();
        foreach (JournalAsset asset in api.Assets.GetMany<JournalAsset>(api.Logger, "config/lore/").Values)
        {
            chapterCounts[asset.Code] = asset.Pieces.Length;
        }
        ItemBookNamePatch.ChapterCounts = chapterCounts;

        harmony = new Harmony(Mod.Info.ModID);
        harmony.PatchAll(typeof(LoreBookTitlesModSystem).Assembly);
    }

    public override void Dispose()
    {
        harmony?.UnpatchAll(Mod.Info.ModID);
    }
}

// The game stores the lore book's title as a lang key in the "titleCode" attribute
// (the read-only book dialog displays Lang.Get(titleCode)), but ItemBook.GetHeldItemName
// only looks at the "title" attribute. Items and the hotbar get their name from
// ItemStack.GetName() -> GetHeldItemName, so we fill that gap here, adding "[chapter/total]".
[HarmonyPatch(typeof(ItemBook), nameof(ItemBook.GetHeldItemName))]
public static class ItemBookNamePatch
{
    // Lore story code (JournalAsset.Code) -> number of chapters (JournalAsset.Pieces)
    public static Dictionary<string, int> ChapterCounts = new();

    public static void Postfix(ItemStack itemStack, ref string __result)
    {
        ITreeAttribute attributes = itemStack?.Attributes;
        string titleCode = attributes?.GetString("titleCode");
        if (string.IsNullOrEmpty(titleCode)) return;

        string title = Lang.Get(titleCode);
        if (string.IsNullOrEmpty(title) || title == titleCode) return;

        // ModJournal.getNextUndiscoveredChapter puts a single chapter on each book
        int[] chapterIds = (attributes["chapterIds"] as IntArrayAttribute)?.value;
        string discoveryCode = attributes.GetString("discoveryCode");
        if (chapterIds?.Length > 0 && discoveryCode != null && ChapterCounts.TryGetValue(discoveryCode, out int total))
        {
            title += $" [{chapterIds[0] + 1}/{total}]";
        }

        __result = title;
    }
}
