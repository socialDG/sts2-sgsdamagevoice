# 三国杀无双音效（STS2-sgsdamageVoice）

《Slay the Spire 2》音效 Mod：**单次伤害**达到设定阈值时播放对应音效。

| 项目 | 值 |
| --- | --- |
| Mod id / 程序集 / 目录名 | `STS2-sgsdamageVoice` |
| 显示名 | 三国杀无双音效 |
| 版本 | 0.2.3 |
| 依赖 | RitsuLib ≥ 0.6.0（`STS2-RitsuLib`） |
| 游戏版本 | 0.105.0 及以上（开发验证于 0.111.0） |

---

## 一、功能

| 触发情形 | 伤害 ≥ 低档阈值 | 伤害 ≥ 高档阈值 |
| --- | --- | --- |
| 我方对敌方造成伤害 | 播放 `player_low` | 播放 `player_high` |
| 敌方对我方造成伤害 | 播放 `enemy_low` | 播放 `enemy_high` |

- 一次伤害事件最多播放一个音效：先判高档，达到高档就只播高档，**不叠播**低档。
- 只处理「我方 ↔ 敌方」的伤害；自伤、敌方互殴、我方互殴都不触发。
- 覆盖面不限于攻击牌：**中毒**、遗物伤害、药水伤害、能力（power）伤害、以及其他 Mod 施加的持续伤害状态，只要最终走游戏的伤害结算，都会触发。
- 击杀最后一只怪物、战斗结束的那一击同样会触发。
- 灾厄（Doom）一类「直接消灭」的效果不产生伤害数字，因此不触发音效。
- 阈值、音量、判定口径都在游戏内的 Mod 设置页里改，即时生效并自动存档。

## 二、安装

Mod 必须放在**游戏可执行文件旁的 `mods` 目录**（游戏只扫描这里和 Steam 创意工坊）：

```
Slay the Spire 2\mods\STS2-sgsdamageVoice\
    mod_manifest.json
    STS2-sgsdamageVoice.dll
    audio\
        map.txt
        playerattack2enemy_1.mp3      -> player_low
        playerattack2enemy_2.mp3      -> player_high
        enemyattack2player_1.mp3      -> enemy_low
        enemyattack2player_2.mp3      -> enemy_high
```

完整路径：`D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\STS2-sgsdamageVoice\`

设置存档：`%APPDATA%\SlayTheSpire2\steam\<steamid>\mod_data\STS2-sgsdamageVoice\sgsdamagevoice_settings.json`

`%APPDATA%\SlayTheSpire2\mods` 不是 Mod 加载目录，放在那里不会生效。

## 三、游戏内设置

入口：主菜单 patch notes 区域的 Mod 设置按钮，或暂停菜单里的 Mod 设置，页面标题「三国杀无双音效」。

| 设置项 | 默认 | 范围 | 说明 |
| --- | --- | --- | --- |
| 启用音效 | 开 | — | 总开关 |
| 音量 | 30 | 0–100 | 播放音量百分比 |
| 同一音效冷却（毫秒） | 120 | 0–5000 | 同一音效键在该时间内只播一次，0 = 不节流 |
| 把被格挡的伤害也算进阈值 | 关 | — | 关 = 只算实际掉血（+溢出）；开 = 把被格挡的部分也算进去 |
| 我方 低档阈值 | 20 | 0–9999 | 我方打敌人 |
| 我方 高档阈值 | 50 | 0–9999 | 我方打敌人 |
| 敌方 低档阈值 | 15 | 0–9999 | 敌人打我方 |
| 敌方 高档阈值 | 30 | 0–9999 | 敌人打我方 |

阈值设为 0 表示该档永不触发（高档位设 0 即只剩低档音效）。

「关于」区块会显示当前的音效目录（从 `Slay the Spire 2\` 起算）和实际生效的四个音效文件名。

## 四、阈值判定口径

判定用的是「这一次伤害的实际总量」：

```
关「把被格挡的伤害也算进阈值」：  amount = 掉血 + 溢出
开「把被格挡的伤害也算进阈值」：  amount = 格挡 + 掉血 + 溢出
```

游戏里 `UnblockedDamage`（掉血）会被目标剩余血量截断，被截掉的部分记在 `OverkillDamage`（溢出）里，所以两者相加才是这一下真正的伤害数字。这样即使是残血目标（血量低于阈值）吃到一击必杀，也能正常触发音效。

中毒这类「失去生命」的伤害没有格挡分量，走的是同一条结算路径，`amount` 就等于这次扣掉的血（含溢出）。

`amount ≥ 阈值` 即触发。

## 五、音效文件

- 直接替换 `audio\` 里的文件即可，保持文件名不变；替换后需要重启游戏。
- 想换文件名或换格式：改 `audio\map.txt`，一行一个 `键 = 文件名`，支持 `#` 注释：

```
player_low  = playerattack2enemy_1.mp3
player_high = playerattack2enemy_2.mp3
enemy_low   = enemyattack2player_1.mp3
enemy_high  = enemyattack2player_2.mp3
```

