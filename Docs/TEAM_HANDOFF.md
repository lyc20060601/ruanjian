# 团队接手说明

## 交接状态

本次上传的是 Unity 源码、场景、素材及配置，不包含个人作业提交包、缓存或账号信息。仓库根目录可直接由 Unity Hub 打开。

项目版本为 Unity 6.3 LTS `6000.3.24f1`。尚未生成可运行的 WebGL 构建。**源码上传不等于网页交付完成。**

## 本分支的 Windows 试玩与自动验收

当前交付位于 `main`；糖果旅程的原开发任务与逐步提交见 [审查顺序](DEVELOPMENT_2026-10-25.md)，大地图和 Unity 6 升级来自 `feature/unique-level-maps-unity6`。

Windows：退出 Play Mode，执行 `Jelly World > Build Windows playtest`，输出 `Builds/CandyOpen/JellyWorld.exe`。仓库不包含构建产物，首次需自行构建；构建后双击根目录 `试玩明亮糖果.bat`。

在仓库根目录使用 PowerShell，先把 `$unityEditorPath` 设为本机对应版本 `Unity.exe` 的完整路径：

```powershell
# 规则与解锁边界
& $unityEditorPath -batchmode -quit -projectPath "$PWD" -executeMethod JellyExperienceVerification.VerifyRules -logFile "Logs/rules.log"
# 完整运行回归：不要添加 -quit，验证程序进入 Play Mode，恢复原档后自行退出
& $unityEditorPath -batchmode -projectPath "$PWD" -executeMethod JellyExperienceBatch.Run -logFile "Logs/playmode.log"
# Windows 构建
& $unityEditorPath -batchmode -quit -projectPath "$PWD" -executeMethod JellyWindowsBuild.Build -logFile "Logs/build.log"
```

完整回归报告和实际渲染截图输出到 `Captures/CandyOpen`。测试暂时设置 QA 存档并在结束后恢复；不要在同一工程的另一个编辑器中同时执行。首次导入需等待包与资源还原。

## 建议分工

| 工作 | 主要入口 | 接手目标 |
| --- | --- | --- |
| 网页构建与发布 | `Editor/JellyWebBuild.cs` | 安装模块、构建、浏览器验收、发布 URL |
| 成长与解锁 | `JellyJourney.cs`、`JellyWorldExperience.cs` | 解锁边界、最佳星星累计、章节与存档兼容 |
| 关卡与难度 | `JellyLevels.cs`、`JellyBoard.cs`、`JellyWorldCampaign.cs` | 调整难度，确认所有关卡可通关 |
| UI 与输入 | `JellyWorld.cs`、`JellyWorldInput.cs`、`JellyWorldToolPool.cs` | 分辨率适配、拖动、暂停体验 |
| 道具与场景 | `JellyWorldToolActions.cs`、`JellyGolemMode.cs`、`JellySugarRange.cs` | 边界、场景往返、计分同步 |
| 图形与预览 | `JellyUISurface.cs`、`JellyMapPreview.cs`、`Resources/SoftButton.shader` | 按钮曲面、预览纹理释放、信息区清晰度 |
| 音效与素材 | `JellyWorldAudio.cs`、`Audio/SOURCES.md` | 网页音频、音量、素材来源 |
| 验收 | `Editor/*Verification.cs` | 自动回归与实际浏览器、人工验收 |

上述目录都在 `Assets/JellyWorld` 内，运行时代码在 `Scripts` 内。12 关地图模板定义在 `JellyLevels.cs` 的 `MapRows` 中：`#` 表示有效格，`.` 表示缺口；当前尺寸为 8×8～12×12。

### 大地图接手注意事项

- 修改 `MapRows` 时，每行长度和行数必须与关卡 `Size` 一致。
- 新轮廓应保持四方向连通，并保留足够的连续三格区域供匹配生成。
- 同步复核步数、目标分数、冰层数量、颜色收集数量和章节道具解锁阶段。
- 镜头会按 `BoardSize` 自动取景；超过当前 12×12 上限时，需要同时检查顶部信息和右侧道具区遮挡。
- 提交前执行 [大地图验证流程](LARGE_MAPS.md#修改后的验证步骤) 和完整 Play Mode 回归。

## Git 协作

```bash
git pull --ff-only
git switch -c feature/你的功能名
# 修改和验证
git add Assets Packages ProjectSettings
git commit -m "说明具体变化"
git push -u origin feature/你的功能名
```

然后创建 Pull Request 合入 `main`。

- 先约定场景／预制体负责人，避免多人同时修改同一文件。
- 资源和 `.meta` 一起提交；移动资源优先在 Unity 内操作，不要重新生成旧 GUID。
- Unity 已启用 Visible Meta Files 和文本序列化。
- 包版本变更时同时提交 `manifest.json` 与 `packages-lock.json`。
- 已移除运行游戏不需要的 Unity MCP Git 包；`Newtonsoft Json` 使用 Unity Registry 依赖。包版本变更需同步提交清单和锁文件。

## 导出 WebGL

1. Unity Hub → 对应编辑器 → Add modules → WebGL Build Support。
2. 打开工程，等待编译完成，退出 Play Mode。
3. 执行 `Jelly World > Build WebGL`，输出到 `Builds/WebGL`。
4. 从仓库根目录启动本地 HTTP 服务：

   ```bash
   python -m http.server 8080 --directory Builds/WebGL
   ```

5. 浏览器访问 `http://localhost:8080`，不要直接双击 HTML。

当前导出脚本关闭压缩，便于先在普通静态服务器使用。后续开启压缩时，应同步配置 Content-Encoding、MIME 或 Unity 解压回退。

## GitHub Pages 发布

将生成的 **`Builds/WebGL` 内容** 放到独立 `gh-pages` 分支的根目录，根目录应直接有 `index.html`、`Build/` 等文件，并添加空的 `.nojekyll`。

仓库 Settings → Pages 中选择从 `gh-pages` 分支根目录发布。Pages 可用性以仓库设置页和账号计划为准；成功后使用页面给出的实际 URL。`main` 保留源码，发布分支只放静态构建。

没有成功构建和部署前，不要把仓库链接标成浏览器游玩链接。

## 网页发布前验收

- 首次加载、中文字体、首次交互后的音频播放。
- 胜利、失败、解锁、重开、刷新后的存档恢复。
- 所有道具、两段碎冰、靶场往返、得分与棋盘一致。
- 按住拖动、动画期间预输入、暂停、鼠标锁定与释放。
- 常用桌面分辨率。目前 WASD/鼠标玩法还不是完整的手机适配。
- 排行榜仅保存在本机；是否增加联网排名或分享，需小组另定范围。

历史自动报告不能替代实际 WebGL 和人工验收。

## 字体与素材

仓库分发版以 OFL 授权的 Noto Sans SC 替换本机 Windows 字体，保留文件名与 Unity GUID；字形可能与制作机略有不同。来源与许可证在 `Assets/JellyWorld/Fonts`。

`rounded_cube.glb` 来自用户提供，目前缺原始下载页／授权记录，由小组正式对外发布前补齐。其他来源见根目录 `THIRD_PARTY_NOTICES.md`。新资源继续遵循 `AGENTS.md`。
