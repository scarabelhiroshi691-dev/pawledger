# 参与贡献

感谢关注 PawLedger。提交代码前请先在 Issue 中说明问题、使用场景和预期结果，避免重复实现。

## 本地开发

需要 .NET 10 SDK。克隆后运行：

```powershell
dotnet restore PetShopManagement.slnx
dotnet test PetShopManagement.slnx
dotnet run --project src/PetShop.Web
```

应用首次启动时会在本机创建管理员账号。测试和截图请使用虚构信息，禁止提交真实会员、手机号、宠物资料、余额、数据库或备份。

Android 店员端位于 `mobile/PawLedgerAndroid`，使用 JDK 17 与 Android SDK 35，可直接用 Android Studio 打开。

## 提交要求

- 一个 Pull Request 只解决一类问题，并说明测试结果。
- 涉及储值余额、退款、冲正或并发扣款时，必须补充自动化测试并保持流水不可被静默修改。
- 涉及权限时同时验证店长和店员两个角色。
- 不提交 `dist`、`tmp`、数据库、APK、签名文件、密钥或账号凭证。
- 不以抓包、复制 Cookie、模拟官方 App 等方式接入第三方平台。

## 授权说明

本仓库目前未附开源许可证。公开可见不等于获得复制、修改、分发或商用授权。提交 Pull Request 前，请确认你有权贡献相关代码和素材；维护者可能在未来引入明确的贡献者授权条款。
