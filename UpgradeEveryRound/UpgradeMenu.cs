using MenuLib;
using MenuLib.MonoBehaviors;
using Steamworks;
using System.Collections.Generic;
using System.Linq;
using REPOLib.Modules;
using UnityEngine;
using Random = UnityEngine.Random;
using System;

namespace UpgradeEveryRound
{
    public static class UpgradeMenu
    {
        public static bool isOpen = false;
        public static bool choseUpgrade = false;

        public static void OpenMenu()
        {
            if (isOpen) return;
            Level[] bannedLevels = [RunManager.instance.levelMainMenu, RunManager.instance.levelLobbyMenu, RunManager.instance.levelTutorial];
            if (bannedLevels.Contains(RunManager.instance.levelCurrent)) return;

            string _steamID = SteamClient.SteamId.Value.ToString();
            int upgradesDeserved = RunManager.instance.levelsCompleted * Plugin.UpgradesPerRound;

#if DEBUG
            upgradesDeserved += 1;
#endif
            if (Plugin.NumUpgradesUsed(_steamID) >= upgradesDeserved) return;
            //if (GameManager.Multiplayer() && !___photonView.IsMine) return;

            MenuManager.instance.PageCloseAll(); //Just in case somehow other menus were opened previously.

            var repoPopupPage = createMenu(_steamID);

            isOpen = true;
            repoPopupPage.OpenPage(false);
        }

        public static REPOPopupPage createMenu(string _steamID)
        {
            choseUpgrade = false;

            StatsUI.instance.Fetch();
            StatsUI.instance.ShowStats();

            REPOPopupPage repoPopupPage = MenuAPI.CreateREPOPopupPage("Choose an upgrade", REPOPopupPage.PresetSide.Right, shouldCachePage: false, pageDimmerVisibility: true, spacing: 1.5f);

            repoPopupPage.menuPage.onPageEnd.AddListener(() => { isOpen = false; }); //They really shouldn't be able to close it, but just in case we want to make sure their menus work
            // Nevermind, let them close it in case the plugin breaks
            //repoPopupPage.onEscapePressed = new REPOPopupPage.ShouldCloseMenuDelegate(() => false);

            List<int> choices = [0, 1, 2, 3, 4, 5, 6, 7];

            List<bool> allowed = [Plugin.AllowEnergy, Plugin.AllowExtraJump, Plugin.AllowRange, Plugin.AllowStrength, Plugin.AllowHealth, Plugin.AllowSpeed, Plugin.AllowTumbleLaunch, Plugin.AllowMapCount];

            foreach (var customAllow in Plugin.AllowExtras) {
                var baseIndex = customAllow.Key;
                choices.Add(choices.Count);
                allowed.Add(Plugin.AllowCustomUpgradeByIndex(baseIndex));
            }

            int numChoices = Plugin.LimitedChoices ? Plugin.NumChoices : choices.Count;

            int removed = 0;
            for (int i = 0; i < choices.Count; i++)
            {
                if (!allowed[i])
                {
                    choices.RemoveAt(i - removed);
                    removed++;
                }
            }

            numChoices = Mathf.Min(numChoices, choices.Count);

            //Add limited buttons randomly or all in order depending on config
            Vector2[] positions = [
                                      new Vector2(390f, 18f), new Vector2(530f, 18f),
                                      new Vector2(390f, 60f), new Vector2(530f, 60f),
                                      new Vector2(390f, 102f), new Vector2(530f, 102f),
                                      new Vector2(390f, 144f), new Vector2(530f, 144f)
                                  ];
            
            for (int i = 0; i < numChoices; i++)
            {
                int choiceIndex = (Plugin.LimitedChoices) ? Random.Range(0, choices.Count) : 0; //If not limited choices then we don't need to use random
                int choice = choices[choiceIndex];
                choices.RemoveAt(choiceIndex);
                var positionIdx = i;

                switch (choice)
                {
                    case 0:
                        repoPopupPage.AddElement(parent => CreateButton(parent, "Stamina", () => PunManager.instance.UpgradePlayerEnergy(_steamID), i, numChoices, _steamID, repoPopupPage));
                        break;

                    case 1:
                        repoPopupPage.AddElement(parent => CreateButton(parent, "Extra Jump", () => PunManager.instance.UpgradePlayerExtraJump(_steamID), i, numChoices, _steamID, repoPopupPage));
                        break;

                    case 2:
                        repoPopupPage.AddElement(parent => CreateButton(parent, "Range", () => PunManager.instance.UpgradePlayerGrabRange(_steamID), i, numChoices, _steamID, repoPopupPage));
                        break;

                    case 3:
                        repoPopupPage.AddElement(parent => CreateButton(parent, "Strength", () => PunManager.instance.UpgradePlayerGrabStrength(_steamID), i, numChoices, _steamID, repoPopupPage));
                        break;

                    case 4:
                        repoPopupPage.AddElement(parent => CreateButton(parent, "Health", () => PunManager.instance.UpgradePlayerHealth(_steamID), i, numChoices, _steamID, repoPopupPage));
                        break;

                    case 5:
                        repoPopupPage.AddElement(parent => CreateButton(parent, "Sprint speed", () => PunManager.instance.UpgradePlayerSprintSpeed(_steamID), i, numChoices, _steamID, repoPopupPage));
                        break;

                    case 6:
                        repoPopupPage.AddElement(parent => CreateButton(parent, "Tumble Launch", () => PunManager.instance.UpgradePlayerTumbleLaunch(_steamID), i, numChoices, _steamID, repoPopupPage));
                        break;

                    case 7:
                        repoPopupPage.AddElement(parent => CreateButton(parent, "Map Player Count", () => PunManager.instance.UpgradeMapPlayerCount(_steamID), i, numChoices, _steamID, repoPopupPage));
                        break;

                    default:
                        repoPopupPage.AddElement(parent => CreateButton(parent, Plugin.ExtraLabels[choice - 8], () => Plugin.DoUpgrade(Plugin.ExtraLabels[choice - 8], _steamID), i, numChoices, _steamID, repoPopupPage));
                        break;
                }
            }
            return repoPopupPage;
        }

        

