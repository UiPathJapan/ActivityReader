# ActivityReader

ActivityReader はふたつの NuGet パッケージの間のアクティビティクラスおよびプロパティについて差分を標準出力にプリントするコンソールアプリケーションです。

### 使用例

例えば、UiPath.Excel.Activities.2.4.6884.25683.nupkg と UiPath.Excel.Activities.2.5.1.nupkg の差分を調べる場合、コマンドプロンプトで次のように実行します。

```
ActivityReader.exe -diff UiPath.Excel.Activities.2.4.6884.25683.nupkg UiPath.Excel.Activities.2.5.1.nupkg
```

また、旧 Core パッケージと新しいの System および UIAutomation との差分を調べる場合、次のように実行します。

```
ActivityReader.exe -diff UiPath.Core.Activities.18.2.6859.24610.nupkg UiPath.System.Activities.18.4.2.nupkg UiPath.UIAutomation.Activities.18.4.3.nupkg
```

単にクラスとプロパティをリストする場合、次のように実行します。

```
ActivityReader.exe -print UiPath.Mail.Activities.1.3.0.nupkg
```

### 依存ライブラリ

ActivityReader は、Mono.Cecil NuGet パッケージを使用してアセンブリからクラスとプロパティを読み出しています。
