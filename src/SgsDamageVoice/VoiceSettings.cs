using System;

namespace SgsDamageVoice;

/// <summary>
/// 持久化的 Mod 设置模型。必须保留无参构造函数，字段名即存档 JSON 的键名（改名会导致旧存档读不回默认值）。
/// </summary>
public sealed class VoiceSettings
{
    /// <summary>总开关。</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>我方造成伤害：低档阈值（伤害 &gt;= 该值时播放低档音效）。</summary>
    public int PlayerLowThreshold { get; set; } = 20;

    /// <summary>我方造成伤害：高档阈值（伤害 &gt;= 该值时播放高档音效）。</summary>
    public int PlayerHighThreshold { get; set; } = 50;

    /// <summary>敌方造成伤害：低档阈值。</summary>
    public int EnemyLowThreshold { get; set; } = 15;

    /// <summary>敌方造成伤害：高档阈值。</summary>
    public int EnemyHighThreshold { get; set; } = 30;

    /// <summary>音量，0-100。</summary>
    public int Volume { get; set; } = 30;

    /// <summary>true 时统计含被格挡的伤害（DamageResult.TotalDamage），false 时只统计实际掉血（UnblockedDamage）。</summary>
    public bool CountBlockedDamage { get; set; } = false;

    /// <summary>同一音效的冷却毫秒数，避免同一段伤害被重复触发时叠爆。</summary>
    public int CooldownMs { get; set; } = 120;
}
