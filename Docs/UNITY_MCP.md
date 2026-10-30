# Unity MCP 编辑器连接

工程通过 Unity Package Manager 安装 `com.coplaydev.unity-mcp`，固定 Git 标签 `v10.2.0`。插件用于编辑器自动化，游戏运行和构建产物不依赖本地 MCP 服务。

1. 安装 Python 3.10 或更高版本以及 UV，确认 `uv --version` 可运行。若 Unity 未找到 UV，在 MCP 窗口中指定本机 `uv.exe` 的完整路径。
2. 启动与插件匹配的本地服务：

   ```powershell
   uvx --from mcpforunityserver==10.2.0 mcp-for-unity --transport http --http-host 127.0.0.1 --http-port 8080
   ```

3. 打开 Unity 的 MCP for Unity 窗口，选择本地 HTTP，地址 `http://127.0.0.1:8080`，连接编辑器会话。
4. MCP 客户端地址为 `http://127.0.0.1:8080/mcp`。通过 `mcpforunity://instances` 确认目标为 `ruanjian`，再执行刷新、菜单和运行检查。

编辑器首次安装包和脚本重新编译时，会话会短暂断开；等待编译结束再继续。个人 UV 路径、服务日志和工作站辅助脚本不应提交到仓库。
