using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using STS2RitsuLib.Patching.Models;

namespace SgsDamageVoice;

/// <summary>
/// 伤害触发点：在 <see cref="Hook.AfterDamageGiven" /> 之后读取本次伤害结果，按阈值播放音效。
/// 该 Hook 对「击杀那一击」同样会触发，所以致死伤害也有声音。
/// 中毒、遗物、药水、能力等伤害同样经过 <c>CreatureCmd.Damage</c>，也会派发本 Hook（dealer 为 null）。
/// </summary>
public sealed class DamageVoicePatch : IPatchMethod
{
    public static string PatchId => "sgsdamagevoice_damage";

    public static string Description => "单次伤害结算后按阈值播放音效";

    /// <summary>补丁失败时不阻止游戏启动。</summary>
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
    [
        // 完整签名：AfterDamageGiven(choiceContext, combatState, dealer, results, props, target, cardSource)
        new(typeof(Hook), "AfterDamageGiven"),
    ];

    // 同一个 DamageResult 实例对应同一次伤害事件；用它做去重，避免 Hook 被多次派发时重复播放。
    private static DamageResult? _lastResult;

    // 每个音效键只在本次游戏进程里报一次「首次触发」，用来确认链路正常，又不会刷爆日志。
    private static readonly HashSet<string> AnnouncedKeys = new(StringComparer.OrdinalIgnoreCase);

    public static void Postfix(Creature? dealer, DamageResult? results, Creature? target)
    {
        try
        {
            if (target == null || results == null)
                return;

            if (ReferenceEquals(_lastResult, results))
                return;

            _lastResult = results;

            var settings = ModEntry.Settings;
            if (!settings.Enabled)
                return;

            // 只关心「我方打敌方」和「敌方打我方」两类；友军互殴、宠物等一律忽略。
            //
            // 中毒、遗物伤害、药水伤害、能力（power）伤害等走的是「没有 dealer 参数」的
            // CreatureCmd.Damage 重载，因此 Hook 里 dealer 是 null。这类伤害按「谁掉血」归属：
            // 敌方掉血记为我方输出，我方掉血记为敌方输出。
            bool playerDealt;
            if (dealer == null)
            {
                if (target.IsEnemy)
                    playerDealt = true;
                else if (target.IsPlayer)
                    playerDealt = false;
                else
                    return;
            }
            else if (ReferenceEquals(dealer, target))
            {
                // 自伤（例如卡牌的「失去生命」）不播。
                return;
            }
            else if (dealer.IsPlayer && target.IsEnemy)
                playerDealt = true;
            else if (dealer.IsEnemy && target.IsPlayer)
                playerDealt = false;
            else
                return;

            // 注意：UnblockedDamage / TotalDamage 都被目标剩余血量截断过，
            // 所以打残血目标时必须加上 OverkillDamage（溢出伤害），否则永远达不到阈值。
            var blockedDamage = Convert.ToDouble(results.BlockedDamage);
            var unblockedDamage = Convert.ToDouble(results.UnblockedDamage);
            var overkillDamage = Convert.ToDouble(results.OverkillDamage);

            var amount = (settings.CountBlockedDamage ? blockedDamage + unblockedDamage : unblockedDamage)
                         + overkillDamage;

            if (amount <= 0)
                return;

            var lowThreshold = playerDealt ? settings.PlayerLowThreshold : settings.EnemyLowThreshold;
            var highThreshold = playerDealt ? settings.PlayerHighThreshold : settings.EnemyHighThreshold;

            string? key = null;
            if (highThreshold > 0 && amount >= highThreshold)
                key = playerDealt ? VoiceAudio.KeyPlayerHigh : VoiceAudio.KeyEnemyHigh;
            else if (lowThreshold > 0 && amount >= lowThreshold)
                key = playerDealt ? VoiceAudio.KeyPlayerLow : VoiceAudio.KeyEnemyLow;

            if (key == null)
                return;

            VoiceAudio.Play(key, settings.Volume / 100f, settings.CooldownMs);

            if (AnnouncedKeys.Add(key))
            {
                var source = dealer == null ? "无来源（中毒/遗物/药水等）" : dealer.IsPlayer ? "我方" : "敌方";
                ModEntry.Logger?.Info(
                    $"[SgsDamageVoice] 首次触发 '{key}'：伤害 {amount:0.#}（格挡 {blockedDamage:0.#} / 掉血 {unblockedDamage:0.#} / 溢出 {overkillDamage:0.#}），" +
                    $"来源 {source}，阈值 低 {lowThreshold} / 高 {highThreshold}",
                    0);
            }
        }
        catch (Exception ex)
        {
            ModEntry.Logger?.Warn($"[SgsDamageVoice] 伤害触发处理失败: {ex.Message}", 0);
        }
    }
}
