using EloBuddy;

namespace LeagueSharp.Common
{
    /// <summary>
    ///     The LeagueSharp.Common official menu.
    /// </summary>
    internal class CommonMenu
    {
        #region Static Fields

        /// <summary>
        ///     The menu instance.
        /// </summary>
        internal static Menu Instance = new Menu("[~] PortAIO - Core", "PortAIO.Settings", true);

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a static instance of the <see cref="CommonMenu" /> class.
        /// </summary>
        static CommonMenu()
        {
            Initialize();
        }

        public static void Initialize()
        {
            TargetSelector.Initialize();
            Prediction.Initialize();
            Hacks.Initialize();

            Instance.AddToMainMenu();
        }

        public static void Shutdown()
        {
            TargetSelector.Shutdown();
            Prediction.Shutdown();
            Hacks.Shutdown();

            Menu.Remove(Instance);
        }

        #endregion
    }
}