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

以下の出力が表示される場合:
```
DISCORD_CHANNEL_ID: (not set)
DISCORD_BOT_TOKEN: (not set)
```

考えられる原因と対処法：

#### 1. `.env` ファイルが存在しない、または名前が間違っている

**確認方法:**
```bash
cd src/Rooster.AppMain
ls -la .env
```

**対処法:**
```bash
# .env.example から .env を作成
cp .env.example .env
# エディタで .env を開いて、実際の値を設定
```

**注意:** ファイル名は `.env` です。`.env.sample` や `.env.example` ではありません。

#### 2. `.env` ファイルの形式が間違っている

**正しい形式:**
```env
DISCORD_BOT_TOKEN=ABC123XYZ.Example.YourActualTokenHere
DISCORD_CHANNEL_ID=1234567890123456789
APP_ENVIRONMENT=Production
```

**間違った形式:**
```env
# スペースを入れない
DISCORD_BOT_TOKEN = your_token  ❌

# 引用符で囲まない
DISCORD_BOT_TOKEN="your_token"  ❌
DISCORD_BOT_TOKEN='your_token'  ❌

# プレースホルダーのまま
DISCORD_BOT_TOKEN=your_discord_bot_token_here  ❌
```

#### 3. `.env` ファイルの内容を確認

```bash
cd src/Rooster.AppMain
cat .env
```

プレースホルダー(`your_discord_bot_token_here` など)が実際の値に置き換えられていることを確認してください。

#### 4. Docker Composeの設定を確認

以下のコマンドで、Docker Composeが環境変数を正しく読み込んでいるか確認できます:

```bash
cd src/Rooster.AppMain
docker compose config
```

`environment` セクションに正しい値が表示されるはずです。

#### 5. Docker Composeを再起動

変更が反映されていない場合は、コンテナを再起動してください:

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

**正常な出力例:**
```
APP_ENVIRONMENT: Production
DISCORD_CHANNEL_ID: 1234567890123456789
DISCORD_BOT_TOKEN: MTI3***
```

**異常な出力例（環境変数が読み込まれていない）:**
```
APP_ENVIRONMENT: Development
DISCORD_CHANNEL_ID: (not set)
DISCORD_BOT_TOKEN: (not set)
```

