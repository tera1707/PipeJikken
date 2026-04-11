# PipeJikken

Named Pipe（名前付きパイプ）を使用したプロセス間通信の実験プロジェクト

## プロジェクト構成

### PipeJikken

Named Pipeのサーバー/クライアント実装を含むクラスライブラリ。

- `PipeServer`: Named Pipeサーバーの実装。クライアントからの接続を待ち、メッセージの送受信を行う
- `PipeClient`: Named Pipeクライアントの実装。サーバーに接続してメッセージの送受信を行う
- `IPipeServer` / `IPipeClient`: インターフェース定義

### PipeJikkenWPF

Named Pipeの動作を確認するためのWPFアプリケーション。  
PipeServerまたはPipeClientとして動作し、プロセス間通信のテストが可能。

### TestProject

PipeJikkenライブラリのユニットテストプロジェクト。

### ServiceJikken

Windowsサービスとして動作するC++プロジェクト。  
Named Pipeを使用したサービス間通信の実験用。

権限の高いWindowsサービスが開いたパイプサーバーに対して、ユーザー権限のクライアントから接続して通信するときの実験のためのプロジェクト。  
CreateNamedPipe関数でセキュリティ属性を設定し、ユーザー権限のクライアントが接続できるようにするあたりがポイント。

## 使用技術

- .NET 6
- C# 10.0
- WPF
- Named Pipe (System.IO.Pipes)

## 主な機能

- 非同期でのパイプ接続・切断処理
- クライアント切断後の自動再接続待ち
- タイムアウト付きメッセージ送信
- キャンセルトークンによる安全な終了処理
