using Discord.Commands;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    public class HelloModule : ModuleBase<SocketCommandContext>
    {
        [Command("hello")]
        [Alias("hi")]
        [Summary("Say hello to the bot and get a response.")]
        public async Task PingAsync()
        {
            var str = SysCordSettings.Settings.HelloResponse;
            var str2 = SysCordSettings.Settings.HelloResponse2;
            var msg = string.Format(str, Context.User.Mention);
            var msg2 = string.Format(str2, Context.User.Mention);
            await ReplyAsync(msg).ConfigureAwait(false);
            if (str2 != "") await ReplyAsync(msg2).ConfigureAwait(false);
        }
    }
}