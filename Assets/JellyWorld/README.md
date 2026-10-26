# Jelly World 实现说明

## 关卡、成长和存档

12 关分为云朵小径、蜜桃群岛、星糖花园，每章 4 关。地图含不可用格，匹配不跨缺口，补位按每列有效格下落。积分、指定颜色与清冰目标需同时完成。

每次基础消除计 60 分，连锁按次数加成；获胜时剩余步数每步奖励 40 分。1～3 星按通关和剩余步数比例结算。累计每关最佳星星在 0/3/9/18/27/36 星时获得对应成长称号。

| 道具 | 开放条件 | 普通关卡每局补给 |
| --- | --- | --- |
| 果冻锤 | 到达第 2 关 | 3 次 |
| 洗牌 | 到达第 3 关 | 2 次 |
| 十字星 | 到达第 4 关 | 2 次 |
| 守护者 | 到达第 6 关 | 1 次，有限时/能量 |
| 糖弹靶场 | 到达第 8 关 | 1 次，16 发 |

到达某关即通关前一关；按玩家最远解锁关卡判断，重玩早期关卡不重新锁定。自由乐园无步数、道具和弹药限制。PlayerPrefs 的原解锁、星星、最佳分与排行榜键保持兼容；没有账号或联网排行榜。

## 表现与交互

首页、顶部信息、目标和道具区域采用纯布局节点，避免嵌套浅色外框；目标数量决定道具区位置。主要按钮使用现有圆角 Sprite、曲面受光 Shader、浅侧边与接触阴影。面板撤除后，独立按钮仍保留 UI 点击拦截，空白区域不拦截。

支持悬停、按压、连续拖动和补位预输入；暂停、离开与隐藏控件会清理输入/视觉状态。碎片数量限制在预算内，可保存简洁特效设置。临时忙碌状态只阻止输入，不让整排按钮反复变平。

地图缩略图由现有网格离屏渲染；每页四张纹理，切换章节/退出时释放。临时模型与相机及时清理，layer 30 预留给地图预览。

## 模块

| 模块 | 文件 |
| --- | --- |
| 棋盘规则、地图和关卡数值 | `Scripts/JellyBoard.cs`、`JellyLevels.cs` |
| 成长与解锁条件 | `Scripts/JellyJourney.cs` |
| 主流程与输入 | `Scripts/JellyWorld.cs`、`JellyWorldInput.cs`、`JellyTile.cs` |
| 首页/章节/目标/光照 | `Scripts/JellyWorldExperience.cs` |
| 立体预览与按钮表面 | `Scripts/JellyMapPreview.cs`、`JellyUISurface.cs`、`JellyButtonFeedback.cs`、`Resources/SoftButton.shader` |
| 道具、守护者与靶场 | `Scripts/JellyWorldToolActions.cs`、`JellyGolemMode.cs`、`JellySugarRange.cs` 及对应 partial 文件 |
| 音效与碎片 | `Scripts/JellyWorldAudio.cs`、`JellyVfx.cs` |
| 验证与导出 | `Editor/*Verification.cs`、`JellyExperienceBatch.cs`、`JellyWindowsBuild.cs` |

## 资源与验证

模型、道具图标与音效复用已有项目资源；字体为 Noto Sans SC（OFL）。摄影棚反光使用 Poly Haven / Sergej Majboroda 的 Studio Small 09（CC0），见 `Resources/StudioLighting/SOURCE.md`。完整来源见仓库根目录 `THIRD_PARTY_NOTICES.md`。

运行与验证步骤见 `Docs/TEAM_HANDOFF.md`；本次分步审查与结果见 `Docs/DEVELOPMENT_2026-10-25.md` 和 `Docs/verification/2026-10-25/RESULTS.md`。截图是 Unity 实际渲染，不能代替真实鼠标手感或 WebGL 验收。
