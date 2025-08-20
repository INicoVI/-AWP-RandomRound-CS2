using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;
using System.Text.Json.Serialization;
using System.Linq;
using System.Collections.Generic;
using System;
using Microsoft.Extensions.Logging;

using CSTimer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace RandomRound;

public class RandomRoundConfig : BasePluginConfig
{
    [JsonPropertyName("VoteInterval")] public int VoteInterval { get; set; } = 10;
    [JsonPropertyName("VoteDuration")] public int VoteDuration { get; set; } = 30;
    [JsonPropertyName("GravityValue")] public int GravityValue { get; set; } = 200;
}

public class RoundType
{
    public string Name { get; set; } = string.Empty;
    public string Weapon { get; set; } = string.Empty;
    public bool OnlyHS { get; set; } = false;
    public bool LowGravity { get; set; } = false;
    public string DisplayName { get; set; } = string.Empty;
}

[MinimumApiVersion(210)]
public class RandomRound : BasePlugin, IPluginConfig<RandomRoundConfig>
{
    public override string ModuleName => "Rastgele Round";
    public override string ModuleVersion => "1.1.1";
    public override string ModuleAuthor => "NicoV";

    public RandomRoundConfig Config { get; set; } = null!;
    public void OnConfigParsed(RandomRoundConfig config) => Config = config;

    private int _roundCount = 0;
    private bool _isVoteActive = false;
    private Dictionary<string, int> _voteResults = new();
    private List<RoundType> _voteOptions = new();
    private CSTimer? _voteTimer;
    private bool _isSpecialRoundActive = false;
    private bool _isNoscopeRound = false;

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        RegisterEventHandler<EventWeaponZoom>(OnWeaponZoom);
        AddCommand("css_rastgeleround", "Rastgele round oylaması başlatır.", OnRandomRoundCommand);

        Logger.LogInformation("Rastgele Round Eklentisi başarıyla yüklendi.");
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (_isSpecialRoundActive)
        {
            ResetSpecialRoundSettings();
            _isSpecialRoundActive = false;
        }

