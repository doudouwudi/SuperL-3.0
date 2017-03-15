#region Licensing
// ---------------------------------------------------------------------
// <copyright file="MenuValues.cs" company="EloBuddy">
// 
// Marksman Master
// Copyright (C) 2016 by gero
// All rights reserved
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/. 
// </copyright>
// <summary>
// 
// Email: geroelobuddy@gmail.com
// PayPal: geroelobuddy@gmail.com
// </summary>
// ---------------------------------------------------------------------
#endregion
namespace Marksman_Master.Utils
{
    using EloBuddy.SDK.Menu;

    using EloBuddy.SDK.Menu.Values;

    internal sealed class MenuValues
    {
        public bool this[string key]
        {
            get
            {
                var menu = ParseMenuNames.GetMenu(key);

                if (menu?[key] != null)
                {
                    var type = menu[key].GetType();

                    switch (type.ToString())
                    {
                        case "EloBuddy.SDK.Menu.Values.CheckBox":
                            return menu[key].Cast<CheckBox>().CurrentValue;
                        case "EloBuddy.SDK.Menu.Values.KeyBind":
                            return menu[key].Cast<KeyBind>().CurrentValue;
                        default:
                            Misc.PrintDebugMessage("Menu item : " + key + " is not oftype bool.");
                            break;
                    }
                }

                Misc.PrintDebugMessage("Menu item : " + key + " doesn't exists.");

                return false;
            }
            set
            {
                var menu = ParseMenuNames.GetMenu(key);

                if (menu?[key] != null)
                {
                    var type = menu[key].GetType();

                    switch (type.ToString())
                    {
                        case "EloBuddy.SDK.Menu.Values.CheckBox":
                            menu[key].Cast<CheckBox>().CurrentValue = value;
                            break;
                        case "EloBuddy.SDK.Menu.Values.KeyBind":
                            menu[key].Cast<KeyBind>().CurrentValue = value;
                            break;
                        default:
                            Misc.PrintDebugMessage("Menu item : " + key + " is not oftype bool.");
                            break;
                    }
                }
                Misc.PrintDebugMessage("Menu item : " + key + " doesn't exists.");
            }
        }

        public int this[string key, bool returnInt = true]
        {
            get
            {
                var menu = ParseMenuNames.GetMenu(key);

                if (menu?[key] != null)
                {
                    var type = menu[key].GetType();

                    switch (type.ToString())
                    {
                        case "EloBuddy.SDK.Menu.Values.ComboBox":
                            return menu[key].Cast<ComboBox>().CurrentValue;
                        case "EloBuddy.SDK.Menu.Values.Slider":
                            return menu[key].Cast<Slider>().CurrentValue;
                        default:
                            Misc.PrintDebugMessage("Menu item : " + key + " is not oftype int.");
                            break;
                    }
                }

                Misc.PrintDebugMessage("Menu item : " + key + " doesn't exists.");

                return 0;
            }
            set
            {
                var menu = ParseMenuNames.GetMenu(key);

                if (menu?[key] != null)
                {
                    var type = menu[key].GetType();

                    switch (type.ToString())
                    {
                        case "EloBuddy.SDK.Menu.Values.ComboBox":
                            menu[key].Cast<ComboBox>().CurrentValue = value;
                            break;
                        case "EloBuddy.SDK.Menu.Values.Slider":
                            menu[key].Cast<Slider>().CurrentValue = value;
                            break;
                        default:
                            Misc.PrintDebugMessage("Menu item : " + key + " is not oftype int.");
                            break;
                    }
                }
                else
                {
                    Misc.PrintDebugMessage("Menu item : " + key + " doesn't exists.");
                }
            }
        }
    }

