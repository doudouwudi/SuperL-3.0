#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Bootstrap.cs" company="EloBuddy">
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
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.Sandbox;
using EloBuddy.SDK;
using Marksman_Master.Utils;
using SharpDX;

namespace Marksman_Master
{
    internal class Bootstrap
    {
        public static bool MenuLoaded { get; set; }

        public static string VersionMessage { get; private set; }

        public static Dictionary<string, ColorBGRA> SavedColorPickerData { get; set; }

        public static Dictionary<VersionInfo.Version, VersionInfo> Versions { get; private set; }

        public static void Initialize()
        {
            Versions = new Dictionary<VersionInfo.Version, VersionInfo>();

            Misc.PrintDebugMessage("Initializing cache");

            StaticCacheProvider.Initialize();

            Misc.PrintDebugMessage("Initializing addon");

            var pluginInitialized = InitializeAddon.Initialize();

            if (!pluginInitialized)
                return;

            var task = Task.Factory.StartNew(PrintVersionInfo);

            AppDomain.CurrentDomain.DomainUnload += (sender, args) =>
            {
                task.Dispose();
            };

            Core.DelayAction(
                () =>
                {
                    Misc.PrintDebugMessage("Creating Menu");

                    MenuManager.CreateMenu();
                    
                    Misc.PrintDebugMessage("Initializing activator");

                    Activator.Activator.InitializeActivator();

                    ChampionTracker.Initialize(ChampionTrackerFlags.PathingTracker);

                    MenuLoaded = true;

                    Misc.PrintInfoMessage(
                        $"<b><font color=\"#5ED43D\">{Player.Instance.ChampionName}</font></b> loaded successfully. Welcome back <b><font color=\"{(SandboxConfig.IsBuddy ? "#BF1B49" : "#1BBF91")}\">{(SandboxConfig.IsBuddy ? "[VIP] " + (SandboxConfig.Username == "intr" ? "intr you boosted animal from Latvia <3" : SandboxConfig.Username) : SandboxConfig.Username == "intr" ? "intr you boosted animal from Latvia <3" : SandboxConfig.Username)}</font></b> !");

                    Misc.PrintDebugMessage("Marksman AIO  fully loaded");
                }, 250);
        }

        private static System.Version GetGithubVersion()
        {
            try
            {
                using (var webClient = new WebClient())
                {
                    var downloadedData = webClient.DownloadString("https://raw.githubusercontent.com/Daeral/Marksman-AIO/master/Marksman%20Master/Marksman%20Master/Properties/AssemblyInfo.cs");

                    var regex = Regex.Match(downloadedData, @"\[assembly\: AssemblyVersion\(""([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)""\)\]");
                    var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;

                    if (string.IsNullOrEmpty(regex.Groups[1].Value) || string.IsNullOrWhiteSpace(regex.Groups[1].Value))
                        return assemblyVersion;

                    var githubSplittedVersion = regex.Groups[1].Value.Split('.');
                    var githubMajor = githubSplittedVersion[0] + "." + githubSplittedVersion[1];

                    var splittedAssemblyVersion = assemblyVersion.ToString().Split('.');
                    var assemblyMajor = splittedAssemblyVersion[0] + "." + splittedAssemblyVersion[1];

                    Versions[VersionInfo.Version.Github] = new VersionInfo(githubMajor, githubSplittedVersion[2], githubSplittedVersion[3], VersionInfo.Version.Github);
                    Versions[VersionInfo.Version.Assembly] = new VersionInfo(assemblyMajor, splittedAssemblyVersion[2], splittedAssemblyVersion[3], VersionInfo.Version.Assembly);

                    var comparedMajorVersions = Versions[VersionInfo.Version.Assembly].CompareMajorVersion(Versions[VersionInfo.Version.Github]);

                    if (comparedMajorVersions < 0)
                    {
                        VersionMessage = $"Your Marksman Master version is {(Math.Abs(comparedMajorVersions) == 1 ? "1" : "several")} major patch{((comparedMajorVersions != -1) && (comparedMajorVersions != 1) ? "es" : "")} behind.\nIt's highly recommended to update it in the loader !\nLatest version : {Versions[VersionInfo.Version.Github]} | Your version : {Versions[VersionInfo.Version.Assembly]}";
                    } else
                    {
                        var comparedMinorVersions = Versions[VersionInfo.Version.Assembly].CompareMinorVersions(Versions[VersionInfo.Version.Github]);

                        if (comparedMinorVersions < 0)
                        {
                            VersionMessage =
                                $"Your Marksman Master version is {Math.Abs(comparedMinorVersions)} patch{((comparedMinorVersions != -1) && (comparedMinorVersions != 1) ? "es" : "")} that include new features behind.\nIt's recommended to update your Marksman Master it in the loader !\nLatest version : {Versions[VersionInfo.Version.Github]} | Your version : {Versions[VersionInfo.Version.Assembly]}";
                        }
                    }

                    return new System.Version(regex.Groups[1].Value);
                }
            }
            catch (Exception exception)
            {
                var ex = exception as WebException;

                Console.WriteLine(ex != null
                    ? $"Couldn't check version a WebException occured\nStatus : {ex.Status} | Message : {ex.Message}{Environment.NewLine}"
                    : $"Couldn't check version an exception occured\n{exception}{Environment.NewLine}");

                return Assembly.GetExecutingAssembly().GetName().Version;
            }
        }

        private static int CompareVersions()
        {
            try
            {
                var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
                return GetGithubVersion().CompareTo(assemblyVersion);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Couldn't check version an exception occured\n{exception}{Environment.NewLine}");

                return 0;
            }
        }

        private static void PrintVersionInfo()
        {
            try
            {
                var version = CompareVersions();

                if (version == 1)
                {
                    Misc.PrintInfoMessage("<i><red>Your assembly version is outdated. Consider updating it in the loader.</red></i>");
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Couldn't check version an exception occured\n{exception}{Environment.NewLine}");
            }
        }

        public class VersionInfo
        {
            public enum Version
            {
                Github, Assembly
            }

            public float MajorVersion { get; }
            public int MinorVersion { get; }
            public int PatchVersion { get; }
            public Version VersionType { get; }

            public VersionInfo(float majorVersion, int minorVersion, int patchVersion, Version version)
            {
                MajorVersion = majorVersion;
                MinorVersion = minorVersion;
                PatchVersion = patchVersion;
                VersionType = version;
            }

            public VersionInfo(string majorVersion, string minorVersion, string patchVersion, Version version)
            {
                MajorVersion = Convert.ToSingle(majorVersion);
                MinorVersion = Convert.ToInt32(minorVersion);
                PatchVersion = Convert.ToInt32(patchVersion);
                VersionType = version;
            }

            /// <summary>
            /// Returns negative integer if <see cref="MajorVersion"/> is behind to <see cref="secondMajorVerion"/>
            /// otherwise returns 0 if both are equal or positive integer if <see cref="MajorVersion"/> is ahead of <see cref="secondMajorVerion"/>
            /// </summary>
            /// <param name="secondMajorVerion">secondMajorVerion</param>
            /// <returns>substraction of <see cref="MajorVersion"/> and <see cref="secondMajorVerion"/></returns>
            public int CompareMajorVersion(float secondMajorVerion)
            {
                float x;
                double xTruncate;
                if (BitConverter.GetBytes(decimal.GetBits((decimal)MajorVersion)[3])[2] == 1)
                {
                    var temp = MajorVersion.ToString(System.Globalization.CultureInfo.InvariantCulture).Split('.');
                    var joined = $"{temp[0]}.0{temp[1]}";
                    x = Convert.ToSingle(joined);
                    
                    xTruncate = Math.Truncate(x);
                }
                else
                {
                    x = MajorVersion;
                    xTruncate = Math.Truncate(MajorVersion);
                }

                float y;
                double yTruncate;
                if (BitConverter.GetBytes(decimal.GetBits((decimal)secondMajorVerion)[3])[2] == 1)
                {
                    var temp = secondMajorVerion.ToString(System.Globalization.CultureInfo.InvariantCulture).Split('.');
                    var joined = $"{temp[0]}.0{temp[1]}";
                    y = Convert.ToSingle(joined);

                    yTruncate = Math.Truncate(y);
                }
                else
                {
                    y = secondMajorVerion;
                    yTruncate = Math.Truncate(secondMajorVerion);
                }

                var xDecimalPart = x - xTruncate;
                var yDecimalPart = y - yTruncate;

                var result = Math.Round(xDecimalPart - yDecimalPart, 2)*100 + (xTruncate - yTruncate);
                return (int)result;
            }

            /// <summary>
            /// Returns negative integer if <see cref="MajorVersion"/> is behind to <see cref="secondMajorVerion"/>
            /// otherwise returns 0 if both are equal or positive integer if <see cref="MajorVersion"/> is ahead of <see cref="secondMajorVerion"/>
            /// </summary>
            /// <param name="secondMajorVerion">secondMajorVerion</param>
            /// <returns>substraction of <see cref="MajorVersion"/> and <see cref="secondMajorVerion"/></returns>
            public int CompareMajorVersion(VersionInfo secondMajorVerion)
            {
                return CompareMajorVersion(secondMajorVerion.MajorVersion);
            }

            public int CompareMinorVersions(int secondMinorVerion)
            {
                return MinorVersion - secondMinorVerion;
            }

            public int CompareMinorVersions(VersionInfo secondMinorVerion)
            {
                return CompareMinorVersions(secondMinorVerion.MinorVersion);
            }
            public int ComparePatchVersions(int secondPatchVerion)
            {
                return PatchVersion - secondPatchVerion;
            }

            public int ComparePatchVersions(VersionInfo secondPatchVerion)
            {
                return CompareMinorVersions(secondPatchVerion.MinorVersion);
            }

            public override string ToString()
            {
                return $"{MajorVersion.ToString("F")}.{MinorVersion}.{PatchVersion}";
            }
        }
    }
}