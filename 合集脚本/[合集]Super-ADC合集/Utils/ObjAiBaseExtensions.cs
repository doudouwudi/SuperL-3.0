#region Licensing
// ---------------------------------------------------------------------
// <copyright file="ObjAiBaseExtensions.cs" company="EloBuddy">
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
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;

namespace Marksman_Master.Utils
{
    using SharpDX;

    internal static class ObjAiBaseExtensions
    {
        public static bool IsUsingHealingPotion(this Obj_AI_Base unit)
        {
            return unit.HasBuff("ItemMiniRegenPotion") || unit.HasBuff("ItemCrystalFlask") ||
                   unit.HasBuff("ItemCrystalFlaskJungle") || unit.HasBuff("ItemDarkCrystalFlask") || unit.HasBuff("Health Potion");
        }

        public static bool HasUndyingBuffA(this AIHeroClient target)
        {
            if (target.Buffs.Any(b => b.IsValid &&
                                      (b.Name.Equals("ChronoShift", StringComparison.CurrentCultureIgnoreCase) ||
                                       b.Name.Equals("FioraW", StringComparison.CurrentCultureIgnoreCase) ||
                                       b.Name.Equals("TaricR", StringComparison.CurrentCultureIgnoreCase) ||
                                       b.Name.Equals("BardRStasis", StringComparison.CurrentCultureIgnoreCase) ||
                                       b.Name.Equals("JudicatorIntervention", StringComparison.CurrentCultureIgnoreCase) ||
                                       b.Name.Equals("UndyingRage", StringComparison.CurrentCultureIgnoreCase) ||
                                       (b.Name.Equals("kindredrnodeathbuff", StringComparison.CurrentCultureIgnoreCase) &&
                                        (target.HealthPercent <= 10)))))
            {
                return true;
            }

            if (target.ChampionName != "Poppy")
                return target.IsInvulnerable;

            return EntityManager.Heroes.Allies.Any(
                o => !o.IsMe && o.Buffs.Any(b => (b.Caster.NetworkId == target.NetworkId) && b.IsValid &&
                                                 b.DisplayName.Equals("PoppyDITarget", StringComparison.CurrentCultureIgnoreCase))) || target.IsInvulnerable;
        }

        internal static Vector3 GetPathingDirection(this Obj_AI_Base source)
        {
            var output = ChampionTracker.GetPathingDirection(source.NetworkId);

            return output == default(Vector3) ? source.ServerPosition : source.ServerPosition.Extend(output, 100).To3D();
        }

        internal static Vector3 GetLastPath(this Obj_AI_Base source)
        {
            var output = ChampionTracker.GetLastPath(source.NetworkId);

            return output == default(Vector3) ? source.ServerPosition : output;
        }

        internal static bool IsMovingTowards(this Obj_AI_Base source, Obj_AI_Base target, int minDistance = 0)
        {
            var safetyDistance = minDistance == 0 ? target.GetAutoAttackRange() : minDistance;

            if (source.DistanceCached(target) < safetyDistance)
                return true;

            if (!source.IsMoving || (source.Distance(source.RealPath().Last()) < 10))
                return false;

            return source.IsFacingB(target) && (source.GetLastPath().DistanceSquared(target.Position) < safetyDistance * safetyDistance);
        }

        public static bool IsFacingB(this Obj_AI_Base source, Obj_AI_Base target)
        {
            if ((source == null) || (target == null))
            {
                return false;
            }

            return source.IsFacingB(target.Position);
        }

        public static bool IsFacingB(this Obj_AI_Base source, Vector3 target)
        {
            if ((source == null) || (target == default(Vector3)))
            {
                return false;
            }

            var direction = source.GetPathingDirection() - source.Position;
            var dotProduct = direction.To2D().Normalized().DotProduct((target - source.Position).To2D().Normalized());
            
            return dotProduct > 0.65;
        }
        
        public static bool HasSpellShield(this Obj_AI_Base target)
        {
            return target.HasBuffOfType(BuffType.SpellShield) || target.HasBuffOfType(BuffType.SpellImmunity);
        }

        public static float TotalHealthWithShields(this Obj_AI_Base target, bool includeMagicShields = false)
        {
            return target.Health + target.AllShield + target.AttackShield + (includeMagicShields ? target.MagicShield : 0);
        }

        public static bool HasSheenBuff(this AIHeroClient unit)
        {
            return
                unit.Buffs.Any(
                    b =>
                        b.IsActive &&
                        (b.DisplayName.Equals("sheen", StringComparison.CurrentCultureIgnoreCase) ||
                         b.DisplayName.Equals("itemfrozenfist", StringComparison.CurrentCultureIgnoreCase)));
        }

        public static bool IsImmobile(this AIHeroClient target)
        {
            return !target.IsRecalling() && !target.HasBuffOfType(BuffType.Stun) && !target.HasBuffOfType(BuffType.Snare) && !target.HasBuffOfType(BuffType.Knockup) && !target.HasBuffOfType(BuffType.Knockback) && !target.HasBuffOfType(BuffType.Flee) && !target.HasBuffOfType(BuffType.Fear) && !target.HasBuffOfType(BuffType.Charm) && !target.HasBuffOfType(BuffType.Suppression) && !target.HasBuffOfType(BuffType.Taunt);
        }
    }
}