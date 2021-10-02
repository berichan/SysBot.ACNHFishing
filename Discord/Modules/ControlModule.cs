using System;
using System.Threading;
using System.Threading.Tasks;
using ACNHMobileSpawner;
using Discord.Commands;
using SysBot.Base;

namespace SysBot.ACNHFishing
{
    // ReSharper disable once UnusedType.Global
    public class ControlModule : ModuleBase<SocketCommandContext>
    {
        [Command("detach")]
        [Summary("Detaches the virtual controller so the operator can use their own handheld controller temporarily.")]
        [RequireSudo]
        public async Task DetachAsync()
        {
            await ReplyAsync("A controller detach request will be executed momentarily.").ConfigureAwait(false);
            var bot = Globals.Bot;
            await bot.Connection.SendAsync(SwitchCommand.DetachController(), CancellationToken.None).ConfigureAwait(false);
        }

        [Command("setScreenOn")]
        [Alias("screenOn", "scrOn")]
        [Summary("Turns the screen on")]
        [RequireSudo]
        public async Task SetScreenOnAsync()
        {
            await SetScreen(true).ConfigureAwait(false);
        }

        [Command("setScreenOff")]
        [Alias("screenOff", "scrOff")]
        [Summary("Turns the screen off")]
        [RequireSudo]
        public async Task SetScreenOffAsync()
        {
            await SetScreen(false).ConfigureAwait(false);
        }

        private async Task SetScreen(bool on)
        {
            var bot = Globals.Bot;
                
            await bot.SetScreen(on, CancellationToken.None).ConfigureAwait(false);
            await ReplyAsync("Screen state set to: " + (on ? "On" : "Off")).ConfigureAwait(false);
        }
    }
}
