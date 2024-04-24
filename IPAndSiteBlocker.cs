using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SiteAndIPBlocker;

public class SiteAndIPBlockerConfig : BasePluginConfig
{
    [JsonPropertyName("whitelist")]
    public List<string> Whitelist { get; set; } = new List<string>();
    
    [JsonPropertyName("block_message")]
    public string BlockMessage { get; set; } = " {darkred}Blocked: Sending IP addresses or websites is not allowed.";
}

public class SiteAndIPBlocker : BasePlugin, IPluginConfig<SiteAndIPBlockerConfig>
{
    public override string ModuleName => "Site and IP Blocker";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Nathy";
    public override string ModuleDescription => "Block sites and IP addresses in chat.";

    public SiteAndIPBlockerConfig Config { get; set; } = null!;

    private static readonly Regex UrlOrIpRegex = new(@"(\b(?:https?|ftp):\/\/[^\s/$.?#].[^\s]*|(?:\d{1,3}\.){3}\d{1,3}\b)", RegexOptions.IgnoreCase);

    public void OnConfigParsed(SiteAndIPBlockerConfig config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        AddCommandListener("say", OnPlayerChatAll);
        AddCommandListener("say_team", OnPlayerChatTeam);
    }

    private static readonly Dictionary<string, char> ColorMap = new Dictionary<string, char>
    {
        { "{default}", ChatColors.Default },
        { "{white}", ChatColors.White },
        { "{darkred}", ChatColors.DarkRed },
        { "{green}", ChatColors.Green },
        { "{lightyellow}", ChatColors.LightYellow },
        { "{lightblue}", ChatColors.LightBlue },
        { "{olive}", ChatColors.Olive },
        { "{lime}", ChatColors.Lime },
        { "{red}", ChatColors.Red },
        { "{lightpurple}", ChatColors.LightPurple },
        { "{purple}", ChatColors.Purple },
        { "{grey}", ChatColors.Grey },
        { "{yellow}", ChatColors.Yellow },
        { "{gold}", ChatColors.Gold },
        { "{silver}", ChatColors.Silver },
        { "{blue}", ChatColors.Blue },
        { "{darkblue}", ChatColors.DarkBlue },
        { "{bluegrey}", ChatColors.BlueGrey },
        { "{magenta}", ChatColors.Magenta },
        { "{lightred}", ChatColors.LightRed },
        { "{orange}", ChatColors.Orange }
    };

    private string ReplaceColorPlaceholders(string message)
    {
        foreach (var colorPlaceholder in ColorMap)
        {
            message = message.Replace(colorPlaceholder.Key, colorPlaceholder.Value.ToString());
        }
        return message;
    }

    private bool ContainsUrlOrIp(string message)
    {
        string cleanedMessage = Regex.Replace(message, @"\s+", ".");

        return UrlOrIpRegex.IsMatch(cleanedMessage);
    }


    private HookResult OnPlayerChatAll(CCSPlayerController? player, CommandInfo message)
    {
        if (player == null || !player.IsValid || player.IsBot || string.IsNullOrEmpty(message.GetArg(1)))
            return HookResult.Handled;

        string chatMessage = message.GetArg(1);

        if (ContainsUrlOrIp(chatMessage))
        {
            if (!IsWhitelisted(chatMessage))
            {
                string blockMessage = ReplaceColorPlaceholders(Config.BlockMessage);
                
                player.PrintToChat(blockMessage);
                return HookResult.Handled;
            }
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerChatTeam(CCSPlayerController? player, CommandInfo message)
    {
        if (player == null || !player.IsValid || player.IsBot || string.IsNullOrEmpty(message.GetArg(1)))
            return HookResult.Handled;

        string chatMessage = message.GetArg(1);

        if (ContainsUrlOrIp(chatMessage))
        {
            if (!IsWhitelisted(chatMessage))
            {
                string blockMessage = ReplaceColorPlaceholders(Config.BlockMessage);
                
                player.PrintToChat(blockMessage);
                return HookResult.Handled;
            }
        }

        return HookResult.Continue;
    }



    private bool IsWhitelisted(string message)
    {
        string lowerCaseMessage = message.ToLowerInvariant();

        return Config.Whitelist.Any(whitelistedItem => lowerCaseMessage.Contains(whitelistedItem.ToLowerInvariant()));
    }
}