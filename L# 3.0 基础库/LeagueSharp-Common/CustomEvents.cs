namespace LeagueSharp.Common
{
    using EloBuddy;
    //    using EloBuddy.SDK.Events;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     Provides custom events.
    /// </summary>
    public static class CustomEvents
    {
        public class Game
        {
            public static event OnGameEndHandler OnGameEnd;

            public delegate void OnGameEndHandler();

            static Game()
            {
                Utility.DelayAction.Add(0, Initialize);
            }

            /// <summary>
            ///     Initializes this instance.
            /// </summary>
            public static void Initialize()
            {
                var gameEndNotified = false;
                EloBuddy.Game.OnTick += delegate
                {
                    // Make sure we're not repeating the invoke
                    if (gameEndNotified)
                    {
                        return;
                    }

                    // Gets a dead nexus
                    // and the nexus is dead or its health is equal to 0
                    var nexus = ObjectManager.Get<Obj_HQ>().FirstOrDefault(n => n.Health <= 0 || n.IsDead);

                    // Check and return if the nexus is null
                    if (nexus == null)
                    {
                        return;
                    }

                    // Invoke the event
                    OnGameEnd?.Invoke();

                    // Set gameEndNotified to true, as the event has been completed
                    gameEndNotified = true;
                };

                Chat.OnClientSideMessage += delegate (ChatClientSideMessageEventArgs eventArgs)
                {
                    if (eventArgs.Message.ToLower().Contains(" team agreed to a surrender with ") && !gameEndNotified)
                    {
                        OnGameEnd?.Invoke();
                        gameEndNotified = true;
                    }
                };
            }
        }

        /// <summary>
        ///     Provides custom events regarding units.
        /// </summary>
        public class Unit
        {
            #region Constructors and Destructors

            /// <summary>
            ///     Initializes static members of the <see cref="Unit" /> class.
            /// </summary>
            static Unit()
            {
                EloBuddy.Game.OnProcessPacket += PacketHandler;

                //Initializes ondash class:
                ObjectManager.Player.IsDashing();
            }

            #endregion

            #region Delegates

            /// <summary>
            ///     The delegate for <see cref="Unit.OnDash" />
            /// </summary>
            /// <param name="sender">The sender.</param>
            /// <param name="args">The arguments.</param>
            public delegate void OnDashed(Obj_AI_Base sender, Dash.DashItem args);

            /// <summary>
            ///     The delegate for <see cref="Unit.OnLevelUp" />
            /// </summary>
            /// <param name="sender">The sender.</param>
            /// <param name="args">The <see cref="OnLevelUpEventArgs" /> instance containing the event data.</param>
            public delegate void OnLeveledUp(Obj_AI_Base sender, OnLevelUpEventArgs args);

            /// <summary>
            ///     The delegate for <see cref="Unit.OnLevelUpSpell" />
            /// </summary>
            /// <param name="sender">The sender.</param>
            /// <param name="args">The <see cref="OnLevelUpSpellEventArgs" /> instance containing the event data.</param>
            public delegate void OnLeveledUpSpell(Obj_AI_Base sender, OnLevelUpSpellEventArgs args);

            #endregion

            #region Public Events

            /// <summary>
            ///     Occurs when a unit dashes.
            /// </summary>
            public static event OnDashed OnDash;

            /// <summary>
            ///     Occurs when a unit levels up.
            /// </summary>
            public static event OnLeveledUp OnLevelUp;

            /// <summary>
            ///     Occurs when the player levels up a spell.
            /// </summary>
            public static event OnLeveledUpSpell OnLevelUpSpell;

            #endregion

            #region Public Methods and Operators

            /// <summary>
            ///     Triggers the on dash.
            /// </summary>
            /// <param name="sender">The sender.</param>
            /// <param name="args">The arguments.</param>
            public static void TriggerOnDash(Obj_AI_Base sender, Dash.DashItem args)
            {
                var dashHandler = OnDash;
                if (dashHandler != null)
                {
                    dashHandler(sender, args);
                }
            }

            #endregion

            #region Methods

            /// <summary>
            ///     Handles packets.
            /// </summary>
            /// <param name="args">The <see cref="GamePacketEventArgs" /> instance containing the event data.</param>
            private static void PacketHandler(GamePacketEventArgs args)
            {
            }

            #endregion

            /// <summary>
            ///     The event arguments for the <see cref="Unit.OnLevelUp" /> event.
            /// </summary>
            public class OnLevelUpEventArgs : EventArgs
            {
                #region Fields

                /// <summary>
                ///     The new level
                /// </summary>
                public int NewLevel;

                /// <summary>
                ///     The remaining points
                /// </summary>
                public int RemainingPoints;

                #endregion
            }

            /// <summary>
            ///     The event arguments for the <see cref="Unit.OnLevelUpSpell" /> event.
            /// </summary>
            public class OnLevelUpSpellEventArgs : EventArgs
            {
                #region Fields

                /// <summary>
                ///     The remainingpoints
                /// </summary>
                public int Remainingpoints;

                /// <summary>
                ///     The spell identifier
                /// </summary>
                public int SpellId;

                /// <summary>
                ///     The spell level
                /// </summary>
                public int SpellLevel;

                #endregion

                #region Constructors and Destructors

                /// <summary>
                ///     Initializes a new instance of the <see cref="OnLevelUpSpellEventArgs" /> class.
                /// </summary>
                internal OnLevelUpSpellEventArgs()
                {
                }

                #endregion
            }
        }
    }
}
