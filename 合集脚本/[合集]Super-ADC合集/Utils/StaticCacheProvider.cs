#region Licensing
// ---------------------------------------------------------------------
// <copyright file="StaticCacheProvider.cs" company="EloBuddy">
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
using EloBuddy.SDK.Menu.Values;
using Marksman_Master.Cache.Modules;
using SharpDX;

namespace Marksman_Master.Utils
{
    internal static class StaticCacheProvider
    {
        internal static Cache.Cache Cache { get; private set; }

        private static Distance CachedDistance { get; set; }
        private static IsValidTarget CachedIsValidTarget { get; set; }
        private static IsInRange CachedIsInRange { get; set; }
        internal static MinionCache MinionCache { get; set; }

        private static CustomCache<KeyValuePair<int, float>, int> CountEnemiesInRange { get; set; }
        private static CustomCache<KeyValuePair<int, float>, int> CountAlliesInRange { get; set; }
        private static CustomCache<KeyValuePair<int, float>, int> CountEnemyMinionsInRange { get; set; }
        private static CustomCache<KeyValuePair<Vector3, float>, int> CountEnemiesInRange2 { get; set; }
        private static CustomCache<KeyValuePair<Vector3, float>, int> CountAlliesInRange2 { get; set; }
        private static CustomCache<KeyValuePair<Vector3, float>, int> CountEnemyMinionsInRange2 { get; set; }

        private static CustomCache<Tuple<int, int, bool>, float> CachedAutoAttackDamage { get; set; }
        private static CustomCache<Tuple<int, int, SpellSlot>, float> CachedSpellDamage { get; set; }

        private static bool _initialized;

        internal static void Initialize()
        {
            if (_initialized)
                return;

            Cache = new Cache.Cache();

            CachedDistance = Cache.Resolve<Distance>();
            CachedIsValidTarget = Cache.Resolve<IsValidTarget>();
            CachedIsInRange = Cache.Resolve<IsInRange>();
            MinionCache = Cache.Resolve<MinionCache>();

            CountEnemiesInRange = Cache.Resolve<CustomCache<KeyValuePair<int, float>, int>>();
            CountAlliesInRange = Cache.Resolve<CustomCache<KeyValuePair<int, float>, int>>();
            CountEnemyMinionsInRange = Cache.Resolve<CustomCache<KeyValuePair<int, float>, int>>();
            CountEnemiesInRange2 = Cache.Resolve<CustomCache<KeyValuePair<Vector3, float>, int>>();
            CountAlliesInRange2 = Cache.Resolve<CustomCache<KeyValuePair<Vector3, float>, int>>();
            CountEnemyMinionsInRange2 = Cache.Resolve<CustomCache<KeyValuePair<Vector3, float>, int>>();
            CachedAutoAttackDamage = Cache.Resolve<CustomCache<Tuple<int, int, bool>, float>>();
            CachedAutoAttackDamage.RefreshRate = 1000;
            CachedSpellDamage = Cache.Resolve<CustomCache<Tuple<int, int, SpellSlot>, float>>();
            CachedSpellDamage.RefreshRate = 1000;

            _initialized = true;
        }

        internal static void Dispose()
        {
            Cache.Dispose();
        }
       
        internal static IEnumerable<AIHeroClient> GetChampions(CachedEntityType type,
            Func<AIHeroClient, bool> predicate = null)
        {
            if (type > CachedEntityType.AllyHero)
            {
                throw new Exception($"Invalid type passed. {type} is not supported by AIHeroClient.");
            }

            switch (type)
            {
                case CachedEntityType.EnemyHero:
                {
                    return predicate != null
                        ? EntityManager.Heroes.Enemies.Where(predicate)
                        : EntityManager.Heroes.Enemies;
                }
                case CachedEntityType.AllyHero:
                {
                    return predicate != null
                        ? EntityManager.Heroes.Allies.Where(predicate)
                        : EntityManager.Heroes.Allies;
                }
                case CachedEntityType.AllHeroes:
                {
                    return predicate != null
                        ? EntityManager.Heroes.AllHeroes.Where(predicate)
                        : EntityManager.Heroes.AllHeroes;
                }
            }
            return null;
        }