- `map.txt` 缺失或不完整时，按 `player_low.wav` / `.ogg` / `.mp3` 这类默认名兜底查找（文件名大小写不敏感）。
- 支持格式：wav / ogg / mp3，作为松散文件按绝对路径播放，不需要制作 FMOD bank。
- 缺少某个音效文件时，该档静默跳过，不影响游戏。

## 六、编译与部署

```powershell
# 编译 + 部署到游戏 mods 目录（游戏必须已退出）
.\build\build.cmd

# 只编译到 dist\，不部署
.\build\build.cmd -NoDeploy
```

`build.cmd` 用 `-ExecutionPolicy Bypass` 调用 `build.ps1`（本机执行策略禁止直接运行脚本），编译完成后自动复制到游戏 `mods` 目录，并清理旧 id 的部署目录。

编译不依赖 .NET SDK：使用 `.tools\` 内的便携 `csc.exe`（Roslyn 5.9.0）+ `Microsoft.NETCore.App.Ref` 9.0.22 引用程序集，目标框架 `net9.0`。

`build.ps1` 顶部把游戏目录与 RitsuLib 目录写成了本机绝对路径（`$GameDir` / `$RitsuDir`），换机器时按自己 Steam 库的位置改这两处即可。

## 七、目录结构

```
src\SgsDamageVoice\   VoiceSettings.cs / VoiceAudio.cs / ModEntry.cs / DamageVoicePatch.cs / mod_manifest.json
build\build.ps1       编译 + 打包 + 部署
build\obj\            csc 响应文件等中间产物
dist\STS2-sgsdamageVoice\        部署源目录（= 要上传/分发的 Mod 内容）
dist\STS2-sgsdamageVoice-*.zip   给朋友的分发包
workshop\             创意工坊发布用工作区（SteamWorkspace\ + 工具）
assets\               原始音频
.tools\               便携编译器 + net9 引用程序集 + MdDump 元数据查看器
_refs\RitsuLib\       RitsuLib 源码（查 API 用）
README.md
```

## 八、实现说明

- **挂点**：`MegaCrit.Sts2.Core.Hooks.Hook.AfterDamageGiven(choiceContext, combatState, dealer, results, props, target, cardSource)` 的 Harmony Postfix，按参数名注入 `dealer / results / target`。该 Hook 对击杀那一击也会派发；游戏内所有伤害（含无来源的 HP 流失）都从 `CreatureCmd.Damage` 统一派发，所以中毒、遗物、药水、能力伤害一并覆盖。
- **阵营判定**：
  - `dealer.IsPlayer && target.IsEnemy` → 我方造成；`dealer.IsEnemy && target.IsPlayer` → 我方受到；其余（自伤、敌方互殴）忽略。
  - 中毒、遗物、药水、能力等走的是「无 dealer」的 `CreatureCmd.Damage` 重载，此时 `dealer == null`，按「谁掉血」归属：敌方掉血记为我方输出，我方掉血记为敌方输出。
- **去重**：同一个 `DamageResult` 实例代表同一次伤害，用引用相等去重，避免 Hook 多次派发造成重复播放。
- **播放**：`GameAudioService.Shared.PlayOneShot(AudioSource.File(<绝对路径>), new AudioPlaybackOptions { Volume, CooldownMs, DebugName, Scope = AudioLifecycleScope.Room })`。
  用 `Room` 而不是 `Combat`：`Combat` 作用域会在战斗结束时立刻清理句柄，导致击杀最后一只怪物的音效刚起播就被掐断；`Room` 只在离开房间时清理。
- **持久化**：`ModDataStore.Register<VoiceSettings>("settings", "sgsdamagevoice_settings.json", SaveScope.Global, ...)`，设置页用 `ModSettingsValueBinding` 双向绑定。
- 补丁 `IsCritical = false`：即使将来游戏更新导致该 Hook 变化，补丁只会应用失败并写日志，不会阻止游戏启动。
- 每个音效键在每次游戏进程里首次触发时写一条日志（含格挡 / 掉血 / 溢出三个分量），便于核对阈值口径。

## 九、排错

日志：`%APPDATA%\SlayTheSpire2\logs\godot.log`，搜索 `SgsDamageVoice`。

正常启动关键行：

```
Found mod manifest file ...\mods\STS2-sgsdamageVoice\mod_manifest.json
Calling initializer method of type SgsDamageVoice.ModEntry for STS2-sgsdamageVoice
[SgsDamageVoice] 音效 'player_low'  -> ...\audio\playerattack2enemy_1.mp3
[Patcher - SgsDamageVoice] Patch application complete: 1 applied, 0 ignored, 0 failed, 1 total
```

阈值核对日志（每键每进程一次）：

```
[SgsDamageVoice] 首次触发 'player_low'：伤害 24（格挡 0 / 掉血 19 / 溢出 5），来源 我方，阈值 低 20 / 高 50
```

| 现象 | 排查方向 |
| --- | --- |
| 完全没声音 | 日志里四行音效是否都解析成功；游戏内音量设置是否 > 0；`map.txt` 指向的文件是否存在 |
| 伤害够高却不响 | 看「首次触发」日志里的「伤害」数值，确认口径与阈值；确认是「我方↔敌方」的伤害 |
| 只响一次后不再响 | 「同一音效冷却」设得过大，或该场战斗已经播过一次（冷却按音效键计时） |
| 击杀最后一只怪物不响 | 0.2.1 已修复（改用 `Room` 生命周期作用域）；确认部署的是最新 dll |
| 中毒 / 遗物 / 药水伤害不响 | 0.2.3 起支持「无来源伤害」（`dealer == null` 时按掉血方归属）；确认部署的是 0.2.3 以上 dll |
| Mod 未出现在设置页 | 确认放在游戏 exe 旁的 `mods` 目录；确认 RitsuLib 已安装且版本 ≥ 0.6.0 |

## 十、发布与分发

### 发给朋友（私下分发包）

直接发 `dist\STS2-sgsdamageVoice-0.2.3.zip`，对方解压到 `Slay the Spire 2\mods\` 下，最终结构必须是：

```
Slay the Spire 2\mods\STS2-sgsdamageVoice\mod_manifest.json
Slay the Spire 2\mods\STS2-sgsdamageVoice\STS2-sgsdamageVoice.dll
Slay the Spire 2\mods\STS2-sgsdamageVoice\audio\...
```

对方必须先安装 RitsuLib（创意工坊订阅 `STS2-RitsuLib`），否则 Mod 不会加载。

### 发布到 Steam 创意工坊

上传内容 = `dist\STS2-sgsdamageVoice\` 里的东西（`mod_manifest.json` + dll + `audio\`），**不是**整个项目目录。
已备好工作区 `workshop\SteamWorkspace\`（`workshop.json` + `content\` + `image.png`），工具在 `workshop\tools\`。

```powershell
cd D:\sls2_wswjqsvoice\workshop
tools\uploader\ModUploader.exe upload -w SteamWorkspace
```

- 上传工具是官方 `megacrit/sts2-mod-uploader`（v0.2.0），运行前需要 Steam 客户端已登录（用发布者的 Steam 账号）。
- `workshop.json` 里 `dependencies` 填创意工坊物品 ID：`3747602295` 即 RitsuLib；`mod_manifest.json` 里另有 `STS2-RitsuLib ≥ 0.6.0` 的加载期依赖声明。
- `image.png` 是创意工坊预览图，必须小于 1 MB。
- 首次上传后工作区会生成 `mod_id.txt`（记录工坊物品 ID）；以后更新沿用同一条命令，`changeNote` 写更新说明。
- 标签（`tags`）上传后无法再改，首次上传前确认好；可见性建议先 `private` 自测，在 Steam 页面上改成 `public`。
- 更新流程：`.\build\build.cmd` 重新编译 → 把 `dist\STS2-sgsdamageVoice\*` 覆盖到 `workshop\SteamWorkspace\content\` → 再跑一次上传命令。

## 十一、托管到 GitHub（源码仓库）

本目录已经是一个 git 仓库（`main` 分支，首次提交完成）。把它推到你的 GitHub 账号只需要两步。

### 1. 在 GitHub 建空仓库

打开 https://github.com/new ，仓库名建议 `sts2-sgsdamagevoice`，可见性随意（Public 任何人都能看源码，Private 只有你自己），**不要**勾选 Add README / Add .gitignore / Choose a license —— 本地已经有这些文件，勾了会在 push 时冲突。

### 2. 关联并推送

把 `<你的用户名>` 换成你的 GitHub 用户名，然后在本目录执行：

```powershell
cd D:\sls2_wswjqsvoice
git remote add origin https://github.com/<你的用户名>/sts2-sgsdamagevoice.git
git push -u origin main
```

首次 push 会弹出浏览器要求登录 GitHub（本机已装 Git Credential Manager），登录成功后凭据会被记住，以后不用再登。如果之前加过别的 remote，用 `git remote set-url origin <地址>` 覆盖。

之后每次改完代码：

```powershell
git add -A
git commit -m "说明这次改了什么"
git push
```

### 关于仓库内容

`.gitignore` 已排除这几样（体积大、可重新获取，不属于源码）：

| 路径 | 原因 |
| --- | --- |
| `.tools\` | 便携编译器 + net9 引用程序集，约 134 MB |
| `_refs\` | RitsuLib 参考源码，需要时重新 clone |
| `workshop\tools\uploader\`、`ModUploader-win-x64.zip` | 官方上传工具，约 21 MB |
| `mod-uploader.log`、`workshop\SteamWorkspace\mod_id.txt` | 本机上传状态 |
| `.acl-recovery\` | 本机一次性修复残留 |

仓库实际内容约 1.7 MB：源码、构建脚本、4 个音效、成品 dll、分发包 zip、创意工坊工作区。

想把可执行文件也做成「下载即用」，在 GitHub 上建一个 Release（`Releases` → `Draft a new release`，tag 用 `v0.2.3`），把 `dist\STS2-sgsdamageVoice-0.2.3.zip` 拖进附件即可。