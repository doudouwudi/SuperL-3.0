#region Licensing
// ---------------------------------------------------------------------
// <copyright file="BaseUlt.cs" company="EloBuddy">
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

namespace Marksman_Master.Extensions.SkinHack
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using EloBuddy;
    using EloBuddy.SDK.Menu;
    using EloBuddy.SDK.Menu.Values;

    internal sealed class SkinHack : ExtensionBase
    {
        private Menu SkinHackMenu { get; set; }

        public override bool IsEnabled { get; set; }

        public static bool EnabledByDefault { get; set; } = true;

        public override string Name { get; } = "SkinHack";

        public Dictionary<string, byte> Skins { get; private set; }
        public Dictionary<KeyValuePair<Champion, byte>, Dictionary<string, byte>> Chromas { get; private set; }
        public Dictionary<Champion, string> BaseSkinNames { get; private set; }

        public ComboBox SkinId { get; set; }
        public Slider ChromaId { get; set; }

        public byte LoadSkinId { get; private set; }

        public byte CurrentSkin { get; set; }

        public override void Load()
        {
            LoadSkinId = (byte) Player.Instance.SkinId;

            IsEnabled = true;

            BaseSkinNames = new Dictionary<Champion, string>
            {
                [Champion.Ashe] = "Ashe",
                [Champion.Caitlyn] = "Caitlyn",
                [Champion.Corki] = "Corki",
                [Champion.Draven] = "Draven",
                [Champion.Ezreal] = "Ezreal",
                [Champion.Graves] = "Graves",
                [Champion.Jhin] = "Jhin",
                [Champion.Jinx] = "Jinx",
                [Champion.Kalista] = "Kalista",
                [Champion.KogMaw] = "KogMaw",
                [Champion.Lucian] = "Lucian",
                [Champion.MissFortune] = "MissFortune",
                [Champion.Quinn] = "Quinn",
                [Champion.Sivir] = "Sivir",
                [Champion.Tristana] = "Tristana",
                [Champion.Twitch] = "Twitch",
                [Champion.Urgot] = "Urgot",
                [Champion.Varus] = "Varus",
                [Champion.Vayne] = "Vayne"
            };

            Chromas = new Dictionary<KeyValuePair<Champion, byte>, Dictionary<string, byte>>
            {
                {new KeyValuePair<Champion, byte>(Champion.Ezreal, 7), new Dictionary<string, byte>
                    {
                        {"Amethyst", 7},
                        {"Meteorite", 10},
                        {"Obsidian", 11},
                        {"Pearl", 12},
                        {"Rose", 13},
                        {"Quartz", 14},
                        {"Ruby", 15},
                        {"Sandstone", 16},
                        {"Striped", 17}
                    }
                },
                {new KeyValuePair<Champion, byte>(Champion.Caitlyn, 0), new Dictionary<string, byte>
                    {
                        {"Default", 0},
                        {"Pink", 7},
                        {"Green", 8},
                        {"Blue", 9}
                    }
                },
                {new KeyValuePair<Champion, byte>(Champion.Lucian, 0), new Dictionary<string, byte>
                    {
                        {"Default", 0},
                        {"Yellow", 3},
                        {"Red", 4},
                        {"Blue", 5}
                    }
                },
                {new KeyValuePair<Champion, byte>(Champion.MissFortune, 7), new Dictionary<string, byte>
                    {
                        {"Amethyst", 7},
                        {"Aquamarine", 11},
                        {"Citrine", 12},
                        {"Peridot", 13},
                        {"Ruby", 14}
                    }
                },
                {new KeyValuePair<Champion, byte>(Champion.Vayne, 3), new Dictionary<string, byte>
                    {
                        {"Default", 3},
                        {"Green", 7},
                        {"Red", 8},
                        {"Silver", 9}
                    }
                },
                {new KeyValuePair<Champion, byte>(Champion.Tristana, 6), new Dictionary<string, byte>
                    {
                        {"Default", 6},
                        {"Navy", 7},
                        {"Purple", 8},
                        {"Orange", 9}
                    }
                }
            };

            var skin = new SkinData(Player.Instance.ChampionName);
            Skins = skin.ToDictionary();
            
            if (!MenuManager.ExtensionsMenu.SubMenus.Any(x => x.UniqueMenuId.Contains("Extension.SkinHack")))
            {
                if (!MainMenu.IsOpen)
                {
                    SkinHackMenu = MenuManager.ExtensionsMenu.AddSubMenu("英雄换肤", "Extension.SkinHack");
                    BuildMenu();
                }
                else MainMenu.OnClose += MainMenu_OnClose;
            }
            else
            {
                var subMenu =
                    MenuManager.ExtensionsMenu.SubMenus.Find(x => x.UniqueMenuId.Contains("Extension.SkinHack"));

                if (subMenu?["SkinId." + Player.Instance.ChampionName] == null)
                    return;

                SkinId = subMenu["SkinId." + Player.Instance.ChampionName].Cast<ComboBox>();
                ChromaId = subMenu["ChromaId." + Player.Instance.ChampionName].Cast<Slider>();

                subMenu["SkinId." + Player.Instance.ChampionName].Cast<ComboBox>().OnValueChange += SkinId_OnValueChange;
                subMenu["ChromaId." + Player.Instance.ChampionName].Cast<Slider>().OnValueChange += ChromaId_OnValueChange;

                UpdateChromaSlider(SkinId.CurrentValue);

                if (HasChromaPack(SkinId.CurrentValue))
                {
                    ChangeSkin(SkinId.CurrentValue, ChromaId.CurrentValue);
                } else ChangeSkin(SkinId.CurrentValue);
            }
        }

        private void MainMenu_OnClose(object sender, EventArgs args)
        {
            if (MenuManager.ExtensionsMenu.SubMenus.Any(x => x.UniqueMenuId.Contains("Extension.SkinHack")))
                return;

            SkinHackMenu = MenuManager.ExtensionsMenu.AddSubMenu("Skin Hack", "Extension.SkinHack");
            BuildMenu();

            MainMenu.OnClose -= MainMenu_OnClose;
        }

        private void BuildMenu()
        {
            var skins =
                Skins.Select(x => x.Key)
                    .ToList();

            if (!skins.Any())
                return;

            SkinHackMenu.AddGroupLabel("Skin hack settings : ");

            SkinId = SkinHackMenu.Add("SkinId." + Player.Instance.ChampionName, new ComboBox("Skin : ", skins));

            if(LoadSkinId != 0)
                SkinId.CurrentValue = LoadSkinId;

            SkinHackMenu.AddSeparator(5);

            BuildChroma();
        }

        private void BuildChroma()
        {
            ChromaId = SkinHackMenu.Add("ChromaId." + Player.Instance.ChampionName, new Slider("Chroma : "));
            ChromaId.IsVisible = false;
            ChromaId.OnValueChange += ChromaId_OnValueChange;
            SkinId.OnValueChange += SkinId_OnValueChange;

            if (HasChromaPack(SkinId.CurrentValue))
            {
                var dictionary = GetChromaList(SkinId.CurrentValue);

                if (dictionary == null)
                {
                    ChangeSkin(SkinId.CurrentValue);

                    return;
                }
                var maxValue = dictionary.Select(x => x.Key).Count();

                ChromaId.MaxValue = maxValue - 1;

                ChromaId.DisplayName = GetChromaName(SkinId.CurrentValue, ChromaId.CurrentValue);

                ChromaId.IsVisible = true;

                if (Player.Instance.SkinId == 0)
                    ChangeSkin(SkinId.CurrentValue, ChromaId.CurrentValue);
            }
            else if(Player.Instance.SkinId == 0)
                ChangeSkin(SkinId.CurrentValue);
        }

        private void ChromaId_OnValueChange(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
        {
            var currentId = SkinId.CurrentValue;

            ChromaId.DisplayName = GetChromaName(SkinId.CurrentValue, ChromaId.CurrentValue);
            
            ChangeSkin(currentId, args.NewValue);
        }

        private void UpdateChromaSlider(int id)
        {
            var dictionary = GetChromaList(id);

            if (dictionary == null)
            {
                ChromaId.IsVisible = false;
                return;
            }

            var maxValue = dictionary.Select(x => x.Key).Count();

            ChromaId.MaxValue = maxValue - 1;

            ChromaId.DisplayName = GetChromaName(SkinId.CurrentValue, ChromaId.CurrentValue);

            ChromaId.IsVisible = true;
        }
        
        private void SkinId_OnValueChange(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
        {
            if (HasChromaPack(args.NewValue))
            {
                UpdateChromaSlider(args.NewValue);

                ChangeSkin(args.NewValue, ChromaId.CurrentValue);
                return;
            }

            ChromaId.IsVisible = false;

            ChangeSkin(args.NewValue);
        }

        private bool HasChromaPack(int id)
            => (Chromas != null) && Chromas.ContainsKey(new KeyValuePair<Champion, byte>(Player.Instance.Hero, (byte) id));

        private string GetChromaName(int id, int chromaId)
        {
            if ((Chromas == null) || !Chromas.ContainsKey(new KeyValuePair<Champion, byte>(Player.Instance.Hero, (byte) id)))
                return string.Empty;

            var dictionary = GetChromaList(id);
            var baseSkinName = Skins.FirstOrDefault(x => x.Value == id).Key;

            if (dictionary == null)
                return baseSkinName;

            var chromaIdT = dictionary.ElementAtOrDefault(chromaId).Key;

            return chromaIdT != default(string) ? $"{baseSkinName} : {chromaIdT} chroma" : baseSkinName;
        }

        private Dictionary<string, byte> GetChromaList(int id)
            =>
                !HasChromaPack(id)
                    ? null
                    : Chromas.FirstOrDefault(x => (x.Key.Key == Player.Instance.Hero) && (x.Key.Value == id)).Value;

        private void ChangeSkin(int id, int? chromaId = null)
        {
            if (!IsEnabled)
                return;

            var skins = Skins;

            if (skins == null)
            {
                return;
            }

            var skinId = skins.ElementAtOrDefault(id).Value;

            if (chromaId.HasValue && HasChromaPack(id))
            {
                var dictionary = GetChromaList(id);

                if (dictionary != null)
                {
                    var chromaIdT = dictionary.ElementAtOrDefault(chromaId.Value).Value;

                    if (chromaIdT != 0)
                    {
                        SetSkin(chromaIdT);
                        return;
                    }
                }
            }

            SetSkin(skinId);

            CurrentSkin = skinId;
        }

        private void SetSkin(int id)
        {
            if (Player.Instance.Model.Equals(Player.Instance.BaseSkinName))
            {
                Player.Instance.SetSkinId(id);
            } else Player.Instance.SetSkin(BaseSkinNames[Player.Instance.Hero], id);
        }
        
        public override void Dispose()
        {
            IsEnabled = false;
            
            SkinId.OnValueChange -= SkinId_OnValueChange;
            ChromaId.OnValueChange -= ChromaId_OnValueChange;

            MainMenu.OnClose -= MainMenu_OnClose;

            SetSkin(LoadSkinId);
        }

        public class SkinData
        {
            public string DDragonVersion { get; private set; }
            public Skins SkinsData { get; private set; }
            public string ChampionName { get; }
            private string Data { get; set; }

            public SkinData(string championName)
            {
                ChampionName = championName;

                try
                {
                    Task.Run(() => DownloadData()).Wait(1500);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }

            private void DownloadData()
            {
                try
                {
                    using (var webClient = new WebClient())
                    {
                        DDragonVersion = (string)JObject.Parse(webClient.DownloadString(new Uri("http://ddragon.leagueoflegends.com/realms/na.json"))).Property("dd");
                        Data = webClient.DownloadString($"http://ddragon.leagueoflegends.com/cdn/{DDragonVersion}/data/en_US/champion/{ChampionName}.json");
                    }

                    var parsedObject = JObject.Parse(Data);
                    var data = parsedObject["data"][ChampionName];

                    SkinsData = data.ToObject<Skins>();
                }
                catch (Exception exception)
                {
                    var ex = exception as WebException;

                    Console.WriteLine(ex != null
                        ? $"Couldn't load skinhack a WebException occured\nStatus : {ex.Status} | Message : {ex.Message}{Environment.NewLine}"
                        : $"Couldn't load skinhack an exception occured\n{exception}{Environment.NewLine}");
                }
            }

            public Dictionary<string, byte> ToDictionary()
            {
                var output = new Dictionary<string, byte>();

                try
                {
                    foreach (var skin in SkinsData.SkinsInfos)
                    {
                        output[skin.SkinName] = (byte) skin.SkinId;
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
                return output;
            }
            
            public class SkinInfo
            {
                [JsonProperty(PropertyName = "id")]
                public string GameSkinId { get; set; }

                [JsonProperty(PropertyName = "num")]
                public int SkinId { get; set; }

                [JsonProperty(PropertyName = "name")]
                public string SkinName { get; set; }

                [JsonProperty(PropertyName = "chromas")]
                public bool HasChromas { get; set; }
            }

            public class Skins
            {
                [JsonProperty(PropertyName = "skins")]
                public SkinInfo[] SkinsInfos { get; set; }
            }
        }

        public class WebService
        {
            public int Timeout { get; set; }

            public WebService(int timeout = 2000)
            {
                Timeout = timeout;
            }

            public string SendRequest(Uri uri)
            {
                var request = WebRequest.Create(uri);

                request.Timeout = Timeout;

                try
                {
                    using (var result = request.GetResponse())
                    {
                        using (var response = result as HttpWebResponse)
                        {
                            if ((response == null) || (response.StatusCode != HttpStatusCode.OK))
                            {
                                return string.Empty;
                            }

                            using (var stream = response.GetResponseStream())
                            {
                                if (stream == null)
                                    return string.Empty;

                                using (var streamReader = new StreamReader(stream))
                                {
                                    return streamReader.ReadToEnd();
                                }
                            }
                        }
                    }
                }
                catch (WebException ex)
                {
                    Console.WriteLine(
                        $"{ex}\nServer : {uri.OriginalString}\nMessage : {ex.Message} | Status code : {ex.Status}");
                }

                return string.Empty;
            }
        }
    }
}