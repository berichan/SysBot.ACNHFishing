using System;
using System.Collections.Generic;
using System.Linq;
using SysBot.Base;

namespace SysBot.ACNHFishing
{
    [Serializable]
    public sealed record CrossBotConfig : SwitchConnectionConfig
    {
        #region Discord

        /// <summary> Custom Discord Status for playing a game. </summary>
        public string Name { get; set; } = "CrossBot";

        /// <summary> Bot login token. </summary>
        public string Token { get; set; } = "DISCORD_TOKEN";

        /// <summary> Bot command prefix. </summary>
        public string Prefix { get; set; } = "$";

        /// <summary> Users with this role are allowed to interact with the bot. If "@everyone", anyone can interact. </summary>
        public string RoleUseBot { get; set; } = "@everyone";

        // 64bit numbers white-listing certain channels/users for permission
        public List<ulong> Channels { get; set; } = new();
        public List<ulong> Users { get; set; } = new();
        public List<ulong> Sudo { get; set; } = new();

        public List<ulong> LoggingChannels { get; set; } = new();

        // Should we ignore all permissions for commands and allow inter-bot talk? This should only be used for debug/apps that layer on top of the acnh bot through discord.
        public bool IgnoreAllPermissions { get; set; } = false;

        #endregion

        #region Features

        /// <summary> Skips creating bots when the program is started; helpful for testing integrations. </summary>
        public bool SkipConsoleBotCreation { get; set; }

        /// <summary> When enabled, the Bot will not allow RAM edits if the player's item metadata is invalid. </summary>
        /// <remarks> Only disable this as a last resort, and you have corrupted your item metadata through other means. </remarks>
        public bool RequireValidInventoryMetadata { get; set; } = true;

        /// <summary> How many bytes to pull at a time. Lower = slower but less likely to crash </summary>
        public int MapPullChunkSize { get; set; } = 4096;

        /// <summary> Extra time to wait before game gets restarted. Possibly useful if you have to wait for the "checking if game can be played" wheel </summary>
        public int RestartGameWait { get; set; } = 0;

        /// <summary> Should we press up once before starting the game? Not guaranteed to avoid the update, but the bot will try its best. </summary>
        public bool AvoidSystemUpdate { get; set; } = true;

        #endregion

        public bool CanUseCommandUser(ulong authorId) => Users.Count == 0 || Users.Contains(authorId);
        public bool CanUseCommandChannel(ulong channelId) => Channels.Count == 0 || Channels.Contains(channelId);
        public bool CanUseSudo(ulong userId) => Sudo.Contains(userId);

        public bool GetHasRole(string roleName, IEnumerable<string> roles)
        {
            return roleName switch
            {
                nameof(RoleUseBot) => roles.Contains(RoleUseBot),
                _ => throw new ArgumentException($"{roleName} is not a valid role type.", nameof(roleName)),
            };
        }
    }
}
