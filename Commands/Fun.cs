using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace DonkBot.Commands 
{
    public class Fun : BaseCommandModule 
    {
        [Command("help")]
        [Aliases("h")]
        public async Task Help(CommandContext ctx, [Description("The command to provide help for.")] string? command = null!)
        {
            if (string.IsNullOrEmpty(command))
            {
                var commands = ctx.CommandsNext.RegisteredCommands;
                var output = "Available commands:\n";
                foreach (var cmd in commands)
                {
                    if (cmd.Key == cmd.Value.Name)
                    {
                        string Aliases = cmd.Value.Aliases.Count > 0 ? $"-{string.Join(", -", cmd.Value.Aliases)}": "";
                        output += $"-{cmd.Key}: {cmd.Value.Description} {Aliases}\n";
                    }
                }
                await ctx.RespondAsync(output);
            }
            else
            {
                await ctx.RespondAsync($"No command found with the name '{command}'.");
            }
        }   
        
        [Command("Bitrate")]
        [Aliases("b")]
        public async Task Bitrate(CommandContext ctx, string? vc = null!)
        {
            if (string.IsNullOrEmpty(vc))
            {
                if (ctx.Member!.VoiceState.Channel == null!)
                {
                    await ctx.Channel.SendMessageAsync("Which voiceChannel");
                    return;
                }
                await ctx.Channel.SendMessageAsync($"{ctx.Member.VoiceState.Channel.Bitrate.ToString()} Bits");
            }
            else
            {
                await ctx.Channel.SendMessageAsync("TODO - overloads");
            }
        }
    }
}
