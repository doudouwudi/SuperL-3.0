#region Licensing
// ---------------------------------------------------------------------
// <copyright file="MinionCache.cs" company="EloBuddy">
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
using Marksman_Master.Cache.Interfaces;
using Marksman_Master.Utils;

namespace Marksman_Master.Cache.Modules
{
    internal sealed class MinionCache : CacheModuleBase, IDisposable, ICachable
    {
        internal override string Name { get; } = "CachedEntityManager";

        public bool IsDisposing { get; private set; }

        private Dictionary<CachedEntityType, float> LastScan { get; set; }
        
        private IEnumerable<Obj_AI_Minion> CachedEnemyMinions { get; set; }
        private IEnumerable<Obj_AI_Minion> CachedAllyMinions { get; set; }
        private IEnumerable<Obj_AI_Minion> CachedCombinedMinions { get; set; }
        private IEnumerable<Obj_AI_Minion> CachedCombinedAttackableMinions { get; set; }
        private IEnumerable<Obj_AI_Minion> CachedMinions { get; set; }
        private IEnumerable<Obj_AI_Minion> CachedMonsters { get; set; }

        private Dictionary<Func<Obj_AI_Minion, bool>, IEnumerable<Obj_AI_Minion>> CachedEnemyMinionsWithAction { get; set; }
        private Dictionary<Func<Obj_AI_Minion, bool>, IEnumerable<Obj_AI_Minion>> CachedAllyMinionsWithAction { get; set; }
        private Dictionary<Func<Obj_AI_Minion, bool>, IEnumerable<Obj_AI_Minion>> CachedCombinedMinionsWithAction { get; set; }
        private Dictionary<Func<Obj_AI_Minion, bool>, IEnumerable<Obj_AI_Minion>> CachedCombinedAttackableMinionsWithAction { get; set; }
        private Dictionary<Func<Obj_AI_Minion, bool>, IEnumerable<Obj_AI_Minion>> CachedMinionsWithAction { get; set; }
        private Dictionary<Func<Obj_AI_Minion, bool>, IEnumerable<Obj_AI_Minion>> CachedMonstersWithAction { get; set; }


        internal override void Load()
        {
            LastScan = new Dictionary<CachedEntityType, float>();
        }

