#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Botrk.cs" company="EloBuddy">
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
namespace Marksman_Master.Activator.Items
{
    using System.Linq;

    using EloBuddy;

    using Utils;

    internal sealed class Botrk : Item
    {
        public Botrk()
        {
            ItemName = "BladeOfTheRuinedKing";
            ItemTargettingType = ItemTargettingType.Unit;
            ItemId = ItemIds.BladeOfTheRuinedKing;
            ItemType = ItemType.Offensive;
            ItemUsageWhen = ItemUsageWhen.Always;
            Range = 550;
        }

        internal sealed class Settings
        {
            public static bool IsEnabled => MenuManager.MenuValues["Activator.ItemsMenu.BladeOfTheRuinedKing"];
            public static int MyMinHp => MenuManager.MenuValues["Activator.ItemsMenu.BladeOfTheRuinedKing.MyMinHP", true];
            public static int TargetsMinHp => MenuManager.MenuValues["Activator.ItemsMenu.BladeOfTheRuinedKing.TargetsMinHP", true];
            public static int IfEnemiesNear => MenuManager.MenuValues["Activator.ItemsMenu.BladeOfTheRuinedKing.IfEnemiesNear", true];
        }

        public static bool IsOwned { get; } =
            Player.Instance.InventoryItems.Any(x => x.Id == EloBuddy.ItemId.Blade_of_the_Ruined_King);
    }
}