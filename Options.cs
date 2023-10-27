using System.Diagnostics.CodeAnalysis;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace Spookcat;
public class SpookyOptions : OptionInterface {
    [AllowNull] public static Configurable<bool> FlashingLights { get; set; }

    public SpookyOptions() {
        FlashingLights = config.Bind("flashingSelectScreen", true);
    }

    public override void Initialize() {
        OpTab newTab = new (this, "Options");
        Tabs = new[]
        {
            newTab
        };

        UIelement[]? UIArrPlayerOptions = new UIelement[]
        {
            new OpLabel(100f, 525f, Translate("Flashing Lights"), false),
            new OpCheckBox(FlashingLights, new Vector2(400f, 520f)) {description = Translate("Select Screen Effects")}
        };
        newTab.AddItems(UIArrPlayerOptions);
    }
}