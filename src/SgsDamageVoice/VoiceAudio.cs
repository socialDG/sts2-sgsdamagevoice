using System;
using System.Collections.Generic;
using System.IO;
using MegaCrit.Sts2.Core.Logging;
using STS2RitsuLib.Audio;

namespace SgsDamageVoice;

/// <summary>
/// 负责把「音效键」解析成磁盘上的音频文件，并通过 RitsuLib 的 GameAudioService 播放。
/// 音频以松散文件形式随 Mod 一起放在 &lt;mod 目录&gt;\audio\ 下，不需要制作 FMOD bank。
/// </summary>
internal static class VoiceAudio
{
    /// <summary>我方造成伤害：低档 / 高档。</summary>
    public const string KeyPlayerLow = "player_low";
    public const string KeyPlayerHigh = "player_high";

    /// <summary>我方受到伤害：低档 / 高档。</summary>
    public const string KeyEnemyLow = "enemy_low";
    public const string KeyEnemyHigh = "enemy_high";

    private static readonly string[] AllKeys =
    [
        KeyPlayerLow, KeyPlayerHigh, KeyEnemyLow, KeyEnemyHigh,
    ];

    private static readonly string[] SupportedExtensions = [".wav", ".ogg", ".mp3"];

    private static readonly Dictionary<string, string> ResolvedPaths = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, AudioSource> Sources = new(StringComparer.OrdinalIgnoreCase);

    private static Logger _logger = null!;

    /// <summary>音频目录（&lt;mod 目录&gt;\audio）。</summary>
    public static string AudioDirectory { get; private set; } = "";

    /// <summary>已解析出来的「音效键 → 绝对路径」映射，供设置页展示。</summary>
    public static IReadOnlyDictionary<string, string> Resolved => ResolvedPaths;

    /// <summary>依次取出已解析音效的键（顺序固定，便于展示）。</summary>
    public static IEnumerable<string> EnumerateResolvedKeys()
    {
        foreach (var key in AllKeys)
        {
            if (ResolvedPaths.ContainsKey(key))
                yield return key;
        }
    }

    /// <summary>
    /// 在 Mod 初始化阶段调用：解析 audio\map.txt（可选）并按默认命名兜底查找四个音频文件。
    /// 缺失的文件只会记日志，不会中断加载。
    /// </summary>
    public static void Initialize(string modDirectory, Logger logger)
    {
        _logger = logger;
        AudioDirectory = Path.Combine(modDirectory, "audio");
        ResolvedPaths.Clear();
        Sources.Clear();

        var overrides = ReadMapFile();

        foreach (var key in AllKeys)
        {
            var path = ResolveOne(key, overrides);
            if (path == null)
            {
                _logger.Warn($"[SgsDamageVoice] 未找到音效 '{key}'，请把文件放到 {AudioDirectory}（map.txt 或 {key}.wav/.ogg/.mp3）", 0);
                continue;
            }

            ResolvedPaths[key] = path;
            _logger.Info($"[SgsDamageVoice] 音效 '{key}' -> {path}", 0);
        }
    }

    /// <summary>播放指定音效键；文件缺失时静默跳过。</summary>
    public static void Play(string key, float volume, int cooldownMs)
    {
        try
        {
            if (!ResolvedPaths.TryGetValue(key, out var path))
                return;

            if (!Sources.TryGetValue(path, out var source))
            {
                source = AudioSource.File(ToGodotPath(path));
                Sources[path] = source;
            }

            // Scope 用 Room 而不是 Combat：Combat 作用域会在战斗结束时（CombatEndedEvent）
            // 立刻把所有句柄 StopScope 掉，导致「击杀最后一只怪物那一击」的音效刚起播就被掐断。
            // Room 只在离开房间时清理，音效可以完整播完，也不会无限累积句柄。
            var options = new AudioPlaybackOptions
            {
                Volume = Math.Clamp(volume, 0f, 1f),
                CooldownMs = cooldownMs,
                DebugName = "sgsdamagevoice:" + key,
                Scope = AudioLifecycleScope.Room,
            };

            GameAudioService.Shared.PlayOneShot(source, options);
        }
        catch (Exception ex)
        {
            _logger?.Warn($"[SgsDamageVoice] 播放音效 '{key}' 失败: {ex.Message}", 0);
        }
    }

    /// <summary>Godot 的资源路径统一使用正斜杠，这里把 Windows 反斜杠转掉。</summary>
    private static string ToGodotPath(string path) => path.Replace('\\', '/');

    private static Dictionary<string, string> ReadMapFile()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var mapPath = Path.Combine(AudioDirectory, "map.txt");
        if (!File.Exists(mapPath))
            return map;

        try
        {
            foreach (var raw in File.ReadAllLines(mapPath))
            {
                // map.txt 可能带 UTF-8 BOM，Trim 去不掉它，这里显式剥掉。
                var line = raw.Trim().TrimStart('\uFEFF').Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                var split = line.IndexOf('=');
                if (split <= 0)
                    continue;

                var key = line[..split].Trim();
                var file = line[(split + 1)..].Trim();
                if (key.Length > 0 && file.Length > 0)
                    map[key] = file;
            }
        }
        catch (Exception ex)
        {
            _logger?.Warn($"[SgsDamageVoice] 读取 {mapPath} 失败: {ex.Message}", 0);
        }

        return map;
    }

    private static string? ResolveOne(string key, Dictionary<string, string> overrides)
    {
        if (overrides.TryGetValue(key, out var mapped))
        {
            var mappedPath = Path.IsPathRooted(mapped) ? mapped : Path.Combine(AudioDirectory, mapped);
            if (File.Exists(mappedPath))
                return mappedPath;
        }

        foreach (var ext in SupportedExtensions)
        {
            var candidate = Path.Combine(AudioDirectory, key + ext);
            if (File.Exists(candidate))
                return candidate;
        }

        // 兜底：目录里任意「文件名主干一致」的文件（大小写不敏感、任意受支持扩展名）。
        if (!Directory.Exists(AudioDirectory))
            return null;

        foreach (var file in Directory.EnumerateFiles(AudioDirectory))
        {
            var stem = Path.GetFileNameWithoutExtension(file);
            if (!string.Equals(stem, key, StringComparison.OrdinalIgnoreCase))
                continue;

            var ext = Path.GetExtension(file);
            if (Array.Exists(SupportedExtensions, e => string.Equals(e, ext, StringComparison.OrdinalIgnoreCase)))
                return file;
        }

        return null;
    }
}