        private static REPOButton CreateButton(Transform parent, string buttonText, Action upgradeStat, int buttonNumber, int totalButtons, string _steamID, REPOPopupPage page)
        {
            (Vector2 pos, float scale) = GetButtonPos(buttonNumber, totalButtons);
            var button = MenuAPI.CreateREPOButton(
                buttonText,
                () =>
                {
                    if (choseUpgrade) return;
                    choseUpgrade = true;
                    upgradeStat.Invoke();
                    Plugin.ApplyUpgrade(_steamID, page);
                    return;
                },
                parent,
                pos
            );

            RectTransform transform = button.gameObject.GetComponent<RectTransform>();
            transform.localScale = new Vector3(scale, scale, 1f);

            return button;
        }

        //Calculate UI scale and pos such that the buttons take up as much of the available space as possible
        //Yeah I'm gonna be real I don't know if I can even explain how this works, but it does.
        private static (Vector2, float) GetButtonPos(int buttonNumber, int totalButtons) {
            const float pageMinX = 370;
            const float pageMinY = 18;

            const float pageMaxX = 740;
            const float pageMaxY = 450;

            const float buttonHeightDefault = 42;
            const float buttonWidthDefault = 180;

            if (totalButtons <= 8) return (new Vector2(pageMinX + buttonWidthDefault * (buttonNumber % 2), pageMinY + buttonHeightDefault * (buttonNumber / 2)), 1f);

            float pageWidth = pageMaxX - pageMinX;
            float pageHeight = pageMaxY - pageMinY;

            int buttonsPerRow = (int)(pageWidth / buttonHeightDefault);
            float scaleMult = pageHeight / (buttonHeightDefault * (totalButtons / buttonsPerRow));

            float buttonHeight = scaleMult * buttonHeightDefault;
            float buttonWidth = scaleMult * buttonWidthDefault;

            buttonsPerRow = Math.Max((int)(pageWidth / buttonWidth), 2);

            do
            {
                if (buttonsPerRow == 1) break;
                buttonsPerRow -= 1;
                scaleMult = Math.Min(pageHeight / (buttonHeightDefault * (totalButtons / buttonsPerRow)), 2f / buttonsPerRow);
                buttonHeight = scaleMult * buttonHeightDefault;
                buttonWidth = scaleMult * buttonWidthDefault;
            } while ((totalButtons / buttonsPerRow) * buttonHeight < pageHeight);

            buttonsPerRow += 1;
            scaleMult = Math.Min(pageHeight / (buttonHeightDefault * (totalButtons / buttonsPerRow)), 2f / buttonsPerRow);

            return (new Vector2(pageMinX + (buttonNumber % buttonsPerRow) * buttonWidth, pageMinY + (buttonNumber /  buttonsPerRow) * buttonHeight), scaleMult * 0.7f);
        }
    }
}
