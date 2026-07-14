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

# Check if there are any changes in the wiki submodule
if [ -n "$(git status --porcelain)" ]; then
    git add .
    git commit -m "Auto-sync documentation updates from main repository"
    git push origin HEAD
    echo "✅ Wiki repository updated and pushed."
else
    echo "ℹ️ No changes detected in the wiki."
fi

cd ..

echo "📌 Checking if parent repository needs to track the new submodule commit..."
# Check if the submodule pointer itself has changed in the main repository
if [ -n "$(git status --porcelain "$WIKI_DIR")" ]; then
    git add "$WIKI_DIR"
    git commit -m "chore: update wiki submodule pointer"
    git push origin HEAD
    echo "✅ Main repository submodule pointer updated and pushed."
else
    echo "ℹ️ Submodule pointer is already up to date in the main repository."
fi