        internal IEnumerable<Obj_AI_Minion> GetMinions(CachedEntityType type)
        {
            if (type < CachedEntityType.EnemyMinion)
            {
                throw new Exception($"Invalid type passed. {type} is not supported by Obj_AI_Minion.");
            }

            switch (type)
            {
                case CachedEntityType.EnemyMinion:
                    {
                        if (IsDisposing)
                        {
                            return EntityManager.MinionsAndMonsters.EnemyMinions;
                        }
                        float lastScan;

                        if (LastScan.TryGetValue(type, out lastScan))
                        {
                            if (Game.Time * 1000 - LastScan[type] < RefreshRate)
                            {
                                return CachedEnemyMinions;
                            }
                            CachedEnemyMinions = EntityManager.MinionsAndMonsters.EnemyMinions;
                            LastScan[type] = Game.Time * 1000;
                        }
                        else
                        {
                            CachedEnemyMinions = EntityManager.MinionsAndMonsters.EnemyMinions;
                            LastScan[type] = Game.Time * 1000;
                        }
                        return CachedEnemyMinions;
                    }
                case CachedEntityType.AllyMinion:
                    {
                        if (IsDisposing)
                        {
                            return EntityManager.MinionsAndMonsters.AlliedMinions;
                        }
                        float lastScan;

                        if (LastScan.TryGetValue(type, out lastScan))
                        {
                            if (Game.Time * 1000 - LastScan[type] < RefreshRate)
                            {
                                return CachedAllyMinions;
                            }
                            CachedAllyMinions = EntityManager.MinionsAndMonsters.AlliedMinions;
                            LastScan[type] = Game.Time * 1000;
                        }
                        else
                        {
                            CachedAllyMinions = EntityManager.MinionsAndMonsters.AlliedMinions;
                            LastScan[type] = Game.Time * 1000;
                        }
                        return CachedAllyMinions;
                    }
                case CachedEntityType.Minions:
                    {
                        if (IsDisposing)
                        {
                            return EntityManager.MinionsAndMonsters.Minions;
                        }

                        float lastScan;

                        if (LastScan.TryGetValue(type, out lastScan))
                        {
                            if (Game.Time * 1000 - LastScan[type] < RefreshRate)
                            {
                                return CachedMinions;
                            }
                            CachedMinions = EntityManager.MinionsAndMonsters.Minions;
                            LastScan[type] = Game.Time * 1000;
                        }
                        else
                        {
                            CachedMinions = EntityManager.MinionsAndMonsters.Minions;
                            LastScan[type] = Game.Time * 1000;
                        }
                        return CachedMinions;
                    }
                case CachedEntityType.CombinedAttackableMinions:
                    {
                        if (IsDisposing)
                        {
                            return EntityManager.MinionsAndMonsters.CombinedAttackable;
                        }

                        float lastScan;

                        if (LastScan.TryGetValue(type, out lastScan))
                        {
                            if (Game.Time * 1000 - LastScan[type] < RefreshRate)
                            {
                                return CachedCombinedAttackableMinions;
                            }
                            CachedCombinedAttackableMinions = EntityManager.MinionsAndMonsters.CombinedAttackable;
                            LastScan[type] = Game.Time * 1000;
                        }
                        else
                        {
                            CachedCombinedAttackableMinions = EntityManager.MinionsAndMonsters.CombinedAttackable;
                            LastScan[type] = Game.Time * 1000;
                        }
                        return CachedCombinedAttackableMinions;
                    }
                case CachedEntityType.CombinedMinions:
                    {
                        if (IsDisposing)
                        {
                            return EntityManager.MinionsAndMonsters.Combined;
                        }

                        float lastScan;

                        if (LastScan.TryGetValue(type, out lastScan))
                        {
                            if (Game.Time * 1000 - LastScan[type] < RefreshRate)
                            {
                                return CachedCombinedMinions;
                            }
                            CachedCombinedMinions = EntityManager.MinionsAndMonsters.Combined;
                            LastScan[type] = Game.Time * 1000;
                        }
                        else
                        {
                            CachedCombinedMinions = EntityManager.MinionsAndMonsters.Combined;
                            LastScan[type] = Game.Time * 1000;
                        }
                        return CachedCombinedMinions;
                    }
                case CachedEntityType.Monsters:
                    {
                        if (IsDisposing)
                        {
                            return EntityManager.MinionsAndMonsters.Monsters;
                        }

                        float lastScan;

                        if (LastScan.TryGetValue(type, out lastScan))
                        {
                            if (Game.Time * 1000 - LastScan[type] < RefreshRate)
                            {
                                return CachedMonsters;
                            }
                            CachedMonsters = EntityManager.MinionsAndMonsters.Monsters;
                            LastScan[type] = Game.Time * 1000;
                        }
                        else
                        {
                            CachedMonsters = EntityManager.MinionsAndMonsters.Monsters;
                            LastScan[type] = Game.Time * 1000;
                        }
                        return CachedMonsters;
                    }
            }
            return null;
        }

