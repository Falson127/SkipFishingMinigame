using System;
using System.Runtime;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewModdingAPI;
using StardewValley.Enchantments;
using StardewModdingAPI.Events;
using StardewValley.Menus;
using StardewValley.Minigames;
using StardewValley.Tools;
using Object = StardewValley.Object;

namespace PatchedNoFishing
{
    public class ModEntry : Mod
    {
      internal ModConfig Config;
      internal Random Random;
      internal AutoHookEnchantment autoHookEnchantment;
      private readonly HashSet<string> bossFish = new HashSet<string>() {"159","160","163","682","775" };
      private int _fishCaught;

      public override void Entry(IModHelper helper)
      {
        this.Config = this.Helper.ReadConfig<ModConfig>();
        this.Log(string.Format("Config.PerfectCatchChance [{0}]", (object) this.Config.PerfectCatchChance));
        this.Log(string.Format("Config.IsAutoHook [{0}]", (object) this.Config.IsAutoHook));
        this.Random = new Random();
        helper.Events.Display.MenuChanged += new EventHandler<MenuChangedEventArgs>(this.Display_MenuChanged);
        helper.Events.Input.ButtonPressed += new EventHandler<ButtonPressedEventArgs>(this.Input_ButtonPressed);
      }

      private void Display_MenuChanged(object sender, MenuChangedEventArgs e)
      {
        _fishCaught = (int)Game1.stats.FishCaught;
        this.Log(string.Format("MenuEvents_MenuChanged {0}", (object)e.NewMenu));
        this.Log(string.Format("Game1.player.CurrentTool {0}", (object)Game1.player.CurrentTool));
        this.Log(string.Format("Is Festival [{0}]", (object)Game1.isFestival()));
        this.Log(string.Format("Is Event Up [{0}]", (object)Game1.eventUp));
        if (!(e.NewMenu is BobberBar newMenu) || !(Game1.player.CurrentTool is FishingRod currentTool))
          return;
        if (_fishCaught < Config.MinimumCatchesRequired){
          this.Log($"Go fish! Caught {_fishCaught}, but minimum catches required to skip {Config.MinimumCatchesRequired}");
        }else{
          string num1 = this.Helper.Reflection.GetField<string>((object)newMenu, "whichFish", true).GetValue();
          int num2 = this.Helper.Reflection.GetField<int>((object)newMenu, "fishSize", true).GetValue();
          int num3 = this.Helper.Reflection.GetField<int>((object)newMenu, "fishQuality", true).GetValue();
          float num4 = this.Helper.Reflection.GetField<float>((object)newMenu, "difficulty", true).GetValue();
          bool flag1 = this.Helper.Reflection.GetField<bool>((object)newMenu, "treasure", true).GetValue();
          bool flag2 = this.Helper.Reflection.GetField<bool>((object)newMenu, "perfect", true).GetValue();
          bool flag3 = this.Helper.Reflection.GetField<bool>((object)newMenu, "fromFishPond", true).GetValue();
          Vector2 vector2 = this.Helper.Reflection.GetField<Vector2>((object)newMenu, "fishShake", true).GetValue();
          int num5 = !(Game1.currentMinigame is FishingGame)
            ? -1
            : this.Helper.Reflection.GetField<int>(Game1.currentMinigame, "perfections", true).GetValue();
          bool flag4 = this.Helper.Reflection.GetField<bool>((object)newMenu, "bossFish", true).GetValue();
          int num6 =
            Game1.player.CurrentTool == null || !(Game1.player.CurrentTool is FishingRod) ||
            (Game1.player.CurrentTool as FishingRod).attachments[0] == null
              ? -1
              : (Game1.player.CurrentTool as FishingRod).attachments[0].ParentSheetIndex;
          //bool flag5 = !flag4 && num6 == 774 && Game1.random.NextDouble() < 0.25 + Game1.player.DailyLuck / 2.0;
          if (this.Config.PerfectCatchChance < 100)
          {
            int num7 = this.Random.Next(100);
            this.Log(string.Format("perfectCatchRoll [{0}]", (object)num7));
            flag2 = num7 <= this.Config.PerfectCatchChance;
          }

          if (flag2 && Game1.currentMinigame is FishingGame)
            Game1.CurrentEvent.perfectFishing();
          bool flag6 = flag1 && !this.Config.IsIgnoreTreasureChests;

          string fish = num1.ToString();
          bool isBoss = bossFish.Contains(num1.ToString());
          currentTool.pullFishFromWater(fish, num2, num3, (int)num4, flag6, flag2, flag3, "", isBoss, 1);
          Game1.exitActiveMenu();
          Game1.setRichPresence("location", (object)Game1.currentLocation.Name);
        }
      }

      private static int GetPlayerFishingExp() => Game1.player.experiencePoints[1];

      private void Input_ButtonPressed(object sender, ButtonPressedEventArgs e)
      {
        if (!Context.IsWorldReady)
          return;
        this.Log(string.Format("Game1.player.CurrentTool [{0}]. isFishingRod [{1}]", (object) Game1.player.CurrentTool, (object) (Game1.player.CurrentTool is FishingRod)));
        if (!SButtonExtensions.IsUseToolButton(e.Button) || !(Game1.player.CurrentTool is FishingRod currentTool) || !Game1.player.CanMove)
          return;
        if (this.autoHookEnchantment == null)
          this.autoHookEnchantment = new AutoHookEnchantment();
        this.Log(string.Format("AutoHookEnchantment set to [{0}] successfully? [{1}]", (object) this.Config.IsAutoHook, (object) this.TrySetEnchantment((Tool) currentTool, (BaseEnchantment) this.autoHookEnchantment, this.Config.IsAutoHook)));
      }

      private bool TrySetEnchantment(
        Tool tool,
        BaseEnchantment newEnchantment,
        bool isApplyEnchantment)
      {
        if (isApplyEnchantment)
        {
          foreach (BaseEnchantment enchantment in tool.enchantments)
          {
            if (enchantment.GetName() == newEnchantment.GetName())
            {
              this.Log("[" + ((Item) tool).Name + "] already has [" + newEnchantment.GetName() + "]");
              return false;
            }
          }
          this.Log("Applying [" + newEnchantment.GetName() + "] to [" + ((Item) tool).Name + "]");
          tool.enchantments.Add(newEnchantment);
          return true;
        }
        foreach (BaseEnchantment enchantment in tool.enchantments)
        {
          if (enchantment.GetName() == newEnchantment.GetName())
          {
            this.Log("Removing [" + enchantment.GetName() + "] from [" + ((Item) tool).Name + "]");
            tool.enchantments.Remove(enchantment);
            return true;
          }
        }
        this.Log("[" + ((Item) tool).Name + "] does not have [" + newEnchantment.GetName() + "] to remove");
        return false;
      }

      internal void Log(string message, LogLevel logLevel = LogLevel.Warn)
      {
      }
    }

}