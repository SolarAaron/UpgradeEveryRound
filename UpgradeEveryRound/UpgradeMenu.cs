using MenuLib;
using MenuLib.MonoBehaviors;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UpgradeEveryRound
{
    public static class UpgradeMenu
    {

        public static bool isOpen = false;

        public static REPOPopupPage createMenu(string _steamID)
        {
            StatsUI.instance.Fetch();
            StatsUI.instance.ShowStats();

            REPOPopupPage repoPopupPage = MenuAPI.CreateREPOPopupPage("Choose an upgrade", REPOPopupPage.PresetSide.Right, shouldCachePage: false, pageDimmerVisibility: true, spacing: 1.5f);

            repoPopupPage.menuPage.onPageEnd.AddListener(() => { isOpen = false; }); //They really shouldn't be able to close it, but just in case we want to make sure their menus work
            repoPopupPage.onEscapePressed = new REPOPopupPage.ShouldCloseMenuDelegate(() => false);

            int numChoices = Plugin.LimitedChoices ? Plugin.NumChoices : 8;
            List<int> choices = [0, 1, 2, 3, 4, 5, 6, 7];

            bool[] allowed = { Plugin.AllowEnergy, Plugin.AllowExtraJump, Plugin.AllowRange, Plugin.AllowStrength, Plugin.AllowHealth, Plugin.AllowSpeed, Plugin.AllowTumbleLaunch, Plugin.AllowMapCount };

            int removed = 0;
            for (int i = 0; i < choices.Count; i++)
            {
                if (!allowed[i])
                {
                    choices.RemoveAt(i - removed);
                    removed++;
                }
            }

            bool choseUpgrade = false;

            //Add limited buttons randomly or all in order depending on config
            for (int i = 0; i < numChoices; i++)
            {
                int choiceIndex = Plugin.LimitedChoices ? Random.Range(0, choices.Count) : 0; //If not limited choices then we don't need to use random
                int choice = choices[choiceIndex];
                choices.RemoveAt(choiceIndex);


                switch (choice)
                {
                    case 0:
                        repoPopupPage.AddElement(parent => MenuAPI.CreateREPOButton("Stamina", () =>
                        {
                            if (choseUpgrade) return; //Sanity check to prevent spamming the buttons.
                            choseUpgrade = true;
                            PunManager.instance.UpgradePlayerEnergy(_steamID);
                            Plugin.ApplyUpgrade(_steamID, repoPopupPage);
                            return;
                        }, parent, new Vector2(390f, 18f)));
                        break;

                    case 1:
                        repoPopupPage.AddElement(parent => MenuAPI.CreateREPOButton("Extra Jump", () =>
                        {
                            if (choseUpgrade) return; //Sanity check to prevent spamming the buttons.
                            choseUpgrade = true;
                            PunManager.instance.UpgradePlayerExtraJump(_steamID);
                            Plugin.ApplyUpgrade(_steamID, repoPopupPage);
                            return;
                        }, parent, new Vector2(530f, 18f)));
                        break;

                    case 2:
                        repoPopupPage.AddElement(parent => MenuAPI.CreateREPOButton("Range", () =>
                        {
                            if (choseUpgrade) return; //Sanity check to prevent spamming the buttons.
                            choseUpgrade = true;
                            PunManager.instance.UpgradePlayerGrabRange(_steamID);
                            Plugin.ApplyUpgrade(_steamID, repoPopupPage);
                            return;
                        }, parent, new Vector2(390f, 60f)));
                        break;

                    case 3:
                        repoPopupPage.AddElement(parent => MenuAPI.CreateREPOButton("Strength", () =>
                        {
                            if (choseUpgrade) return; //Sanity check to prevent spamming the buttons.
                            choseUpgrade = true;
                            PunManager.instance.UpgradePlayerGrabStrength(_steamID);
                            Plugin.ApplyUpgrade(_steamID, repoPopupPage);
                            return;
                        }, parent, new Vector2(530f, 60f)));
                        break;

                    case 4:
                        repoPopupPage.AddElement(parent => MenuAPI.CreateREPOButton("Health", () =>
                        {
                            if (choseUpgrade) return; //Sanity check to prevent spamming the buttons.
                            choseUpgrade = true;
                            PunManager.instance.UpgradePlayerHealth(_steamID);
                            Plugin.ApplyUpgrade(_steamID, repoPopupPage);
                            return;
                        }, parent, new Vector2(390f, 102f)));
                        break;

                    case 5:
                        repoPopupPage.AddElement(parent => MenuAPI.CreateREPOButton("Sprint speed", () =>
                        {
                            if (choseUpgrade) return; //Sanity check to prevent spamming the buttons.
                            choseUpgrade = true;
                            PunManager.instance.UpgradePlayerSprintSpeed(_steamID);
                            Plugin.ApplyUpgrade(_steamID, repoPopupPage);
                            return;
                        }, parent, new Vector2(530f, 102f)));
                        break;

                    case 6:
                        repoPopupPage.AddElement(parent => MenuAPI.CreateREPOButton("Tumble Launch", () =>
                        {
                            if (choseUpgrade) return; //Sanity check to prevent spamming the buttons.
                            choseUpgrade = true;
                            PunManager.instance.UpgradePlayerTumbleLaunch(_steamID);
                            Plugin.ApplyUpgrade(_steamID, repoPopupPage);
                            return;
                        }, parent, new Vector2(390f, 144f)));
                        break;

                    case 7:
                        repoPopupPage.AddElement(parent => MenuAPI.CreateREPOButton("Map Player Count", () =>
                        {
                            if (choseUpgrade) return; //Sanity check to prevent spamming the buttons.
                            choseUpgrade = true;
                            PunManager.instance.UpgradeMapPlayerCount(_steamID);
                            Plugin.ApplyUpgrade(_steamID, repoPopupPage);
                            return;
                        }, parent, new Vector2(530f, 144f)));
                        break;
                }
            }
            return repoPopupPage;
        }
    }
}