        internal IEnumerable<Obj_AI_Minion> GetMinions(CachedEntityType type, Func<Obj_AI_Minion, bool> predicate)
        {
            if (type < CachedEntityType.EnemyMinion)
            {
                throw new Exception($"Invalid type passed. {type} is not supported by Obj_AI_Minion.");
            }

            switch (type)
            {
                case CachedEntityType.EnemyMinion:
                    {
                        if (IsDisposing)
                        {
                            return EntityManager.MinionsAndMonsters.EnemyMinions.Where(predicate);
                        }

                        float lastScan;

                        if (LastScan.TryGetValue(type, out lastScan))
                        {
                            if (CachedEnemyMinionsWithAction == null)
                            {
                                CachedEnemyMinionsWithAction =
                                    new Dictionary<Func<Obj_AI_Minion, bool>, IEnumerable<Obj_AI_Minion>>();
                            }

                            if (Game.Time * 1000 - LastScan[type] < RefreshRate)
                            {
                                IEnumerable<Obj_AI_Minion> output;

                                if (CachedEnemyMinionsWithAction.TryGetValue(predicate, out output))
                                {
                                    return CachedEnemyMinionsWithAction[predicate];
                                }
                            }
                            CachedEnemyMinionsWithAction[predicate] = GetMinions(type).Where(predicate);
                            LastScan[type] = Game.Time * 1000;
                        }
                        else
                        {
                            if (CachedEnemyMinionsWithAction == null)
                            {
                                CachedEnemyMinionsWithAction =
                                    new Dictionary<Func<Obj_AI_Minion, bool>, IEnumerable<Obj_AI_Minion>>();
                            }

                            CachedEnemyMinionsWithAction[predicate] = GetMinions(type).Where(predicate);

                            LastScan[type] = Game.Time * 1000;
                        }
                        return CachedEnemyMinionsWithAction[predicate];
                    }

                case CachedEntityType.AllyMinion:
                    {
                        if (IsDisposing)
                        {
                            return EntityManager.MinionsAndMonsters.AlliedMinions.Where(predicate);
                        }

                        float lastScan;

                        if (LastScan.TryGetValue(type, out lastScan))
                        {
                            if (CachedAllyMinionsWithAction == null)
                            {
                                CachedAllyMinionsWithAction =
                                    new Dictionary<Func<Obj_AI_Minion, bool>, IEnumerable<Obj_AI_Minion>>();
                            }

                            if (Game.Time * 1000 - LastScan[type] < RefreshRate)
                            {
                                IEnumerable<Obj_AI_Minion> output;

                                if (CachedAllyMinionsWithAction.TryGetValue(predicate, out output))
                                {
                                    return CachedAllyMinionsWithAction[predicate];
                                }
                            }
                            CachedAllyMinionsWithAction[predicate] = GetMinions(type).Where(predicate);
                            LastScan[type] = Game.Time * 1000;
                        }
                        else
                        {
                            if (CachedAllyMinionsWithAction == null)
                            {
                                CachedAllyMinionsWithAction =
                                    new Dictionary<Func<Obj_AI_Minion, bool>, IEnumerable<Obj_AI_Minion>>();
                            }

                            CachedAllyMinionsWithAction[predicate] = GetMinions(type).Where(predicate);

                            LastScan[type] = Game.Time * 1000;
                        }
                        return CachedAllyMinionsWithAction[predicate];
                    }
                case CachedEntityType.Minions:
                    {
                        if (IsDisposing)
                        {
                            return EntityManager.MinionsAndMonsters.Minions.Where(predicate);
                        }

                        float lastScan;

                        if (LastScan.TryGetValue(type, out lastScan))
                        {
                            if (CachedMinionsWithAction == null)
                            {
                                CachedMinionsWithAction =
                                    new Dictionary<Func<Obj_AI_Minion, bool>, IEnumerable<Obj_AI_Minion>>();
                            }

                            if (Game.Time * 1000 - LastScan[type] < RefreshRate)
                            {
                                IEnumerable<Obj_AI_Minion> output;

                                if (CachedMinionsWithAction.TryGetValue(predicate, out output))
                                {
                                    return CachedMinionsWithAction[predicate];
                                }
                            }
                            CachedMinionsWithAction[predicate] = GetMinions(type).Where(predicate);
                            LastScan[type] = Game.Time * 1000;
                        }
                        else
                        {
                            if (CachedMinionsWithAction == null)
                            {
                                CachedMinionsWithAction =
                                    new Dictionary<Func<Obj_AI_Minion, bool>, IEnumerable<Obj_AI_Minion>>();
                            }

                            CachedMinionsWithAction[predicate] = GetMinions(type).Where(predicate);

                            LastScan[type] = Game.Time * 1000;
                        }
                        return CachedMinionsWithAction[predicate];
                    }
                case CachedEntityType.CombinedAttackableMinions:
                    {
                        if (IsDisposing)
                        {
                            return EntityManager.MinionsAndMonsters.CombinedAttackable.Where(predicate);
                        }
                        float lastScan;

                        if (LastScan.TryGetValue(type, out lastScan))
                        {
                            if (CachedCombinedAttackableMinionsWithAction == null)
                            {
                                CachedCombinedAttackableMinionsWithAction =
                                    new Dictionary<Func<Obj_AI_Minion, bool>, IEnumerable<Obj_AI_Minion>>();
                            }

                            if (Game.Time * 1000 - LastScan[type] < RefreshRate)
                            {
                                IEnumerable<Obj_AI_Minion> output;

                                if (CachedCombinedAttackableMinionsWithAction.TryGetValue(predicate, out output))
                                {
                                    return CachedCombinedAttackableMinionsWithAction[predicate];
                                }
                            }
                            CachedCombinedAttackableMinionsWithAction[predicate] = GetMinions(type).Where(predicate);
                            LastScan[type] = Game.Time * 1000;
                        }
                        else
                        {
                            if (CachedCombinedAttackableMinionsWithAction == null)
                            {
                                CachedCombinedAttackableMinionsWithAction =
                                    new Dictionary<Func<Obj_AI_Minion, bool>, IEnumerable<Obj_AI_Minion>>();
                            }

                            CachedCombinedAttackableMinionsWithAction[predicate] = GetMinions(type).Where(predicate);

                            LastScan[type] = Game.Time * 1000;
                        }
                        return CachedCombinedAttackableMinionsWithAction[predicate];
                    }
                case CachedEntityType.CombinedMinions:
                    {
                        if (IsDisposing)
                        {
                            return EntityManager.MinionsAndMonsters.Combined.Where(predicate);
                        }
                        float lastScan;

                        if (LastScan.TryGetValue(type, out lastScan))
                        {
                            if (CachedCombinedMinionsWithAction == null)
                            {
                                CachedCombinedMinionsWithAction =
                                    new Dictionary<Func<Obj_AI_Minion, bool>, IEnumerable<Obj_AI_Minion>>();
                            }

                            if (Game.Time * 1000 - LastScan[type] < RefreshRate)
                            {
                                IEnumerable<Obj_AI_Minion> output;

                                if (CachedCombinedMinionsWithAction.TryGetValue(predicate, out output))
                                {
                                    return CachedCombinedMinionsWithAction[predicate];
                                }
                            }
                            CachedCombinedMinionsWithAction[predicate] = GetMinions(type).Where(predicate);
                            LastScan[type] = Game.Time * 1000;
                        }
                        else
                        {
                            if (CachedCombinedMinionsWithAction == null)
                            {
                                CachedCombinedMinionsWithAction =
                                    new Dictionary<Func<Obj_AI_Minion, bool>, IEnumerable<Obj_AI_Minion>>();
                            }

                            CachedCombinedMinionsWithAction[predicate] = GetMinions(type).Where(predicate);

                            LastScan[type] = Game.Time * 1000;
                        }
                        return CachedCombinedMinionsWithAction[predicate];
                    }
                case CachedEntityType.Monsters:
                    {
                        if (IsDisposing)
                        {
                            return EntityManager.MinionsAndMonsters.Monsters.Where(predicate);
                        }
                        float lastScan;

                        if (LastScan.TryGetValue(type, out lastScan))
                        {
                            if (CachedMonstersWithAction == null)
                            {
                                CachedMonstersWithAction =
                                    new Dictionary<Func<Obj_AI_Minion, bool>, IEnumerable<Obj_AI_Minion>>();
                            }

                            if (Game.Time * 1000 - LastScan[type] < RefreshRate)
                            {
                                IEnumerable<Obj_AI_Minion> output;

                                if (CachedMonstersWithAction.TryGetValue(predicate, out output))
                                {
                                    return CachedMonstersWithAction[predicate];
                                }
                            }
                            CachedMonstersWithAction[predicate] = GetMinions(type).Where(predicate);
                            LastScan[type] = Game.Time * 1000;
                        }
                        else
                        {
                            if (CachedMonstersWithAction == null)
                            {
                                CachedMonstersWithAction =
                                    new Dictionary<Func<Obj_AI_Minion, bool>, IEnumerable<Obj_AI_Minion>>();
                            }

                            CachedMonstersWithAction[predicate] = GetMinions(type).Where(predicate);

                            LastScan[type] = Game.Time * 1000;
                        }
                        return CachedMonstersWithAction[predicate];
                    }
            }
            return null;
        }

        ~MinionCache()
        {
            Dispose(true);
        }
        private void Finallize()
        {
            LastScan?.Clear();
            CachedEnemyMinionsWithAction?.Clear();
            CachedAllyMinionsWithAction?.Clear();
            CachedCombinedMinionsWithAction?.Clear();
            CachedCombinedAttackableMinionsWithAction?.Clear();
            CachedMinionsWithAction?.Clear();
            CachedMonstersWithAction?.Clear();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void DeleteUnnecessaryData()
        {
            Finallize();
        }

        private void Dispose(bool dispose)
        {
            if (IsDisposing || !dispose)
                return;

            IsDisposing = true;

            Finallize();
        }
    }
}