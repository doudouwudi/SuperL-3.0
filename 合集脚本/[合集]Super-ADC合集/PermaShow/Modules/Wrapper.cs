#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Wrapper.cs" company="EloBuddy">
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
namespace Marksman_Master.PermaShow.Modules
{
    using System;

    using System.Collections.Generic;

    using System.Linq;

    using System.Reflection;

    internal class Wrapper : ModuleBase
    {
        public Wrapper this[string name] { get { return ModuleObjects.First(x=>x.GetType().Name == name); } }
        
        public List<Wrapper> ModuleObjects { get; } = new List<Wrapper>(); 

        public override void Load()
        {
            try
            {
                LoadAll();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occurred : {0}", ex);
                throw;
            }
        }

        public void InvokeLoadMethodForAll()
        {
            ModuleObjects.ForEach(x => x.Load());
        }

        public IEnumerable<Type> GetAll()
        {
            try
            {
                return Assembly.GetAssembly(typeof(Wrapper)).GetTypes().Where(x => x.IsClass && !x.IsAbstract && x.IsSubclassOf(typeof(Wrapper)) && x.Name != "Wrapper").ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occurred : {0}", ex);
                throw;
            }
        }

        private void LoadAll()
        {
            try
            {
                foreach (var type in GetAll())
                {
                    try
                    {
                        var instance = Activator.CreateInstance(type, null);
                        try
                        {
                            ModuleObjects.Add(instance as Wrapper);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Exception occurred : {0}", ex);
                            throw;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception occurred : {0}", ex);
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occurred : {0}", ex);
                throw;
            }
        }

        public T Bind<T>(ModuleBase baseType) where T : Wrapper
        {
            try
            {
                return (T)Convert.ChangeType(baseType, typeof(T));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occurred : {0}", ex);
                throw;
            }
        }

        public T Bind<T>() where T : Wrapper
        {
            try
            {
                return (T)Convert.ChangeType(ModuleObjects.First(x => x.GetType() == typeof(T)), typeof(T));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occurred : {0}", ex);
                throw;
            }
        }
    }
}
