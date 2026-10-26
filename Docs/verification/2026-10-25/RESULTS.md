# 2026-10-25 验收记录

被测源码提交：`276e047`，分支 `feature/candy-journey-review`。最后的交接提交只新增/整理文档、截图、结果和启动脚本，不修改被测游戏代码。

## 环境与范围

- Unity 2022.3.42f1c1，Windows 编辑器。
- 验证在独立 Git 审查工作区执行，重新生成了 Library 导入数据库和脚本构建缓存。
- 运行时和 Editor 程序集在每个代码提交后均使用本机 Unity 2022.3 的真实编译器与引用编译成功。
- 最终工作区执行 `JellyExperienceBatch.Run`，结果为 PASS。测试会恢复原有进度/排行榜数据。

## 已通过的检查

| 检查 | 结果 |
| --- | --- |
| 棋盘 | 200 个普通棋盘、360 个异形棋盘；连通、开局无匹配、合法交换、空洞与交叉去重 |
| 成长与解锁 | 解锁边界、自由乐园全开放、老存档、重复通关不重复加星 |
| 关卡完整流程 | 27,365 项断言通过，包含目标、动态工具边界、守护者、靶场往返与星级保存 |
| 预输入 | 下落中缓存、暂停清理、被消除棋子取消、一次扣步与棋盘同步 |
| 冰层与音效 | 下载音效引用、两段碎冰、计分与能量不重复 |
| 无限模式 | 本次 37,785 项断言通过，包含移动、蓄力跳跃、相机/鼠标释放和返场；逐帧断言数量随帧率变化 |
| UI | 按压/释放/悬停/隐藏复位、无底板区域射线、曲面 Shader 支持、忙碌时按钮外观稳定 |
| 图形资源 | 地图预览纹理随章节切换释放，临时预览对象清理，HDRI 成功导入 |
| 画面 | 1600×900 与 1280×720 实际渲染截图已检查 |
| Windows 构建 | Succeeded，StandaloneWindows64，106,523,921 bytes；编辑器退出码 0 |

精简结果：[checks.txt](checks.txt)、[runtime-result.txt](runtime-result.txt)、[campaign-result.txt](campaign-result.txt)、[input-result.txt](input-result.txt)、[feel-result.txt](feel-result.txt)、[infinite-result.txt](infinite-result.txt)。截图校验值见 [manifest.json](manifest.json)。

## 实际截图

- [首页](../../images/review-20261025/home.png)
- [章节](../../images/review-20261025/chapters.png)
- [游戏](../../images/review-20261025/board.png)
- [1280×720 游戏](../../images/review-20261025/board-1280.png)
- [新玩家第一关](../../images/review-20261025/first-level.png)

## 复现与边界

运行命令见 [团队接手说明](../../TEAM_HANDOFF.md)。最初尝试复用制作工程的 Library 时出现失效脚本引用，未把那次执行算作通过；重新导入后完整回归通过。接手时使用源码重新导入，不复制另一工程的 Library。

以上为编辑器自动流程、实际渲染检查及 Windows 构建验证。没有声称通过浏览器/WebGL、所有硬件、所有宽高比或真实鼠标手感的完整人工验收。原始编辑器日志留在本机，仓库只保存不含机器/授权信息的结果摘录。
