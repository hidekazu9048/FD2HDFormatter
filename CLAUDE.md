# format2hd

## 言語（最優先）

- **ユーザーに訊くときは絶対に日本語で書く。**質問・確認・選択肢（AskUserQuestion、wrap_up の質問とボタンを含む）はすべて日本語。返答・報告・要約も日本語で書く。

USB フロッピードライブで 2HD をフォーマットする C# WinForms（.NET 10、x64）ツール。要求は `docs/要求仕様.txt`。

- 形式：IBM PC 1.44MB／PC-98 1.25MB（1024 バイト×8 セクタ×77 シリンダ）／PC-98 1.44MB。
- 結果欄に全容量・不良セクタ・使用可能容量・不良セクタの位置を出す。完了・失敗はダイアログ（MessageBox のアイコンに応じた Windows 標準音）。

## 構成

- `format2hd.Core`：形式の定義（DiskFormat）、FAT12 システム領域の生成（FatBuilder）、UFI コマンド送信（UfiDevice）、フォーマット処理（Formatter）、結果の文面（ResultReport）。
- `format2hd`：GUI（アセンブリ名 `format2hd`、名前空間 `Format2hd`）。アイコンは `format2hd/Resources/`（D88ToFdd と同じ Fluent Emoji 3D「Floppy disk」の4ファイル）。
- `format2hd.Check`：検証用コンソール。メモリ上のディスク（不良セクタ・フォーマット不能トラック・ライトプロテクト・非対応ドライブを仕込める）でフォーマット処理を流し、BPB・FAT を検査、`7z l -slt` で FAT として開けて空き容量が一致するか確かめる。
  `format2hd.Check <出力フォルダ> "C:\Program Files\7-Zip\7z.exe"`

## 仕組み

- D88ToFdd と同じく `IOCTL_SCSI_PASS_THROUGH` で UFI コマンドを直接送る（sfloppy は 1.44MB しか扱わないため）。
- トラックごとに FORMAT UNIT（SingleTrack、記述子＝総ブロック数×ブロック長）→ READ(10) で検査。読めなければセクタごとに 3 回まで再試行。
- 不良セクタはそのクラスタを FAT に FF7h で登録。システム領域（ブートセクタ・FAT・ルート）に不良が出たらその時点で失敗にする。
- 最後にシステム領域を書いて読み返し照合。
- ブートセクタのプログラム：IBM は INT 10h でメッセージ→INT 16h→INT 19h。PC-98 はテキスト VRAM（A000h／属性 A200h）へ直接メッセージを書いて停止。

## 進捗

- 2026-10-28 1.0.0 作成。format2hd.Check で3形式とも全項目 OK。実機（USB FDD）での確認はまだ。