        internal static IEnumerable<Obj_AI_Minion> GetMinions(CachedEntityType type,
            Func<Obj_AI_Minion, bool> predicate = null)
        {
            if (type < CachedEntityType.EnemyMinion)
            {
                throw new Exception($"Invalid type passed. {type} is not supported by Obj_AI_Minion.");
            }

            MinionCache.RefreshRate =
                MenuManager.CacheMenu["MenuManager.ExtensionsMenu.MinionCacheRefreshRate"].Cast<Slider>().CurrentValue;

            switch (type)
            {
                case CachedEntityType.EnemyMinion:
                    {
                        if (!MenuManager.IsCacheEnabled)
                            return predicate != null
                                ? EntityManager.MinionsAndMonsters.EnemyMinions.Where(predicate)
                                : EntityManager.MinionsAndMonsters.EnemyMinions;

                        return (predicate != null
                            ? MinionCache.GetMinions(type, predicate)
                            : MinionCache.GetMinions(type)) ?? EntityManager.MinionsAndMonsters.EnemyMinions;
                    }
                case CachedEntityType.AllyMinion:
                    {
                        if (!MenuManager.IsCacheEnabled)
                            return predicate != null
                                ? EntityManager.MinionsAndMonsters.AlliedMinions.Where(predicate)
                                : EntityManager.MinionsAndMonsters.AlliedMinions;

                        return (predicate != null
                            ? MinionCache.GetMinions(type, predicate)
                            : MinionCache.GetMinions(type)) ?? EntityManager.MinionsAndMonsters.AlliedMinions;
                    }
                case CachedEntityType.Minions:
                    {
                        if (!MenuManager.IsCacheEnabled)
                            return predicate != null
                                ? EntityManager.MinionsAndMonsters.Minions.Where(predicate)
                                : EntityManager.MinionsAndMonsters.Minions;

                        return (predicate != null
                            ? MinionCache.GetMinions(type, predicate)
                            : MinionCache.GetMinions(type)) ?? EntityManager.MinionsAndMonsters.Minions;
                    }
                case CachedEntityType.CombinedAttackableMinions:
                    {
                        if (!MenuManager.IsCacheEnabled)
                            return predicate != null
                                ? EntityManager.MinionsAndMonsters.CombinedAttackable.Where(predicate)
                                : EntityManager.MinionsAndMonsters.CombinedAttackable;

                        return (predicate != null
                            ? MinionCache.GetMinions(type, predicate)
                            : MinionCache.GetMinions(type)) ?? EntityManager.MinionsAndMonsters.CombinedAttackable;
                    }
                case CachedEntityType.CombinedMinions:
                    {
                        if (!MenuManager.IsCacheEnabled)
                            return predicate != null
                                ? EntityManager.MinionsAndMonsters.Combined.Where(predicate)
                                : EntityManager.MinionsAndMonsters.Combined;

                        return (predicate != null
                            ? MinionCache.GetMinions(type, predicate)
                            : MinionCache.GetMinions(type)) ?? EntityManager.MinionsAndMonsters.Combined;
                    }
                case CachedEntityType.Monsters:
                    {
                        if (!MenuManager.IsCacheEnabled)
                            return predicate != null
                                ? EntityManager.MinionsAndMonsters.Monsters.Where(predicate)
                                : EntityManager.MinionsAndMonsters.Monsters;

                        return (predicate != null
                            ? MinionCache.GetMinions(type, predicate)
                            : MinionCache.GetMinions(type)) ?? EntityManager.MinionsAndMonsters.Monsters;
                    }
            }
            return null;
        }

        internal static float GetSpellDamageCached(this AIHeroClient from, Obj_AI_Base target, SpellSlot spellSlot)
        {
            if (!MenuManager.IsCacheEnabled)
                return from.GetSpellDamage(target, spellSlot);

            if (CachedSpellDamage.Exist(new Tuple<int, int, SpellSlot>(from.NetworkId, target.NetworkId, spellSlot)))
            {
                return CachedSpellDamage.Get(new Tuple<int, int, SpellSlot>(from.NetworkId, target.NetworkId, spellSlot));
            }

            CachedSpellDamage.Add(new Tuple<int, int, SpellSlot>(from.NetworkId, target.NetworkId, spellSlot),
                from.GetSpellDamage(target, spellSlot));

            return CachedSpellDamage.Get(new Tuple<int, int, SpellSlot>(from.NetworkId, target.NetworkId, spellSlot));
        }


        internal static float GetAutoAttackDamageCached(this Obj_AI_Base from, Obj_AI_Base target,
            bool respectPassives = false)
        {
            if (!MenuManager.IsCacheEnabled)
                return from.GetAutoAttackDamage(target, respectPassives);

            if (
                CachedAutoAttackDamage.Exist(new Tuple<int, int, bool>(from.NetworkId, target.NetworkId, respectPassives)))
            {
                return
                    CachedAutoAttackDamage.Get(new Tuple<int, int, bool>(from.NetworkId, target.NetworkId,
                        respectPassives));
            }

            CachedAutoAttackDamage.Add(new Tuple<int, int, bool>(from.NetworkId, target.NetworkId, respectPassives),
                from.GetAutoAttackDamage(target, respectPassives));

            return
                CachedAutoAttackDamage.Get(new Tuple<int, int, bool>(from.NetworkId, target.NetworkId, respectPassives));
        }

