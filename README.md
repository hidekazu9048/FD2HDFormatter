# FD2HDFormatter

USB フロッピードライブで 3.5 インチ 2HD ディスクをフォーマットする Windows のフォームアプリです。

- IBM PC 1.44MB（512 バイト×18 セクタ×80 シリンダ×2 面）
- PC-98 1.25MB（1024 バイト×8 セクタ×77 シリンダ×2 面）※3モード対応ドライブが必要
- PC-98 1.44MB（512 バイト×18 セクタ×80 シリンダ×2 面）

トラックごとにフォーマットと読み取り検査を行い、不良セクタは FAT に使用不可として登録します。結果欄に全容量・不良セクタ・使用可能容量と不良セクタの位置を表示します。

コマンドラインのフォーマッタ [format2hd](https://github.com/lpproj/mydosuty/tree/master/win/format2hd)（sava 氏作）をフォームアプリで使いたくて作ったものです。コードは独自に書いたもので、format2hd のソースは使っていません。

## ダウンロード

[Releases](https://github.com/hidekazu9048/FD2HDFormatter/releases) から `FD2HDFormatter-<版>-win-x64.zip` を取得し、展開して `FD2HDFormatter.exe` を実行してください。

- Windows 10 / 11（64 ビット）
- [.NET 10 デスクトップランタイム](https://dotnet.microsoft.com/download/dotnet/10.0)（未導入なら起動時に案内が出ます）
- USB 接続のフロッピードライブ（`\Device\Floppy*` として認識されるもの）

## 注意

フォーマットするとディスクの内容はすべて消えます。本ソフトは無保証です（LICENSE を参照）。

## ビルド

.NET 10 SDK で `dotnet build FD2HDFormatter.slnx -c Release`。

`FD2HDFormatter.Check` はメモリ上のディスクでフォーマット処理を検証するコンソールです（`FD2HDFormatter.Check <出力フォルダ> [7z.exe のパス]`）。

## ライセンス

MIT License（`LICENSE`）。アイコンは Microsoft [Fluent UI Emoji](https://github.com/microsoft/fluentui-emoji) の「Floppy disk」3D 版で、MIT License（`FD2HDFormatter/Resources/LICENSE-fluentui-emoji.txt`）です。
