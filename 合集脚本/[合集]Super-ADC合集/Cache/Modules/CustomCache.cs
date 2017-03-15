#region Licensing
// ---------------------------------------------------------------------
// <copyright file="CustomCache.cs" company="EloBuddy">
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
using EloBuddy;
using Marksman_Master.Cache.Interfaces;

namespace Marksman_Master.Cache.Modules
{
    internal class CustomCache<TComparer, TValue> : CacheModuleBase, IDisposable, ICachable
    {
        internal override string Name { get; } = "CustomCache";

        public bool IsDisposing { get; private set; }

        private Dictionary<TComparer, Tuple<float, TValue>> CachedValues { get; set; }

        internal TValue this[TComparer id] => CachedValues.ContainsKey(id) ? CachedValues[id].Item2 : default(TValue);

        internal override void Load()
        {
            CachedValues = new Dictionary<TComparer, Tuple<float, TValue>>();
        }

        ~CustomCache()
        {
            Dispose(true);
        }

        internal void Add(TComparer comparer, TValue value)
        {
            if (IsDisposing)
                return;

            CachedValues[comparer] = new Tuple<float, TValue>(Game.Time*1000, value);
        }

        internal TValue Get(TComparer comparer)
        {
            if (IsDisposing)
                return default(TValue);

            Tuple<float, TValue> output;

            if (CachedValues.TryGetValue(comparer, out output))
            {
                if (Game.Time*1000 - output.Item1 < RefreshRate)
                {
                    return output.Item2;
                }
            }
            else
            {
                CachedValues[comparer] = new Tuple<float, TValue>(Game.Time * 1000, default(TValue));
            }
            return CachedValues[comparer].Item2;
        }

        public void DeleteUnnecessaryData()
        {
            CachedValues?.Clear();
        }

        internal bool Exist(TComparer comparer)
        {
            return !IsDisposing && CachedValues.ContainsKey(comparer) &&
                   (Game.Time*1000 - CachedValues[comparer].Item1 < RefreshRate);
        }


        private void Finallize()
        {
            CachedValues?.Clear();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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