        internal static int CountEnemiesInRangeCached(this GameObject from, float range)
        {
            if (!MenuManager.IsCacheEnabled)
                return from.CountEnemiesInRange(range);

            if (CountEnemiesInRange.Exist(new KeyValuePair<int, float>(from.NetworkId, range)))
            {
                return CountEnemiesInRange.Get(new KeyValuePair<int, float>(from.NetworkId, range));
            }
            CountEnemiesInRange.Add(new KeyValuePair<int, float>(from.NetworkId, range), from.CountEnemiesInRange(range));

            return CountEnemiesInRange.Get(new KeyValuePair<int, float>(from.NetworkId, range));
        }

        internal static int CountAlliesInRangeCached(this GameObject from, float range)
        {
            if (!MenuManager.IsCacheEnabled)
                return from.CountAlliesInRange(range);

            if (CountAlliesInRange.Exist(new KeyValuePair<int, float>(from.NetworkId, range)))
            {
                return CountAlliesInRange.Get(new KeyValuePair<int, float>(from.NetworkId, range));
            }

            CountAlliesInRange.Add(new KeyValuePair<int, float>(from.NetworkId, range), from.CountAlliesInRange(range));

            return CountAlliesInRange.Get(new KeyValuePair<int, float>(from.NetworkId, range));
        }

        internal static int CountEnemyMinionsInRangeCached(this GameObject from, float range)
        {
            if (!MenuManager.IsCacheEnabled)
                return from.CountEnemyMinionsInRange(range);

            if (CountEnemyMinionsInRange.Exist(new KeyValuePair<int, float>(from.NetworkId, range)))
            {
                return CountEnemyMinionsInRange.Get(new KeyValuePair<int, float>(from.NetworkId, range));
            }

            CountEnemyMinionsInRange.Add(new KeyValuePair<int, float>(from.NetworkId, range),
                from.CountEnemyMinionsInRange(range));

            return CountEnemyMinionsInRange.Get(new KeyValuePair<int, float>(from.NetworkId, range));
        }

        internal static int CountEnemiesInRangeCached(this Vector3 from, float range)
        {
            if (!MenuManager.IsCacheEnabled)
                return from.CountEnemiesInRange(range);

            if (CountEnemiesInRange2.Exist(new KeyValuePair<Vector3, float>(from, range)))
            {
                return CountEnemiesInRange2.Get(new KeyValuePair<Vector3, float>(from, range));
            }

            CountEnemiesInRange2.Add(new KeyValuePair<Vector3, float>(from, range), from.CountEnemiesInRange(range));

            return CountEnemiesInRange2.Get(new KeyValuePair<Vector3, float>(from, range));
        }

        internal static int CountAlliesInRangeCached(this Vector3 from, float range)
        {
            if (!MenuManager.IsCacheEnabled)
                return from.CountAlliesInRange(range);

            if (CountAlliesInRange2.Exist(new KeyValuePair<Vector3, float>(from, range)))
            {
                return CountAlliesInRange2.Get(new KeyValuePair<Vector3, float>(from, range));
            }

            CountAlliesInRange2.Add(new KeyValuePair<Vector3, float>(from, range), from.CountAlliesInRange(range));

            return CountAlliesInRange2.Get(new KeyValuePair<Vector3, float>(from, range));
        }

        internal static int CountEnemyMinionsInRangeCached(this Vector3 from, float range)
        {
            if (!MenuManager.IsCacheEnabled)
                return from.CountEnemyMinionsInRange(range);

            if (CountEnemyMinionsInRange2.Exist(new KeyValuePair<Vector3, float>(from, range)))
            {
                return CountEnemyMinionsInRange2.Get(new KeyValuePair<Vector3, float>(from, range));
            }

            CountEnemyMinionsInRange2.Add(new KeyValuePair<Vector3, float>(from, range),
                from.CountEnemyMinionsInRange(range));

            return CountEnemyMinionsInRange2.Get(new KeyValuePair<Vector3, float>(from, range));
        }


        internal static int CountEnemiesInRangeCached(this Vector2 from, float range)
        {
            if (!MenuManager.IsCacheEnabled)
                return from.CountEnemiesInRange(range);

            if (CountEnemiesInRange2.Exist(new KeyValuePair<Vector3, float>(from.To3D(), range)))
            {
                return CountEnemiesInRange2.Get(new KeyValuePair<Vector3, float>(from.To3D(), range));
            }

            CountEnemiesInRange2.Add(new KeyValuePair<Vector3, float>(from.To3D(), range),
                from.CountEnemiesInRange(range));

            return CountEnemiesInRange2.Get(new KeyValuePair<Vector3, float>(from.To3D(), range));
        }

