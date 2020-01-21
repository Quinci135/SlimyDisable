using System;
using System.Timers;
using System.IO;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace SlimyDisable
{
    [ApiVersion(2, 1)]
    public class SlimyDisable : TerrariaPlugin
    {

        public override string Author => "Quinci";

        public override string Description => "Stops slimy saddle invulnerability.";

        public override string Name => "SlimyDisable";

        public override Version Version => new Version(1, 0, 0, 0);

        public static Config config = new Config();

        public string configPath = Path.Combine(TShock.SavePath, "SlimyDisable.json");

        public SlimyDisable(Main game) : base(game)
        {

        }

        public override void Initialize()
        {
            ServerApi.Hooks.NpcStrike.Register(this, OnNpcStrike);
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NpcStrike.Deregister(this, OnNpcStrike);
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
            }
            base.Dispose(disposing);

        }
        Timer timer = new Timer(2000);
        int strikes;
        const int strikeCap = 18;
        TSPlayer player;
        private void OnNpcStrike(NpcStrikeEventArgs args)
        {
            player = TShock.Players[args.Player.whoAmI];
            if (!args.Handled && !(player.TPlayer.FindBuffIndex(130) == -1) && !player.HasPermission("slimydisable.bypass")) //check if player has slimy saddle buff
            {
                timer.Elapsed += ClearStrikes;
#if DEBUG
                Console.WriteLine($"Player {player.Name} has slime buff and hit {args.Npc.netID}");
#endif
                if (args.Npc.netID == 488 || args.Npc.netID == 68) // target dummy and dungeon guardian respectively
                {
                    if ((args.Player.position.Y + 80 > args.Npc.position.Y) && (args.Player.position.Y + 22 < args.Npc.position.Y) && ((args.Player.position.X - args.Npc.position.X < 64) && (args.Npc.position.X - args.Player.position.X < 64)))
                    {
                        strikes++;
#if DEBUG
                        player.SendMessage($"{strikes} strikes were counted", Color.BurlyWood);
                        player.SendMessage($"Player y: {args.Player.position.Y},\nNpc {args.Npc.netID} y: {args.Npc.position.Y}\n Diff y: {args.Player.position.Y - args.Npc.position.Y}", Color.Bisque);
                        player.SendMessage($"Player x: {args.Player.position.X},\nNpc {args.Npc.netID} x: {args.Npc.position.X}\n Diff x: {args.Player.position.X - args.Npc.position.X}", Color.Bisque);
#endif 
                        args.Handled = true;
                    }
                    if (strikes >= strikeCap)
                    {
#if DEBUG
                        player.SendMessage("You've been oofed p hard tbh ngl imho.", Color.BurlyWood);
#endif
                        Punishment();
                    }

                }
                timer.Start();

            }

        }

        void ClearStrikes(object sender, ElapsedEventArgs e)
        {
            timer.Stop();
            strikes = 0;

        }

        private void SlimyLogWarn(string action, bool silent=false)
        {
            string silence = "";
            if (silent)
            {
                silence = " silently";
            }
            TShock.Log.Warn($"Player {player.Name} has been {action}{silence} for attempting to abuse the slime mount invulnerability exploit.");
        }

        Color broadcastColor = Color.Aquamarine;

        private void SlimyBroadcast(string action)
        {
            SlimyLogWarn(action);
            TShock.Utils.Broadcast($"Player {player.Name} has been {action} for attempting to abuse an exploit.", broadcastColor);
        }

        private void Punishment()
        {
            switch (config.SelectedOption)
            {
                case "disableA":
                    player.Disable();
                    SlimyBroadcast("disabled");
                    break;
                case "disableS":
                    player.Disable();
                    SlimyLogWarn("disabled");
                    break;
                case "killA":
                    player.KillPlayer();
                    SlimyBroadcast("killed");
                    break;
                case "killS":
                    NetMessage.SendPlayerDeath(player.Index, Terraria.DataStructures.PlayerDeathReason.LegacyEmpty(), 99999, (new Random()).Next(-1, 1), false, -1, -1);
                    SlimyLogWarn("killed", true);
                    break;
                case "slapKill":
                    player.KillPlayer();
                    TShock.Utils.Broadcast($"Server has slapped {player.Name} to death for attempting to abuse an exploit.", broadcastColor);
                    break;
                case "kick":
                    TShock.Utils.ForceKick(player, "Attempted exploit abuse.", false, true);
                    SlimyLogWarn("kicked");
                    break;
                case "skick":
                    TShock.Utils.ForceKick(player, "Attempted exploit abuse.", true, true);
                    SlimyLogWarn("kicked", true);
                    break;
                case "ban":
                    TShockAPI.Commands.HandleCommand(TSPlayer.Server, $"{TShock.Config.CommandSpecifier}ban add {player.Name} \"Attempted invulnerability exploit.\"");
                    SlimyLogWarn("banned");
                    break;
                case "sban":
                    TShock.Utils.ForceKick(player, "Attempted invulnerability exploit", true, true);
                    TShock.Bans.AddBan(player.IP, player.Name, player.UUID, "Attempted abuse of slime mount invulnerability exploit.", false, "Server");
                    SlimyLogWarn("banned");
                    break;
                default:
                    player.Disable();
                    SlimyLogWarn("disabled");
                    break;
            }


        }
        private void OnInitialize(EventArgs e)
        {
            LoadConfig();
            Commands.ChatCommands.Add(new Command("slimydisable.change", ChangeSlimyConfig, "slimychange", "slc") { AllowServer = true, HelpText = "Changes the active punishment for slimy exploit attempts." });
        }
        private void ChangeSlimyConfig(CommandArgs args)
        {
            if (args.Parameters.Count == 0 || args.Parameters.Count > 2)
            {
                args.Player.SendInfoMessage($"Slimy Change subcommands: \n option [option name] \n change <option name>\n reloadconfig");
            }
            else if (args.Parameters[0] == "option")
            {
                if(args.Parameters.Count == 1)
                {
                    args.Player.SendErrorMessage($"Invalid syntax! Valid syntax: {TShock.Config.CommandSpecifier}slimychange option [option name]\n Available options: {config.ListOfAvailableOptions}.");
                }
                else 
                {
                    args.Player.SendInfoMessage(SelectOption(args.Parameters[1]));
                }
            }
            else if (args.Parameters[0] == "change")
            {
                if(args.Parameters.Count == 1)
                {
                    args.Player.SendErrorMessage($"Invalid syntax! Valid syntax: {TShock.Config.CommandSpecifier}slimychange change <option name>\n Use {TShock.Config.CommandSpecifier}slimychange option [option name] for more information on options.");
                }
                else 
                {
                    if (ChangeOption(args.Parameters[1]))
                    {
                        args.Player.SendSuccessMessage($"Succesfully changed the slimydisable action to \"{args.Parameters[1]}\"");
                    }
                    else
                    {
                        args.Player.SendErrorMessage($"Invalid option! Use {TShock.Config.CommandSpecifier}slimychange option [option name] for more information on options.");
                    }
                }

            }
            else if (args.Parameters[0] == "reloadconfig")
            {
                LoadConfig();
                args.Player.SendSuccessMessage("SlimyDisable config successfully reloaded.");
            }
            else
            {
                args.Player.SendErrorMessage($"Invalid syntax! Slimy Change subcommands: \n option [option name] \n change <option name> \n reloadconfig");
            }
            
        }
        private bool ChangeOption (string option)
        {
            switch (option)
            {
                case "disableA":
                case "disableS":
                case "killA":
                case "killS":
                case "slapKill":
                case "kick":
                case "skick":
                case "ban":
                case "sban":
                    config.SelectedOption = option;
                    config.Write(configPath);
                    LoadConfig();
                    return true;
                default:
                    return false;

            }
        }
        private string SelectOption(string option)
        {
            string message;
            switch (option)
            {
                case "disableA":
                    message = config.disableA;
                    break;
                case "disableS":
                    message = config.disableS;
                    break;
                case "killA":
                    message = config.killA;
                    break;
                case "killS":
                    message = config.killS;
                    break;
                case "slapKill":
                    message = config.slapKill;
                    break;
                case "kick":
                    message = config.kick;
                    break;
                case "skick":
                    message = config.skick;
                    break;
                case "ban":
                    message = config.ban;
                    break;
                case "sban":
                    message = config.sban;
                    break;
                case "list":
                    message = $"Valid options: {config.ListOfAvailableOptions}";
                    break;
                default:
                    message = $"Invalid option! Valid options: {config.ListOfAvailableOptions}";
                    break;
            }
            return message;
        }
        private void LoadConfig()
        {
            (config = Config.Read(configPath)).Write(configPath);
        }
    }
}
