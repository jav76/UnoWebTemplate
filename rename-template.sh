#!/usr/bin/env bash

# Exit immediately if any command fails
set -euo pipefail

if [ "$#" -ne 1 ]; then
    echo "Usage: $0 <NewProjectName>"
    echo "Example: $0 MyAwesomeDashboard"
    exit 1
fi

NEW_NAME=$1
OLD_NAME="UnoWebTemplate"

echo "🔄 Renaming template from '${OLD_NAME}' to '${NEW_NAME}'..."

# 1. Rename files and directories matching "UnoWebTemplate"
# We process directories from deepest to shallowest using depth order
find . -depth -name "*${OLD_NAME}*" | while read -r path; do
    dir=$(dirname "$path")
    base=$(basename "$path")
    new_base="${base//$OLD_NAME/$NEW_NAME}"
    mv "$path" "$dir/$new_base"
done

# 2. Replace occurrences of "UnoWebTemplate" inside text files
# Excludes .git folder and the rename script itself
find . -type f \
    -not -path '*/.git/*' \
    -not -path '*/bin/*' \
    -not -path '*/obj/*' \
    -not -name 'rename-template.sh' \
    -not -name 'tailwindcss' \
    -exec sed -i "s/$OLD_NAME/$NEW_NAME/g" {} +

echo "✅ Rename complete! Build project with: dotnet build"
