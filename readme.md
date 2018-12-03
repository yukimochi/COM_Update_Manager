# COM Update Manager
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Fyukimochi%2FCOM_Update_Manager.svg?type=shield)](https://app.fossa.io/projects/git%2Bgithub.com%2Fyukimochi%2FCOM_Update_Manager?ref=badge_shield)


## 概要
このツールは、カスタムメイド3D2・カスタムオーダーメイド3D2のDLC,プラグイン,アップデートを1つにまとめて、インストールファイルの軽量化と手間の軽減を図るソフトウェアです。
SP+メーカー([A.K.Office]( http://www.ak-office.jp/software/winsppm.html)様)のようなプロダクトを目指して開発しました。

現在、zipアーカイブ・isoアーカイブ・解凍されたプラグインに対応しています。

## 使い方
### プラグインフォルダの準備
#### DLC,アップデート,プラグインの場合
 1. カスタムメイド3D2・カスタムオーダーメイド3D2のプラグイン・アップデートのzipファイル・isoファイル・解凍済みのディレクトリ(自己解凍書庫の場合)を1つのフォルダにまとめて格納してください。
 （このまとめたフォルダをプラグインフォルダとします。）
 
#### DVDに収録されるタイプのプラグイン(追加性格,ビジュアルパック,プラス)の場合
 1. プラグインフォルダの下にプラグインごとに新規フォルダを作成します。
 2. そのフォルダにDVDの内容をすべてコピーします。
 
### COM Update Manager の操作
1. プラグインフォルダを"Select Source"をクリックして指定します。
2. 出力先のフォルダを"Select Destination"をクリックして指定します。
3. 抽出したいアップデータの対象を CM3D2{x86,x64},CM3D2OH{x86,x64}(Chu-B Lip),COM3D2,COM3DOH(ChuB-B Lip) の中からセレクトボックスで選択します。
4. "Scan" をクリックして、統合するファイルのスキャンを行います。
5. "Copy"をクリックして、結合処理を行います。

※ COM3D2向けのCM3D2向けプラグインディスクによる認証がいるプラグインには、未対応です。

## 取得
- GitHub release からダウンロードします。
- 書庫を解凍し、 COM-UM-WPFUI.exe を実行します。


## License
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Fyukimochi%2FCOM_Update_Manager.svg?type=large)](https://app.fossa.io/projects/git%2Bgithub.com%2Fyukimochi%2FCOM_Update_Manager?ref=badge_large)