# Docker Environment Variables Setup

このドキュメントでは、Dockerを使用してアプリケーションに環境変数を渡す方法について説明します。

## セットアップ手順

### 1. 環境変数ファイルの作成

`.env.example` ファイルを `.env` にコピーして、実際の値を設定します：

```bash
cd src/Rooster.AppMain
cp .env.example .env
```

### 2. .env ファイルの編集

`.env` ファイルを開いて、以下の値を設定します：

```env
# Discord bot token from Discord Developer Portal
DISCORD_BOT_TOKEN=your_actual_bot_token_here

# Discord channel ID where the bot will operate
DISCORD_CHANNEL_ID=your_channel_id_here

# Application environment (Development, Staging, Production)
APP_ENVIRONMENT=Development
```

### 3. Docker Composeでの実行

環境変数を使用してアプリケーションを起動：

```bash
cd src/Rooster.AppMain
docker compose up --build
```

## 環境変数の設定方法

### 方法1: .envファイルを使用（推奨）

`.env` ファイルに環境変数を設定します。このファイルは `.gitignore` に含まれているため、機密情報がコミットされることはありません。

```env
DISCORD_BOT_TOKEN=your_token
DISCORD_CHANNEL_ID=123456789
APP_ENVIRONMENT=Development
```

### 方法2: compose.yamlで直接指定

`compose.yaml` の `environment` セクションで直接値を指定することもできます：

```yaml
services:
  rooster.appmain:
    environment:
      - DISCORD_BOT_TOKEN=your_token
      - DISCORD_CHANNEL_ID=123456789
      - APP_ENVIRONMENT=Development
```

**注意**: この方法では機密情報がファイルに直接記述されるため、`.env` ファイルの使用を推奨します。

### 方法3: docker composeコマンドで指定

環境変数をコマンドラインで指定することもできます：

```bash
DISCORD_BOT_TOKEN=your_token DISCORD_CHANNEL_ID=123456789 docker compose up
```

## 利用可能な環境変数

| 変数名 | 説明 | デフォルト値 | 必須 |
|--------|------|--------------|------|
| `DISCORD_BOT_TOKEN` | Discord Developer Portalから取得したボットトークン | - | はい |
| `DISCORD_CHANNEL_ID` | ボットが動作するDiscordチャンネルのID | - | はい |
| `APP_ENVIRONMENT` | アプリケーションの実行環境 (Development/Staging/Production) | Development | いいえ |

## セキュリティに関する注意事項

- `.env` ファイルには機密情報が含まれるため、絶対にGitにコミットしないでください
- `.env` ファイルは `.gitignore` に追加されています
- 代わりに `.env.example` ファイルを参照用としてコミットしています
- 本番環境では、環境変数を安全に管理するためのシークレット管理サービスの使用を検討してください

## トラブルシューティング

### 環境変数が読み込まれない場合

1. `.env` ファイルが `compose.yaml` と同じディレクトリにあることを確認
2. `.env` ファイルの形式が正しいことを確認（`KEY=value` の形式）
3. Docker Composeを再起動してみる：
   ```bash
   docker compose down
   docker compose up --build
   ```

### アプリケーションのログを確認

環境変数が正しく読み込まれているかログで確認できます：

```bash
docker compose logs
```

アプリケーション起動時に設定された環境変数が表示されます（機密情報はマスクされます）。
