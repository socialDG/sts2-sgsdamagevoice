using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;

namespace SgsDamageVoice;

/// <summary>
/// Mod 入口：注册数据存档、设置页、音频目录和伤害补丁。
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    /// <summary>Mod 唯一 id，必须和 mod_manifest.json 的 id 一致。</summary>
    public const string ModId = "STS2-sgsdamageVoice";

    /// <summary>设置页上显示的名字。</summary>
    public const string DisplayName = "三国杀无双音效";

    private const string SettingsKey = "settings";
    private const string SettingsFileName = "sgsdamagevoice_settings.json";

    /// <summary>游戏安装目录名，用来把绝对路径缩写成「Slay the Spire 2\...」。</summary>
    private const string GameFolderName = "Slay the Spire 2";

    /// <summary>RitsuLib 提供的日志器。</summary>
    public static Logger Logger { get; private set; } = null!;

    /// <summary>当前 Mod 程序集所在目录（部署后即 mods\STS2-sgsdamageVoice）。</summary>
    public static string ModDirectory { get; private set; } = "";

    private static readonly VoiceSettings FallbackSettings = new();

    /// <summary>实时读取设置对象（框架持有同一实例，改动立即生效）。</summary>
    public static VoiceSettings Settings
    {
        get
        {
            try
            {
                return RitsuLibFramework.GetDataStore(ModId).Get<VoiceSettings>(SettingsKey);
            }
            catch
            {
                return FallbackSettings;
            }
        }
    }

    public static void Initialize()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            Logger = RitsuLibFramework.CreateLogger(ModId);
            ModDirectory = ResolveModDirectory(assembly);

            Logger.Info($"[SgsDamageVoice] 启动，mod 目录 = {ModDirectory}", 0);

            // 先解析音频，设置页的「关于」才能列出实际生效的文件名。
            VoiceAudio.Initialize(ModDirectory, Logger);
            RegisterSettings();

            var patcher = RitsuLibFramework.CreatePatcher(ModId, "main", "SgsDamageVoice");
            patcher.RegisterPatch<DamageVoicePatch>();
            RitsuLibFramework.ApplyRequiredPatcher(patcher, DisableMod, "SgsDamageVoice 伤害补丁应用失败，已禁用本 Mod。");
        }
        catch (Exception ex)
        {
            Logger?.Error($"[SgsDamageVoice] 初始化失败: {ex}", 0);
        }
    }

    private static void RegisterSettings()
    {
        using (RitsuLibFramework.BeginModDataRegistration(ModId))
        {
            RitsuLibFramework.GetDataStore(ModId).Register<VoiceSettings>(
                key: SettingsKey,
                fileName: SettingsFileName,
                scope: SaveScope.Global,
                defaultFactory: () => new VoiceSettings(),
                autoCreateIfMissing: true);

            // 立刻读一次，确保文件是最新的，并让框架完成加载。
            _ = RitsuLibFramework.GetDataStore(ModId).Get<VoiceSettings>(SettingsKey);
        }

        var audioPath = DisplayPath(VoiceAudio.AudioDirectory);
        var mapText = BuildAudioMapText();

        RitsuLibFramework.RegisterModSettings(ModId, page => page
            .WithTitle(ModSettingsText.Literal(DisplayName))
            .WithModDisplayName(ModSettingsText.Literal(DisplayName))
            .WithDescription(ModSettingsText.Literal("一次造成/受到伤害达到阈值时播放对应音效。高档阈值命中时只播高档音。"))
            .AddSection("general", section => section
                .WithTitle(ModSettingsText.Literal("常规"))
                .AddToggle(
                    "enabled",
                    ModSettingsText.Literal("启用音效"),
                    Bind(s => s.Enabled, (s, v) => s.Enabled = v),
                    ModSettingsText.Literal("关闭后完全不播放任何音效。"))
                .AddIntSlider(
                    "volume",
                    ModSettingsText.Literal("音量"),
                    Bind(s => s.Volume, (s, v) => s.Volume = v),
                    0,
                    100,
                    1,
                    null,
                    ModSettingsText.Literal("0 = 静音，100 = 原始音量。"))
                .AddIntSlider(
                    "cooldown_ms",
                    ModSettingsText.Literal("同一音效冷却（毫秒）"),
                    Bind(s => s.CooldownMs, (s, v) => s.CooldownMs = v),
                    0,
                    2000,
                    10,
                    null,
                    ModSettingsText.Literal("防止短时间连续命中时音效叠爆；0 = 不节流。"))
                .AddToggle(
                    "count_blocked",
                    ModSettingsText.Literal("把被格挡的伤害也算进阈值"),
                    Bind(s => s.CountBlockedDamage, (s, v) => s.CountBlockedDamage = v),
                    ModSettingsText.Literal("开：用总伤害（含被护甲挡掉的部分）；关：只用实际掉血量。两种情况都额外计算溢出的过量伤害。")))
            .AddSection("player", section => section
                .WithTitle(ModSettingsText.Literal("我方造成伤害（打敌人）"))
                .AddIntSlider(
                    "player_low",
                    ModSettingsText.Literal("低档阈值"),
                    Bind(s => s.PlayerLowThreshold, (s, v) => s.PlayerLowThreshold = v),
                    0,
                    999,
                    1,
                    null,
                    ModSettingsText.Literal("单次伤害 ≥ 该值时播放低档音效。"))
                .AddIntSlider(
                    "player_high",
                    ModSettingsText.Literal("高档阈值"),
                    Bind(s => s.PlayerHighThreshold, (s, v) => s.PlayerHighThreshold = v),
                    0,
                    999,
                    1,
                    null,
                    ModSettingsText.Literal("单次伤害 ≥ 该值时播放高档音效（应大于低档阈值）。")))
            .AddSection("enemy", section => section
                .WithTitle(ModSettingsText.Literal("敌方造成伤害（打我方）"))
                .AddIntSlider(
                    "enemy_low",
                    ModSettingsText.Literal("低档阈值"),
                    Bind(s => s.EnemyLowThreshold, (s, v) => s.EnemyLowThreshold = v),
                    0,
                    999,
                    1,
                    null,
                    ModSettingsText.Literal("敌方单次伤害 ≥ 该值时播放低档音效。"))
                .AddIntSlider(
                    "enemy_high",
                    ModSettingsText.Literal("高档阈值"),
                    Bind(s => s.EnemyHighThreshold, (s, v) => s.EnemyHighThreshold = v),
                    0,
                    999,
                    1,
                    null,
                    ModSettingsText.Literal("敌方单次伤害 ≥ 该值时播放高档音效（应大于低档阈值）。")))
            .AddSection("about", section => section
                .WithTitle(ModSettingsText.Literal("关于"))
                .AddParagraph(
                    "about_path",
                    ModSettingsText.Literal($"音效目录（换文件后重启游戏生效）：\n{audioPath}"),
                    null)
                .AddParagraph(
                    "about_map",
                    ModSettingsText.Literal($"当前生效的音效文件：\n{mapText}"),
                    null)
                .AddParagraph(
                    "about_tip",
                    ModSettingsText.Literal($"想换成别的文件名或格式（wav / ogg / mp3），编辑同目录下的 map.txt，一行写「键 = 文件名」即可。"),
                    null)
                .AddParagraph(
                    "about_damage",
                    ModSettingsText.Literal("阈值按「这一次伤害的实际总量」判断：包含打掉的血量，也包含把目标打死后多出来的溢出伤害，所以残血目标也能正常触发。"),
                    null)));
    }

    /// <summary>把绝对路径缩写成从游戏目录名开始的形式，例如 Slay the Spire 2\mods\...\audio。</summary>
    private static string DisplayPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        var index = path.IndexOf(GameFolderName, StringComparison.OrdinalIgnoreCase);
        return index >= 0 ? path[index..] : path;
    }

    /// <summary>生成「键 = 文件名」展示文本。</summary>
    private static string BuildAudioMapText()
    {
        var lines = new List<string>();
        foreach (var key in VoiceAudio.EnumerateResolvedKeys())
        {
            var fileName = Path.GetFileName(VoiceAudio.Resolved[key]);
            lines.Add($"{key} = {fileName}");
        }

        return lines.Count > 0 ? string.Join("\n", lines) : "（未找到音频文件）";
    }

    private static ModSettingsValueBinding<VoiceSettings, T> Bind<T>(
        Func<VoiceSettings, T> getter,
        Action<VoiceSettings, T> setter)
    {
        return new ModSettingsValueBinding<VoiceSettings, T>(ModId, SettingsKey, SaveScope.Global, getter, setter);
    }

    private static string ResolveModDirectory(Assembly assembly)
    {
        try
        {
            var location = assembly.Location;
            if (!string.IsNullOrEmpty(location))
                return Path.GetDirectoryName(location) ?? AppContext.BaseDirectory;
        }
        catch
        {
            // 某些加载方式下 Location 为空，退回 BaseDirectory。
        }

        return AppContext.BaseDirectory;
    }

    private static void DisableMod()
    {
        Logger?.Error("[SgsDamageVoice] 补丁应用失败，本 Mod 已停用（不会影响游戏本体）。", 0);
    }
}
