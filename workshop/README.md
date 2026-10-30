# 创意工坊发布工作区

上传到 Steam 创意工坊的内容就是这个目录里的工作区，不需要整个项目目录。

```
workshop\
    SteamWorkspace\           ← 上传用工作区（-w 参数指向它）
        workshop.json         标题 / 描述 / 可见性 / 标签 / 依赖
        content\              要上传的 Mod 文件（manifest + dll + audio）
        image.png             创意工坊预览图（< 1MB）
        mod_id.txt            首次上传后自动生成，记录工坊物品 ID
    image.png                 预览图源文件备份
    tools\uploader\ModUploader.exe  官方上传工具（megacrit/sts2-mod-uploader v0.2.0）
    tools\make_preview.py     预览图生成脚本
```

## 上传 / 更新

```powershell
cd D:\sls2_wswjqsvoice\workshop
tools\uploader\ModUploader.exe upload -w SteamWorkspace
```

- 运行前先启动 Steam 并登录（发布者账号）。
- 首次上传会创建创意工坊物品，并把物品 ID 写入 `SteamWorkspace\mod_id.txt`，之后同一条命令即为更新。
- `workshop.json` 里的字段在上传时覆盖工坊页面内容：`title` / `description` / `visibility` / `changeNote` / `tags` / `dependencies` / `contentDescriptors`。
- `dependencies: [3747602295]` 是 RitsuLib 的工坊物品 ID。

## 更新流程

```powershell
cd D:\sls2_wswjqsvoice
.\build\build.cmd
Copy-Item .\dist\STS2-sgsdamageVoice\* .\workshop\SteamWorkspace\content\ -Recurse -Force
cd .\workshop
tools\ModUploader.exe upload -w SteamWorkspace
```

## 注意

- `image.png` 必须小于 1 MB，否则上传失败。
- 标签（`tags`）上传后不能修改，首次上传前确定好。
- 建议先 `"visibility": "private"` 自测，再在 Steam 页面上改成公开。
- 依赖项不会替订阅者自动安装，但会在页面上显示为必需项。