    internal sealed class ParseMenuNames
    {
        public static Menu GetMenu(string uniqueIdentifier)
        {
            var splitted = uniqueIdentifier.Split('.');

            switch (splitted[0])
            {
                case "Activator":
                    switch (splitted[1])
                    {
                        case "ItemsMenu":
                            return Activator.Activator.ItemsMenu;
                        case "PotionsAndElixirsMenu":
                            return Activator.Activator.PotionsAndElixirsMenu;
                        case "CleanseMenu":
                            return Activator.Activator.CleanseMenu;
                        case "SummonersMenu":
                            return Activator.Activator.SummonersMenu;
                        default:
                            return Activator.Activator.ActivatorMenu;
                    }
                case "MenuManager":
                    switch (splitted[1])
                    {
                        case "GapcloserMenu":
                            return MenuManager.GapcloserMenu;
                        case "InterrupterMenu":
                            return MenuManager.InterrupterMenu;
                        default:
                            return MenuManager.Menu;
                    }
                case "Plugins":
                    switch (splitted[1])
                    {
                        case "Ashe":
                        {
                            switch (splitted[2])
                            {
                                case "ComboMenu":
                                    return Plugins.Ashe.Ashe.ComboMenu;
                                case "HarassMenu":
                                    return Plugins.Ashe.Ashe.HarassMenu;
                                case "LaneClearMenu":
                                    return Plugins.Ashe.Ashe.LaneClearMenu;
                                case "MiscMenu":
                                    return Plugins.Ashe.Ashe.MiscMenu;
                                case "DrawingsMenu":
                                    return Plugins.Ashe.Ashe.DrawingsMenu;
                                default:
                                    return MenuManager.Menu;
                            }
                        }
                        case "Caitlyn":
                        {
                            switch (splitted[2])
                            {
                                case "ComboMenu":
                                    return Plugins.Caitlyn.Caitlyn.ComboMenu;
                                case "HarassMenu":
                                    return Plugins.Caitlyn.Caitlyn.HarassMenu;
                                case "LaneClearMenu":
                                    return Plugins.Caitlyn.Caitlyn.LaneClearMenu;
                                case "MiscMenu":
                                    return Plugins.Caitlyn.Caitlyn.MiscMenu;
                                case "DrawingsMenu":
                                    return Plugins.Caitlyn.Caitlyn.DrawingsMenu;
                                default:
                                    return MenuManager.Menu;
                            }
                        }
                        case "Corki":
                        {
                            switch (splitted[2])
                            {
                                case "ComboMenu":
                                    return Plugins.Corki.Corki.ComboMenu;
                                case "HarassMenu":
                                    return Plugins.Corki.Corki.HarassMenu;
                                case "LaneClearMenu":
                                    return Plugins.Corki.Corki.LaneClearMenu;
                                case "JungleClearMenu":
                                    return Plugins.Corki.Corki.JungleClearMenu;
                                case "MiscMenu":
                                    return Plugins.Corki.Corki.MiscMenu;
                                case "DrawingsMenu":
                                    return Plugins.Corki.Corki.DrawingsMenu;
                                default:
                                    return MenuManager.Menu;
                            }
                        }
                        case "Draven":
                        {
                            switch (splitted[2])
                            {
                                case "ComboMenu":
                                    return Plugins.Draven.Draven.ComboMenu;
                                case "HarassMenu":
                                    return Plugins.Draven.Draven.HarassMenu;
                                case "LaneClearMenu":
                                    return Plugins.Draven.Draven.LaneClearMenu;
                                case "AxeSettingsMenu":
                                    return Plugins.Draven.Draven.AxeSettingsMenu;
                                case "MiscMenu":
                                    return Plugins.Draven.Draven.MiscMenu;
                                case "DrawingsMenu":
                                    return Plugins.Draven.Draven.DrawingsMenu;
                                default:
                                    return MenuManager.Menu;
                            }
                        }
                        case "Ezreal":
                        {
                            switch (splitted[2])
                            {
                                case "ComboMenu":
                                    return Plugins.Ezreal.Ezreal.ComboMenu;
                                case "HarassMenu":
                                    return Plugins.Ezreal.Ezreal.HarassMenu;
                                case "LaneClearMenu":
                                    return Plugins.Ezreal.Ezreal.LaneClearMenu;
                                case "MiscMenu":
                                    return Plugins.Ezreal.Ezreal.MiscMenu;
                                case "DrawingsMenu":
                                    return Plugins.Ezreal.Ezreal.DrawingsMenu;
                                default:
                                    return MenuManager.Menu;
                            }
                        }
                        case "Graves":
                        {
                            switch (splitted[2])
                            {
                                case "ComboMenu":
                                    return Plugins.Graves.Graves.ComboMenu;
                                case "HarassMenu":
                                    return Plugins.Graves.Graves.HarassMenu;
                                case "LaneClearMenu":
                                    return Plugins.Graves.Graves.LaneClearMenu;
                                case "MiscMenu":
                                    return Plugins.Graves.Graves.MiscMenu;
                                case "DrawingsMenu":
                                    return Plugins.Graves.Graves.DrawingsMenu;
                                default:
                                    return MenuManager.Menu;
                            }
                        }
                        case "Jhin":
                        {
                            switch (splitted[2])
                            {
                                case "ComboMenu":
                                    return Plugins.Jhin.Jhin.ComboMenu;
                                case "HarassMenu":
                                    return Plugins.Jhin.Jhin.HarassMenu;
                                case "LaneClearMenu":
                                    return Plugins.Jhin.Jhin.LaneClearMenu;
                                case "MiscMenu":
                                    return Plugins.Jhin.Jhin.MiscMenu;
                                case "DrawingsMenu":
                                    return Plugins.Jhin.Jhin.DrawingsMenu;
                                default:
                                    return MenuManager.Menu;
                            }
                        }
                        case "Jinx":
                        {
                            switch (splitted[2])
                            {
                                case "ComboMenu":
                                    return Plugins.Jinx.Jinx.ComboMenu;
                                case "HarassMenu":
                                    return Plugins.Jinx.Jinx.HarassMenu;
                                case "LaneClearMenu":
                                    return Plugins.Jinx.Jinx.LaneClearMenu;
                                case "MiscMenu":
                                    return Plugins.Jinx.Jinx.MiscMenu;
                                case "DrawingsMenu":
                                    return Plugins.Jinx.Jinx.DrawingsMenu;
                                default:
                                    return MenuManager.Menu;
                            }
                        }
                        case "Kalista":
                        {
                            switch (splitted[2])
                            {
                                case "ComboMenu":
                                    return Plugins.Kalista.Kalista.ComboMenu;
                                case "HarassMenu":
                                    return Plugins.Kalista.Kalista.HarassMenu;
                                case "JungleLaneClearMenu":
                                    return Plugins.Kalista.Kalista.JungleLaneClearMenu;
                                case "FleeMenu":
                                    return Plugins.Kalista.Kalista.FleeMenu;
                                case "MiscMenu":
                                    return Plugins.Kalista.Kalista.MiscMenu;
                                case "DrawingsMenu":
                                    return Plugins.Kalista.Kalista.DrawingsMenu;
                                default:
                                    return MenuManager.Menu;
                            }
                        }
                        case "KogMaw":
                        {
                            switch (splitted[2])
                            {
                                case "ComboMenu":
                                    return Plugins.KogMaw.KogMaw.ComboMenu;
                                case "HarassMenu":
                                    return Plugins.KogMaw.KogMaw.HarassMenu;
                                case "FarmingMenu":
                                    return Plugins.KogMaw.KogMaw.FarmingMenu;
                                case "DrawingsMenu":
                                    return Plugins.KogMaw.KogMaw.DrawingsMenu;
                                default:
                                    return MenuManager.Menu;
                            }
                        }
                        case "Lucian":
                        {
                            switch (splitted[2])
                            {
                                case "ComboMenu":
                                    return Plugins.Lucian.Lucian.ComboMenu;
                                case "HarassMenu":
                                    return Plugins.Lucian.Lucian.HarassMenu;
                                case "LaneClearMenu":
                                    return Plugins.Lucian.Lucian.LaneClearMenu;
                                case "MiscMenu":
                                    return Plugins.Lucian.Lucian.MiscMenu;
                                case "DrawingsMenu":
                                    return Plugins.Lucian.Lucian.DrawingsMenu;
                                default:
                                    return MenuManager.Menu;
                            }
                        }
                        case "MissFortune":
                        {
                            switch (splitted[2])
                            {
                                case "ComboMenu":
                                    return Plugins.MissFortune.MissFortune.ComboMenu;
                                case "HarassMenu":
                                    return Plugins.MissFortune.MissFortune.HarassMenu;
                                case "LaneClearMenu":
                                    return Plugins.MissFortune.MissFortune.LaneClearMenu;
                                case "MiscMenu":
                                    return Plugins.MissFortune.MissFortune.MiscMenu;
                                case "DrawingsMenu":
                                    return Plugins.MissFortune.MissFortune.DrawingsMenu;
                                default:
                                    return MenuManager.Menu;
                            }
                        }
                        case "Quinn":
                        {
                            switch (splitted[2])
                            {
                                case "ComboMenu":
                                    return Plugins.Quinn.Quinn.ComboMenu;
                                case "HarassMenu":
                                    return Plugins.Quinn.Quinn.HarassMenu;
                                case "LaneClearMenu":
                                    return Plugins.Quinn.Quinn.LaneClearMenu;
                                case "MiscMenu":
                                    return Plugins.Quinn.Quinn.MiscMenu;
                                case "DrawingsMenu":
                                    return Plugins.Quinn.Quinn.DrawingsMenu;
                                default:
                                    return MenuManager.Menu;
                            }
                        }
                        case "Sivir":
                        {
                            switch (splitted[2])
                            {
                                case "ComboMenu":
                                    return Plugins.Sivir.Sivir.ComboMenu;
                                case "HarassMenu":
                                    return Plugins.Sivir.Sivir.HarassMenu;
                                case "LaneClearMenu":
                                    return Plugins.Sivir.Sivir.LaneClearMenu;
                                case "SpellBlockerMenu":
                                    return Plugins.Sivir.Sivir.SpellBlockerMenu;
                                case "DrawingsMenu":
                                    return Plugins.Sivir.Sivir.DrawingsMenu;
                                default:
                                    return MenuManager.Menu;
                            }
                        }
                        case "Tristana":
                        {
                            switch (splitted[2])
                            {
                                case "ComboMenu":
                                    return Plugins.Tristana.Tristana.ComboMenu;
                                case "LaneClearMenu":
                                    return Plugins.Tristana.Tristana.LaneClearMenu;
                                case "DrawingsMenu":
                                    return Plugins.Tristana.Tristana.DrawingsMenu;
                                default:
                                    return MenuManager.Menu;
                            }
                        }
                        case "Twitch":
                        {
                            switch (splitted[2])
                            {
                                case "ComboMenu":
                                    return Plugins.Twitch.Twitch.ComboMenu;
                                case "HarassMenu":
                                    return Plugins.Twitch.Twitch.HarassMenu;
                                case "LaneClearMenu":
                                    return Plugins.Twitch.Twitch.LaneClearMenu;
                                case "JungleClearMenu":
                                    return Plugins.Twitch.Twitch.JungleClearMenu;
                                case "MiscMenu":
                                    return Plugins.Twitch.Twitch.MiscMenu;
                                case "DrawingsMenu":
                                    return Plugins.Twitch.Twitch.DrawingsMenu;
                                default:
                                    return MenuManager.Menu;
                            }
                        }
                        case "Urgot":
                        {
                            switch (splitted[2])
                            {
                                case "ComboMenu":
                                    return Plugins.Urgot.Urgot.ComboMenu;
                                case "HarassMenu":
                                    return Plugins.Urgot.Urgot.HarassMenu;
                                case "LaneClearMenu":
                                    return Plugins.Urgot.Urgot.LaneClearMenu;
                                case "MiscMenu":
                                    return Plugins.Urgot.Urgot.MiscMenu;
                                case "DrawingsMenu":
                                    return Plugins.Urgot.Urgot.DrawingsMenu;
                                default:
                                    return MenuManager.Menu;
                            }
                        }
                        case "Varus":
                        {
                            switch (splitted[2])
                            {
                                case "ComboMenu":
                                    return Plugins.Varus.Varus.ComboMenu;
                                case "HarassMenu":
                                    return Plugins.Varus.Varus.HarassMenu;
                                case "LaneClearMenu":
                                    return Plugins.Varus.Varus.LaneClearMenu;
                                case "MiscMenu":
                                    return Plugins.Varus.Varus.MiscMenu;
                                case "DrawingsMenu":
                                    return Plugins.Varus.Varus.DrawingsMenu;
                                default:
                                    return MenuManager.Menu;
                            }
                        }
                        case "Vayne":
                        {
                            switch (splitted[2])
                            {
                                case "ComboMenu":
                                    return Plugins.Vayne.Vayne.ComboMenu;
                                case "HarassMenu":
                                    return Plugins.Vayne.Vayne.HarassMenu;
                                case "LaneClearMenu":
                                    return Plugins.Vayne.Vayne.LaneClearMenu;
                                case "MiscMenu":
                                    return Plugins.Vayne.Vayne.MiscMenu;
                                case "DrawingsMenu":
                                    return Plugins.Vayne.Vayne.DrawingsMenu;
                                default:
                                    return MenuManager.Menu;
                            }
                        }
                    }
                    break;
            }
            return null;
        }
    }
}