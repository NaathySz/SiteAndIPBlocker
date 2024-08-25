using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Text.Json;

namespace SiteAndIPBlocker;

public class SiteAndIPBlockerConfig : BasePluginConfig
{
    [JsonPropertyName("whitelist")]
    public List<string> Whitelist { get; set; } = new List<string>();

    [JsonPropertyName("block_message")]
    public string BlockMessage { get; set; } = "{darkred}Blocked: Sending IP addresses or websites is not allowed.";
    
    [JsonPropertyName("name_action")]
    public int NameAction { get; set; } = 0;

    [JsonPropertyName("rename_message")]
    public string RenameMessage { get; set; } = "{darkred}Your name contains a blocked IP address or website. It will be renamed.";

    [JsonPropertyName("admin_immunity")]
    public int AdminImmunity { get; set; } = 0;

    [JsonPropertyName("ConfigVersion")]
    public override int Version { get; set; } = 3;
}

public class SiteAndIPBlocker : BasePlugin, IPluginConfig<SiteAndIPBlockerConfig>
{
    public override string ModuleName => "Site and IP Blocker";
    public override string ModuleVersion => "1.2.0";
    public override string ModuleAuthor => "Nathy";
    public override string ModuleDescription => "Block sites and IP addresses in chat.";

    public SiteAndIPBlockerConfig Config { get; set; } = null!;

    private static readonly Regex UrlOrIpRegex = new(@"(\b(?:https?|ftp):\/\/[^\s/$.?#].[^\s]*|(?:\d{1,3}\.){3}\d{1,3}\b)", RegexOptions.IgnoreCase);

    public void OnConfigParsed(SiteAndIPBlockerConfig config)
    {
        Config = config;
        UpdateConfig(config);
    }

    private static readonly string AssemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? "";
    private static readonly string CfgPath = $"{Server.GameDirectory}/csgo/addons/counterstrikesharp/configs/plugins/{AssemblyName}/{AssemblyName}.json";

    private static void UpdateConfig<T>(T config) where T : BasePluginConfig, new()
    {
        var newCfgVersion = new T().Version;

        if (config.Version == newCfgVersion)
            return;

        config.Version = newCfgVersion;

        var updatedJsonContent = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(CfgPath, updatedJsonContent);

        Console.WriteLine($"Config updated for V{newCfgVersion}.");
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

        if (!string.IsNullOrEmpty(message) && message[0] != ' ')
        {
            message = " " + message;
        }
        
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

        if (Config.AdminImmunity == 1 && AdminManager.PlayerHasPermissions(player, "@css/generic"))
        {
            return HookResult.Continue;
        }

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

        if (Config.AdminImmunity == 1 && AdminManager.PlayerHasPermissions(player, "@css/generic"))
        {
            return HookResult.Continue;
        }

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

    [GameEventHandler]
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        checkPlayerName(@event.Userid);
        return HookResult.Continue;
    }


    private void checkPlayerName(CCSPlayerController? player)
    {
        if (player == null || player.IsBot)
        {
            return;
        }
        string playerName = player.PlayerName;
        
        if (ContainsUrlOrIp(playerName))
        {
            if (!IsWhitelisted(playerName))
            {
                if (Config.NameAction == 0)
                {
                    NativeAPI.IssueServerCommand($"kickid {player.UserId}");
                }
                else if (Config.NameAction == 1)
                {
                    RenamePlayer(player, playerName);
                }
            }
        }
    }


    [GameEventHandler]
    public HookResult OnPlayerChangeName(EventPlayerChangename @event, GameEventInfo info)
    {
        CCSPlayerController player = @event.Userid;

        if (player == null || player.IsBot)
        {
            return HookResult.Continue;
        }
        
        string newName = @event.Newname;
        
        if (ContainsUrlOrIp(newName))
        {
            if (!IsWhitelisted(newName))
            {
                if (Config.NameAction == 0)
                {
                    NativeAPI.IssueServerCommand($"kickid {player.UserId}");
                    return HookResult.Handled;
                }
                else if (Config.NameAction == 1)
                {
                    RenamePlayer(player, newName);
                }
            }
        }
        
        return HookResult.Continue;
    }


    private void RenamePlayer(CCSPlayerController player, string playerName)
    {
        string cleanedName = UrlOrIpRegex.Replace(playerName, "").Trim();
        
        NativeAPI.IssueServerCommand($"css_rename \"{player.PlayerName}\" \"{cleanedName}\"");

        string renameMessage = ReplaceColorPlaceholders(Config.RenameMessage);
        
        player.PrintToChat(renameMessage);
    }


    private bool IsWhitelisted(string message)
    {
        string lowerCaseMessage = message.ToLowerInvariant();

        return Config.Whitelist.Any(whitelistedItem => lowerCaseMessage.Contains(whitelistedItem.ToLowerInvariant()));
    }
}