        if (_roundCount > 0 && _roundCount % Config.VoteInterval == 0 && !_isVoteActive)
        {
            StartVote();
        }

        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        _roundCount++;
        _isNoscopeRound = false;
        return HookResult.Continue;
    }

    private HookResult OnWeaponZoom(EventWeaponZoom @event, GameEventInfo info)
    {
        if (!_isNoscopeRound) return HookResult.Continue;

        var player = @event.Userid;
        // 'ActiveWeapon' özelliğini 'WeaponServices' üzerinden alacak şekilde düzeltildi
        var weapon = player.Pawn?.Value?.WeaponServices?.ActiveWeapon.Value;

        if (weapon?.DesignerName == "weapon_awp")
        {
            return HookResult.Stop;
        }

        return HookResult.Continue;
    }

    private void StartVote()
    {
        if (_isVoteActive) return;

        _isVoteActive = true;
        _voteResults.Clear();
        _voteOptions.Clear();

        Server.PrintToChatAll($"{ChatColors.LightRed}---{ChatColors.White} {ChatColors.Yellow}Rastgele Round Oylaması Başlıyor! {ChatColors.White}---{ChatColors.LightRed}");
        Server.PrintToChatAll($"{ChatColors.Gold}▶ {ChatColors.Yellow}Bir sonraki tur için favori round'unuza oy verin! {ChatColors.Gold}◀");
        Server.PrintToChatAll($"{ChatColors.Silver}Oylama {Config.VoteDuration} saniye sürecektir.");

        _voteOptions.Add(new RoundType { Name = "awp_noscope", Weapon = "weapon_awp", DisplayName = $"{ChatColors.Green}AWP No Scope Round" });

        var weaponList = new List<string> { "weapon_ssg08", "weapon_ak47", "weapon_scar20", "weapon_deagle", "weapon_nova" };
        var random = new Random();

        while (_voteOptions.Count < 6 && weaponList.Count > 0)
        {
            var randomIndex = random.Next(weaponList.Count);
            var selectedWeapon = weaponList[randomIndex];
            weaponList.RemoveAt(randomIndex);

            var round = new RoundType { Name = selectedWeapon.Replace("weapon_", ""), Weapon = selectedWeapon };

            bool hasOnlyHS = random.Next(2) == 0;
            bool hasLowGravity = random.Next(2) == 0;

            var modifiers = new List<string>();
            if (hasOnlyHS) modifiers.Add($"{ChatColors.LightRed}OnlyHS{ChatColors.Default}");
            if (hasLowGravity) modifiers.Add($"{ChatColors.LightBlue}Gravity{ChatColors.Default}");

            var modifierString = modifiers.Count > 0 ? string.Join($"{ChatColors.White} + ", modifiers) + $"{ChatColors.White} + " : "";
            round.DisplayName = $"{modifierString}{ChatColors.Yellow}{round.Name.ToUpper()}{ChatColors.Default} Round";

            round.OnlyHS = hasOnlyHS;
            round.LowGravity = hasLowGravity;

            _voteOptions.Add(round);
        }

        var menu = new ChatMenu($"{ChatColors.Gold}Rastgele Round Oylaması");

        for (int i = 0; i < _voteOptions.Count; i++)
        {
            var localRoundType = _voteOptions[i];
            menu.AddMenuOption(localRoundType.DisplayName, (player, option) => OnVoteSelected(player, localRoundType.Name));
        }

        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid && !player.IsBot)
            {
                MenuManager.OpenChatMenu(player, menu);
            }
        }

        _voteTimer?.Kill();
        _voteTimer = new CSTimer(Config.VoteDuration, EndVote, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
    }

    private void EndVote()
    {
        _isVoteActive = false;

        var winningOptionName = "normal";
        int maxVotes = 0;
        var tiedWinners = new List<string>();

        foreach (var vote in _voteResults)
        {
            if (vote.Value > maxVotes)
            {
                maxVotes = vote.Value;
                tiedWinners.Clear();
                tiedWinners.Add(vote.Key);
            }
            else if (vote.Value == maxVotes && maxVotes > 0)
            {
                tiedWinners.Add(vote.Key);
            }
        }

        if (tiedWinners.Count > 0)
        {
            winningOptionName = tiedWinners[new Random().Next(tiedWinners.Count)];
        }

        if (winningOptionName != "normal")
        {
            var winner = _voteOptions.FirstOrDefault(o => o.Name == winningOptionName);
            if (winner != null)
            {
                Server.PrintToChatAll($"{ChatColors.LightRed}[Rastgele Round]{ChatColors.White} Oylama sona erdi! Kazanan: {ChatColors.Gold}{winner.DisplayName}{ChatColors.White}!");
                SetSpecialRound(winner);
            }
        }
        else
        {
            Server.PrintToChatAll($"{ChatColors.LightRed}[Rastgele Round]{ChatColors.White} Oylama sonucunda kazanan olmadı. Normal tur oynanacak.");
        }
    }

    private void OnVoteSelected(CCSPlayerController player, string optionName)
    {
        if (!_isVoteActive) return;

        if (_voteResults.ContainsKey(optionName))
        {
            _voteResults[optionName]++;
        }
        else
        {
            _voteResults.Add(optionName, 1);
        }
        Server.PrintToChatAll($"{ChatColors.LightRed}[Rastgele Round]{ChatColors.White} {player.PlayerName} {optionName.ToUpper()} için oy kullandı!");
    }

    [RequiresPermissions("@css/root")]
    private void OnRandomRoundCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (_isVoteActive)
        {
            if (player != null) player.PrintToChat($"Oylama zaten devam ediyor!");
            return;
        }

        if (player != null)
        {
            Server.PrintToChatAll($"{ChatColors.LightRed}[Avrupa Gaming]{ChatColors.White} {ChatColors.LightRed}{player.PlayerName}{ChatColors.White} isimli yetkili rastgele round oylaması başlattı.");
        }
        else
        {
            Server.PrintToChatAll($"{ChatColors.LightRed}[Avrupa Gaming]{ChatColors.White} Konsol tarafından rastgele round oylaması başlattı.");
        }

        _roundCount = Config.VoteInterval - 1;
        StartVote();
    }

    private void SetSpecialRound(RoundType winner)
    {
        AddTimer(2.0f, () => {
            Server.ExecuteCommand("mp_buy_anywhere 1");
            Server.ExecuteCommand("mp_buy_time 999");
            Server.ExecuteCommand($"sv_gravity {(winner.LowGravity ? Config.GravityValue : 800)}");
            Server.ExecuteCommand($"mp_damage_headshot_only {(winner.OnlyHS ? 1 : 0)}");

            foreach (var player in Utilities.GetPlayers().Where(p => p.IsValid && p.PawnIsAlive))
            {
                player.RemoveWeapons();
                if (winner.Weapon != string.Empty)
                {
                    player.GiveNamedItem(winner.Weapon);
                }
                player.GiveNamedItem("item_assaultsuit");
                player.GiveNamedItem("weapon_knife");
            }
            if (winner.Name == "awp_noscope")
            {
                _isNoscopeRound = true;
            }
        });
        _isSpecialRoundActive = true;
    }

    private static void ResetSpecialRoundSettings()
    {
        Server.ExecuteCommand("mp_buy_anywhere 0");
        Server.ExecuteCommand("mp_buy_time 15");
        Server.ExecuteCommand("sv_gravity 800");
        Server.ExecuteCommand("mp_damage_headshot_only 0");
    }
}