#!/bin/bash

# 環境変数設定の検証スクリプト
# このスクリプトを実行して、環境変数が正しく読み込まれるか確認してください

echo "=========================================="
echo "環境変数設定の検証"
echo "=========================================="
echo ""

# Step 1: Check if .env exists
echo "【ステップ1】.env ファイルの確認"
if [ -f .env ]; then
    echo "✓ .env ファイルが存在します"
    echo ""
    echo ".env ファイルの内容（コメント行を除く）:"
    echo "---"
    cat .env | grep -v "^#" | grep -v "^$"
    echo "---"
else
    echo "✗ .env ファイルが見つかりません"
    echo ""
    echo "以下のコマンドで作成してください:"
    echo "  cp .env.example .env"
    exit 1
fi

echo ""
echo "【ステップ2】Docker Composeの設定確認"
echo "Docker Composeが認識している環境変数:"
echo "---"
docker compose config 2>&1 | grep -A 5 "environment:"
echo "---"

echo ""
echo "【ステップ3】Dockerイメージの再ビルド"
echo "古いイメージをクリーンアップして再ビルドします..."
docker compose down 2>/dev/null
docker rmi rooster.appmain 2>/dev/null || true
echo ""

echo "【ステップ4】アプリケーションの実行"
echo "docker compose up --build を実行します..."
echo "---"
docker compose up --build 2>&1 | tee /tmp/docker-verify-output.log
echo "---"

echo ""
echo "【ステップ5】結果の確認"
echo "アプリケーション出力:"
echo "---"
grep -A 10 "Discord Bot" /tmp/docker-verify-output.log
echo "---"

echo ""
echo "=========================================="
echo "検証完了"
echo "=========================================="
echo ""
echo "期待される出力:"
echo "  APP_ENVIRONMENT: Development"
echo "  DISCORD_CHANNEL_ID: your_discord_channel_id_here"
echo "  DISCORD_BOT_TOKEN: your***"
echo ""
echo "もし環境変数が (not set) と表示される場合は、"
echo "このスクリプトの出力をGitHubのコメントに貼り付けてください。"

# Cleanup
docker compose down 2>/dev/null
