﻿#region Licensing
// ---------------------------------------------------------------------
// <copyright file="CorruptingPotion.cs" company="EloBuddy">
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

    internal class CorruptingPotion : Item
    {
        public CorruptingPotion()
        {
            ItemName = "CorruptingPotion";
            ItemTargettingType = ItemTargettingType.None;
            ItemId = ItemIds.CorruptingPotion;
            ItemType = ItemType.Potion;
            ItemUsageWhen = ItemUsageWhen.AfterAttack;
            Range = 550;
        }

        public static bool IsOwned { get; } =
            Player.Instance.InventoryItems.Any(x => x.Id == EloBuddy.ItemId.Corrupting_Potion);
    }
}