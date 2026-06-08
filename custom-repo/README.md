# Dalamud 自定义插件仓库

这个目录就是可上传的 Dalamud 自定义插件仓库。它可以同时包含多个插件，每个插件在 `plugins/` 下有自己的目录。

## 目录结构

```text
custom-repo/
  repo.json
  plugins/
    WeiyueMistakeTTS/
      latest.zip
```

## 部署方式

把 `custom-repo` 目录上传到任意可公开 HTTP 访问的静态托管服务，例如：

- GitHub Pages
- Cloudflare Pages
- Nginx 静态目录
- 腾讯云 COS / 阿里云 OSS / S3
- 内网 HTTP 服务

假设最终访问地址是：

```text
https://example.com/dalamud-plugin-repo/
```

那么需要把 `repo.json` 中的示例域名替换成：

```text
example.com/dalamud-plugin-repo
```

最终仓库地址应为：

```text
https://example.com/dalamud-plugin-repo/repo.json
```

插件包地址应为：

```text
https://example.com/dalamud-plugin-repo/plugins/WeiyueMistakeTTS/latest.zip
```

## Dalamud 添加方式

1. 打开 Dalamud 设置
2. 进入 Experimental / 实验性功能
3. 找到 Custom Plugin Repositories
4. 添加 `repo.json` 的 HTTP 地址
5. 保存并刷新插件列表
6. 搜索 `Team Mistake`
7. 安装

## 更新插件

每次更新插件时：

1. 修改代码
2. 提升 `WeiyueMistakeTTS.json` 和 `WeiyueMistakeTTS.csproj` 中的版本号
3. 执行 Release 构建
4. 用新的 `latest.zip` 覆盖：

```text
custom-repo/plugins/WeiyueMistakeTTS/latest.zip
```

5. 修改 `custom-repo/repo.json` 中的 `AssemblyVersion`
6. 上传整个 `custom-repo` 目录
