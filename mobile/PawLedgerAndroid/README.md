# 爪账 Android 店员端

这是店内局域网客户端，不复制或同步主数据库。首次打开时输入 Windows 主机“管理”页显示的地址；省略端口时自动使用 `5088`。

## 构建

需要 JDK 17、Android SDK 35、Android Build Tools 35.0.0 和 Android Gradle Plugin 8.7.3。用 Android Studio 打开本目录，等待 Gradle 同步后运行 `app` 配置；也可以在终端执行：

```powershell
.\gradlew.bat assembleDebug lintDebug
```

仓库自带 Gradle 8.9 Wrapper，首次执行会下载对应 Gradle 发行包。

当前仓库不提交 APK、发布签名或 `local.properties`。如需对外分发，请先创建独立且妥善保管的签名密钥，并按 Android 官方发布流程生成 release 包。

## 安全边界

客户端默认允许通过 HTTP 访问门店电脑，只适用于可信的门店私有 Wi-Fi。WebView 只在已配置的主机内导航、拒绝无效 HTTPS 证书、不接受第三方 Cookie；不要把电脑端口映射到互联网。
