#!/usr/bin/env bash

# Exit immediately if any command fails
set -euo pipefail

WIKI_DIR="wiki"
DOCS_DIR="docs"

echo "🔄 Initializing/updating wiki submodule..."
git submodule update --init --recursive

echo "🧹 Syncing docs to wiki folder..."
# Copy all markdown files from docs folder directly into the flat wiki folder
cp -v "$DOCS_DIR"/*.md "$WIKI_DIR"/

echo "💾 Committing and pushing changes to GitHub Wiki..."
cd "$WIKI_DIR"

# Check if there are any changes or unpushed commits in the wiki submodule
if [ -n "$(git status --porcelain)" ] || git status -sb | grep -q "ahead"; then
    if [ -n "$(git status --porcelain)" ]; then
        git add .
        git commit -m "Auto-sync documentation updates from main repository"
    fi
    git push origin HEAD
    echo "✅ Wiki repository updated and pushed."
else
    echo "ℹ️ Wiki repository is already up to date."
fi

cd ..

echo "📌 Checking if parent repository needs to track the new submodule commit..."
# Check if the submodule pointer itself has changed in the main repository
if [ -n "$(git status --porcelain "$WIKI_DIR")" ]; then
    git add "$WIKI_DIR"
    git commit -m "chore: update wiki submodule pointer"
fi

if git status -sb | grep -q "ahead"; then
    git push origin HEAD
    echo "✅ Main repository updated and pushed."
else
    echo "ℹ️ Main repository is already up to date."
fi
