# format2hd

USB フロッピードライブで 3.5 インチ 2HD ディスクをフォーマットする Windows アプリです。

- IBM PC 1.44MB（512 バイト×18 セクタ×80 シリンダ×2 面）
- PC-98 1.25MB（1024 バイト×8 セクタ×77 シリンダ×2 面）※3モード対応ドライブが必要
- PC-98 1.44MB（512 バイト×18 セクタ×80 シリンダ×2 面）

トラックごとにフォーマットと読み取り検査を行い、不良セクタは FAT に使用不可として登録します。結果欄に全容量・不良セクタ・使用可能容量と不良セクタの位置を表示します。

## ダウンロード

[Releases](https://github.com/hidekazu9048/format2hd/releases) から `format2hd-<版>-win-x64.zip` を取得し、展開して `format2hd.exe` を実行してください。

- Windows 10 / 11（64 ビット）
- [.NET 10 デスクトップランタイム](https://dotnet.microsoft.com/download/dotnet/10.0)（未導入なら起動時に案内が出ます）
- USB 接続のフロッピードライブ（`\Device\Floppy*` として認識されるもの）

## 注意

フォーマットするとディスクの内容はすべて消えます。本ソフトは無保証です（LICENSE を参照）。

## ビルド

.NET 10 SDK で `dotnet build format2hd.slnx -c Release`。

`format2hd.Check` はメモリ上のディスクでフォーマット処理を検証するコンソールです（`format2hd.Check <出力フォルダ> [7z.exe のパス]`）。

## ライセンス

MIT License（`LICENSE`）。アイコンは Microsoft [Fluent UI Emoji](https://github.com/microsoft/fluentui-emoji) の「Floppy disk」3D 版で、MIT License（`format2hd/Resources/LICENSE-fluentui-emoji.txt`）です。
