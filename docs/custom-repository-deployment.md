# Weiyue Mistake TTS 自定义仓库部署说明

## 1. 当前可用文件

插件包：

```text
E:\win_c\Desktop\weiyue\custom-repo\plugins\WeiyueMistakeTTS\latest.zip
```

仓库清单：

```text
E:\win_c\Desktop\weiyue\custom-repo\repo.json
```

GitHub Raw 仓库地址：

```text
https://raw.githubusercontent.com/skymansoul/weiyue-mistake-tts/main/custom-repo/repo.json
```

Dalamud 中添加上面这个地址即可。

## 2. 推荐发布方式

最省事的方式是静态托管：

- GitHub Pages：适合公开分发
- Cloudflare Pages：适合免费静态托管
- Nginx：适合自己服务器
- COS/OSS/S3：适合对象存储

Dalamud 需要能直接访问 `repo.json` 和 `latest.zip`。

## 3. GitHub Pages 示例

假设你创建一个 GitHub 仓库：

```text
weiyue-mistake-tts-repo
```

把 `custom-repo` 里的内容提交到仓库根目录：

```text
repo.json
plugins/WeiyueMistakeTTS/latest.zip
```

开启 GitHub Pages 后，地址可能是：

```text
https://你的用户名.github.io/weiyue-mistake-tts-repo/
```

那么 `repo.json` 里的地址改成：

```json
"RepoUrl": "https://你的用户名.github.io/weiyue-mistake-tts-repo",
"DownloadLinkInstall": "https://你的用户名.github.io/weiyue-mistake-tts-repo/plugins/WeiyueMistakeTTS/latest.zip",
"DownloadLinkUpdate": "https://你的用户名.github.io/weiyue-mistake-tts-repo/plugins/WeiyueMistakeTTS/latest.zip",
"DownloadLinkTesting": "https://你的用户名.github.io/weiyue-mistake-tts-repo/plugins/WeiyueMistakeTTS/latest.zip"
```

队友在 Dalamud 里添加：

```text
https://你的用户名.github.io/weiyue-mistake-tts-repo/repo.json
```

## 4. 本地内网测试

如果只是自己测试，可以在 `custom-repo` 目录启动 HTTP 服务：

```powershell
cd E:\win_c\Desktop\weiyue\custom-repo
python -m http.server 8080
```

然后把 `repo.json` 里的地址改成：

```json
"RepoUrl": "http://127.0.0.1:8080",
"DownloadLinkInstall": "http://127.0.0.1:8080/plugins/WeiyueMistakeTTS/latest.zip",
"DownloadLinkUpdate": "http://127.0.0.1:8080/plugins/WeiyueMistakeTTS/latest.zip",
"DownloadLinkTesting": "http://127.0.0.1:8080/plugins/WeiyueMistakeTTS/latest.zip"
```

Dalamud 添加：

```text
http://127.0.0.1:8080/repo.json
```

固定队其他人不能用 `127.0.0.1` 访问你的电脑。给别人用时，需要公网地址或内网可访问地址。

## 5. 更新流程

1. 修改代码
2. 更新版本号
3. 重新构建：

```powershell
dotnet build -c Release -p:DalamudLibPath="C:\Users\lenovo\AppData\Roaming\XIVLauncherCN\addon\Hooks\26-06-03-01\"
```

4. 覆盖插件包：

```text
custom-repo/plugins/WeiyueMistakeTTS/latest.zip
```

5. 修改 `custom-repo/repo.json` 里的 `AssemblyVersion`
6. 重新上传

## 6. 检查清单

- [ ] `repo.json` 能用浏览器打开
- [ ] `latest.zip` 能用浏览器下载
- [ ] `repo.json` 中三个 DownloadLink 都是 HTTP/HTTPS 地址
- [ ] `AssemblyVersion` 和插件包中的版本一致
- [ ] `DalamudApiLevel` 是当前目标 API Level
- [ ] 队友添加的是 `repo.json` 地址，不是 zip 地址
