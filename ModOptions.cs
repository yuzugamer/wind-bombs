using Menu.Remix.MixedUI;
using UnityEngine;

namespace WindBombs;

public sealed class ModOptions : OptionInterface
{
    public static Configurable<bool> NaturalWindBombSpawns = new Configurable<bool>(false);
    public static Configurable<float> WindBombSpawnMult = new Configurable<float>(1f);


    public ModOptions()
    {
        NaturalWindBombSpawns = config.Bind("cfgNaturalWindBombSpawns", false);
        WindBombSpawnMult = config.Bind("cfgWindBombSpawnMult", 1f, new ConfigurableInfo("The spawn rate can't reach 100%, but can still get fairly high", new ConfigAcceptableRange<float>(0f, 20f), "", ["Wind Bomb spawn multiplier"]));
    }

    public override void Initialize()
    {
        base.Initialize();
        Tabs = new OpTab[] { new OpTab(this) };
        var modName = new OpLabel(new Vector2(150f, 520f), new Vector2(300f, 30f), "Wind Bombs", FLabelAlignment.Center, true);
        var naturalSpawnsButton = new OpCheckBox(NaturalWindBombSpawns, 195f, 460f);
        var naturalSpawnsLabel = new OpLabel(227f, 463f, "Naturally spawn Wind Bombs", false);
        var spawnMultInput = new OpTextBox(WindBombSpawnMult, new Vector2(184f, 413f), 50f) { accept = OpTextBox.Accept.Float };
        var spawnMultLabel = new OpLabel(226f, 416f, "Wind Bomb spawn multiplier", false) { description = "The spawn rate can't reach 100%, but can still get fairly high" };
        spawnMultLabel.alignment = FLabelAlignment.Center;
        Tabs[0].AddItems(modName, naturalSpawnsButton, naturalSpawnsLabel, spawnMultInput, spawnMultLabel);
    }
}