#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Cache.cs" company="EloBuddy">
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

namespace Marksman_Master.Cache
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EloBuddy;
    using Interfaces;
    using Utils;

    public class Cache : IDisposable
    {
        private List<ICachable> Objects { get; }
        private float LastTick { get; set; }

        public Cache()
        {
            Objects = new List<ICachable>();

            Game.OnTick += Game_OnTick;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
        }

        private void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            Dispose();
        }

        private void Game_OnTick(EventArgs args)
        {
            if (Game.Time*1000 - LastTick < 5000)
                return;

            Objects.ForEach(x =>
            {
                x.DeleteUnnecessaryData();
            });

            LastTick = Game.Time*1000;
        }

        /// <summary>
        /// Creates new instance of given type, if has default constructor
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <returns></returns>
        internal T Resolve<T>(int? refreshRate = null) where T : ICachable
        {
            if (typeof (T).GetConstructors().All(x => x.GetParameters().Length > 0))
            {
                throw new Exception($"{typeof(T)} does not contain a default constructor.");
            }

            dynamic resolvedType = null;

            try
            {
                resolvedType = (T) Activator.CreateInstance(typeof (T));
            }
            catch (Exception ex)
            {
                Misc.PrintDebugMessage(ex);
            }
            finally
            {
                if (resolvedType != null)
                {
                    if (refreshRate.HasValue)
                    {
                        resolvedType.RefreshRate = refreshRate.Value;
                    }

                    resolvedType.Load();

                    Objects.Add(resolvedType);
                }
            }

            return resolvedType;
        }


        /// <summary>
        /// Disposes object of given type
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="objectToDispose">Object to dispose</param>
        internal void Dispose<T>(object objectToDispose) where T : ICachable
        {
            if(Objects.Any(x=>x.GetType() == typeof(T)))
                return;

            Objects.ForEach(x =>
            {
                if (!x.Equals(objectToDispose)) // we compare values instead of references
                    return;

                x.Dispose();

                Objects.Remove(x);
            });
        }

        internal IEnumerable<T> Get<T>() where T : ICachable
        {
            foreach (var cachable in Objects)
            {
                if (cachable.GetType() == typeof (T))
                {
                    yield return (T)cachable;
                }
            }
        }

        public void Dispose()
        {
            Game.OnTick -= Game_OnTick;

            Objects.ForEach(x =>
            {
                x.Dispose();
                Objects.Remove(x);
            });

            GC.SuppressFinalize(this);
        }
    }
}