#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Enumerators.cs" company="EloBuddy">
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
using System;

namespace Marksman_Master.Utils
{
    public enum CachedEntityType
    {
        EnemyHero = 0,
        AllHeroes,
        AllyHero,
        EnemyMinion,
        AllyMinion,
        CombinedMinions,
        CombinedAttackableMinions,
        Minions,
        Monsters
    }

    public enum ItemType
    {
        Defensive,
        Offensive,
        Cleanse,
        Potion
    }

    public enum ItemTargettingType
    {
        Self,
        Unit,
        None
    }

    public enum ItemUsageWhen
    {
        Always,
        AfterAttack,
        ComboMode,
    }

    public enum ItemsEnum
    {
        HealthPotion,
        RefillablePotion,
        HuntersPotion,
        CorruptingPotion,
        ElixirofIron,
        ElixirofSorcery,
        ElixirofWrath,
        Scimitar,
        Quicksilver,
        Ghostblade,
        Cutlass,
        Gunblade,
        BladeOfTheRuinedKing
    }

    public enum ItemIds
    {
        HealthPotion = 2003,
        Biscuit = 2010,
        RefillablePotion = 2031,
        HuntersPotion = 2032,
        CorruptingPotion = 2033,
        ElixirofIron = 2138,
        ElixirofSorcery = 2139,
        ElixirofWrath = 2140,
        Scimitar = 3139,
        Quicksilver = 3140,
        Ghostblade = 3142,
        Cutlass = 3144,
        Gunblade = 3146,
        BladeOfTheRuinedKing = 3153
    }

    [Flags]
    public enum Summoner
    {
        Unknown = 1 << 0,
        Barrier = 1 << 1,
        Heal = 1 << 2,
        Ignite = 1 << 3,
        Exhaust= 1 << 4,
        Smite = 1 << 5,
        ChillingSmite = 1 << 6,
        ChallengingSmite = 1 << 7
    }

    public enum GapcloserTypes
    {
        Targeted,
        Skillshot
    }

    [Flags]
    public enum ChampionTrackerFlags
    {
        VisibilityTracker = 1 << 0,
        LongCastTimeTracker = 1 << 1,
        PostBasicAttackTracker = 1 << 2,
        PathingTracker = 1 << 3
    }

    [Flags]
    public enum RectangleDrawingFlags
    {
        Top = 1 << 0,
        Bottom = 1 << 1,
        Side = 1 << 2,
        All = 1 << 3,
        Fill = 1 << 4
    }
}