        internal static int CountAlliesInRangeCached(this Vector2 from, float range)
        {
            if (!MenuManager.IsCacheEnabled)
                return from.CountAlliesInRange(range);

            if (CountAlliesInRange2.Exist(new KeyValuePair<Vector3, float>(from.To3D(), range)))
            {
                return CountAlliesInRange2.Get(new KeyValuePair<Vector3, float>(from.To3D(), range));
            }

            CountAlliesInRange2.Add(new KeyValuePair<Vector3, float>(from.To3D(), range), from.CountAlliesInRange(range));

            return CountAlliesInRange2.Get(new KeyValuePair<Vector3, float>(from.To3D(), range));
        }

        internal static int CountEnemyMinionsInRangeCached(this Vector2 from, float range)
        {
            if (!MenuManager.IsCacheEnabled)
                return from.CountEnemyMinionsInRange(range);

            if (CountEnemyMinionsInRange2.Exist(new KeyValuePair<Vector3, float>(from.To3D(), range)))
            {
                return CountEnemyMinionsInRange2.Get(new KeyValuePair<Vector3, float>(from.To3D(), range));
            }

            CountEnemyMinionsInRange2.Add(new KeyValuePair<Vector3, float>(from.To3D(), range),
                from.CountEnemyMinionsInRange(range));

            return CountEnemyMinionsInRange2.Get(new KeyValuePair<Vector3, float>(from.To3D(), range));
        }

        internal static bool IsInRangeCached(this Vector2 from, Vector2 target, float range)
        {
            return from.To3D().IsInRangeCached(target.To3D(), range);
        }

        internal static bool IsInRangeCached(this Vector2 from, Vector3 target, float range)
        {
            return from.To3D().IsInRangeCached(target, range);
        }

        internal static bool IsInRangeCached(this Vector2 from, GameObject target, float range)
        {
            return from.To3D().IsInRangeCached(target.Position, range);
        }

        internal static bool IsInRangeCached(this Vector3 from, Vector3 target, float range)
        {
            return MenuManager.IsCacheEnabled
                ? CachedIsInRange.Get(from, target, range)
                : from.IsInRange(target, range);
        }

        internal static bool IsInRangeCached(this Vector3 from, Vector2 target, float range)
        {
            return MenuManager.IsCacheEnabled
                ? CachedIsInRange.Get(from, target.To3D(), range)
                : from.IsInRange(target, range);
        }

        internal static bool IsInRangeCached(this Vector3 from, GameObject target, float range)
        {
            return MenuManager.IsCacheEnabled
                ? CachedIsInRange.Get(from, target.Position, range)
                : from.IsInRange(target, range);
        }

        internal static bool IsInRangeCached(this GameObject from, GameObject target, float range)
        {
            return MenuManager.IsCacheEnabled
                ? CachedIsInRange.Get(from, target, range)
                : from.IsInRange(target, range);
        }

        internal static float DistanceCached(this Vector2 from, Vector3 target)
        {
            return MenuManager.IsCacheEnabled ? CachedDistance.Get(from.To3D(), target) : from.Distance(target);
        }

        internal static float DistanceCached(this Vector2 from, Vector2 target)
        {
            return MenuManager.IsCacheEnabled ? CachedDistance.Get(from.To3D(), target.To3D()) : from.Distance(target);
        }

        internal static float DistanceCached(this Vector2 from, GameObject target)
        {
            return MenuManager.IsCacheEnabled
                ? CachedDistance.Get(from.To3D(), target.Position)
                : from.Distance(target);
        }

        internal static float DistanceCached(this Vector3 from, Vector3 target)
        {
            return MenuManager.IsCacheEnabled ? CachedDistance.Get(from, target) : from.Distance(target);
        }

        internal static float DistanceCached(this Vector3 from, Vector2 target)
        {
            return MenuManager.IsCacheEnabled ? CachedDistance.Get(from, target.To3D()) : from.Distance(target);
        }

        internal static float DistanceCached(this Vector3 from, GameObject target)
        {
            return MenuManager.IsCacheEnabled ? CachedDistance.Get(from, target.Position) : from.Distance(target);
        }

        internal static float DistanceCached(this GameObject from, GameObject target)
        {
            return MenuManager.IsCacheEnabled ? CachedDistance.Get(from, target) : from.Distance(target);
        }

        internal static float DistanceCached(this GameObject from, Vector3 target)
        {
            return MenuManager.IsCacheEnabled ? CachedDistance.Get(from.Position, target) : from.Distance(target);
        }

        internal static float DistanceCached(this GameObject from, Vector2 target)
        {
            return MenuManager.IsCacheEnabled
                ? CachedDistance.Get(from.Position, target.To3D())
                : from.Distance(target);
        }

        internal static bool IsValidTargetCached(this AttackableUnit from, float? range = null)
        {
            return MenuManager.IsCacheEnabled
                ? CachedIsValidTarget.Get(from, range ?? 999999)
                : from.IsValidTarget(range);
        }
    }
}