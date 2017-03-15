#region Licensing
// ---------------------------------------------------------------------
// <copyright file="TearStacker.cs" company="EloBuddy">
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
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;

namespace Marksman_Master.Utils
{
    internal class TearStacker
    {
        /// <summary>
        /// Returns tear's SpellSlot
        /// </summary>
        public static SpellSlot GetTearSpellSlot
        {
            get
            {
                if (Player.Instance.GetSpellSlotFromName("ArchAngelsDummySpell") != SpellSlot.Unknown)
                    return
                        Player.Instance.Spellbook.GetSpell(Player.Instance.GetSpellSlotFromName("ArchAngelsDummySpell"))
                            .Slot;

                if (Player.Instance.GetSpellSlotFromName("ManamuneDummySpell") != SpellSlot.Unknown)
                    return
                        Player.Instance.Spellbook.GetSpell(Player.Instance.GetSpellSlotFromName("ManamuneDummySpell"))
                            .Slot;

                return Player.Instance.GetSpellSlotFromName("TearsDummySpell") != SpellSlot.Unknown
                    ? Player.Instance.Spellbook.GetSpell(Player.Instance.GetSpellSlotFromName("TearsDummySpell")).Slot
                    : SpellSlot.Unknown;
            }
        }

        /// <summary>
        /// Returns true if tear is ready
        /// </summary>
        public static bool IsTearReady
        {
            get
            {
                if (GetTearSpellSlot != SpellSlot.Unknown)
                {
                    return Player.Instance.Spellbook.GetSpell(GetTearSpellSlot).State == SpellState.Surpressed;
                }
                return false;
            }
        }

        /// <summary>
        /// Returns true if tear is fully stacked
        /// </summary>
        public static bool IsTearStacked
        {
            get
            {
                if (GetTearSpellSlot != SpellSlot.Unknown)
                {
                    return Player.Instance.Spellbook.GetSpell(GetTearSpellSlot).CooldownExpires + 120 > Game.Time;
                }
                return true;
            }
        }

        /// <summary>
        /// Enables tear stacker
        /// </summary>
        public static bool Enabled { get; set; } = true;

        /// <summary>
        /// Tear stacker enabled only while being near fountain
        /// </summary>
        public static bool OnlyInFountain { get; set; } = false;

        /// <summary>
        /// Minimum mana percent to enable tear stacker. Default set to 0.
        /// </summary>
        public static int MinimumManaPercent { get; set; } = 0;

        /// <summary>
        /// Spells dictionary
        /// </summary>
        public static Dictionary<SpellSlot, float> Spells { get;  private set; }

        /// <summary>
        /// Custom conditions
        /// </summary>
        public static Func<bool> CustomConditions { get; private set; }

        /// <summary>
        /// Delay between each tick
        /// </summary>
        public static int TickDelay { get; set; } = 1000;
        
        private static int _lastTick;
        private static readonly Dictionary<SpellSlot, float> LastCastTime = new Dictionary<SpellSlot, float>();
        
        /// <summary>
        /// Static initializer of TearStacker class
        /// </summary>
        /// <param name="spell">Spells Dictionary where key is their spellslot and value is their delay between casts</param>
        /// <param name="customConditions">Custom conditions</param>
        public static void Initializer(Dictionary<SpellSlot, float> spell, Func<bool> customConditions)
        {
            Spells = spell;

            foreach (var f in spell)
            {
                LastCastTime.Add(f.Key, 0);
            }

            CustomConditions = customConditions;

            Game.OnTick += Game_OnTick;
        }

        private static void Game_OnTick(EventArgs args)
        {
            if (!Enabled || Player.Instance.IsDead || Player.Instance.IsRecalling() || _lastTick + TickDelay > Game.Time*1000 || !IsTearReady || Spells == null ||
                LastCastTime == null ||
                GetTearSpellSlot == SpellSlot.Unknown || (OnlyInFountain && !Player.Instance.IsInShopRange()) ||
                Player.Instance.ManaPercent < MinimumManaPercent || !CustomConditions())
                return;
            
            foreach (var spell in Spells.Where(x=> Player.Instance.Spellbook.GetSpell(x.Key).IsReady && LastCastTime[x.Key] + x.Value < Game.Time * 1000))
            {
                if (!IsTearReady)
                    return;

                Player.Instance.Spellbook.CastSpell(spell.Key,
                    Player.Instance.Position.Extend(Game.CursorPos, new Random().Next(200, 1000)).To3D());

                LastCastTime[spell.Key] = Game.Time*1000;
            }

            _lastTick = (int) Game.Time*1000;
        }
    }
}