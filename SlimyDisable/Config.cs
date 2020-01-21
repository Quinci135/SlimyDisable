using System;
using Newtonsoft.Json;
using System.IO;

namespace SlimyDisable
{
    public class Config
    {

        public readonly string ListOfAvailableOptions = "disableA, disableS, killA, killS, slapKill, kick, skick, ban, sban";

        public readonly string disableA = "Disables player and announces to everyone \"Player {Playername} has been disabled for attempting an invulnerability exploit.\"";

        public readonly string disableS = "Disables player without announcement and warns on console.";

        public readonly string killA = "Kills player and announces to everyone \"Player {Playername} has been killed for attempting an invulnerability exploit.\"";

        public readonly string killS = "Kills player without announcement and warns on console.";

        public readonly string slapKill = "Kills player with announcements \"Player {Playername} has been killed for attempting an invulnerability exploit.\" and \"Server slapped {Playername} for 5,000 damage.\"";

        public readonly string kick = "Kicks the player and announces \"Player {Playername} has been kicked for attempting an invulnerability exploit.\"";

        public readonly string skick = "Kicks the player silently for the reason \"Attempting an invulnerability exploit.\" and warns on console. ";

        public readonly string ban = "Bans the player for \"Attempting an invulnerability exploit.\"";

        public readonly string sban = "Silently bans the player for the reason \"Attempting an invulnerability exploit.\"";

        public string SelectedOption = "disableS";

        public void Write(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public static Config Read(string path)
        {
            return !File.Exists(path)
                ? new Config()
                : JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
        }
    }
}
