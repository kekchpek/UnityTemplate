#!/bin/bash

# Check if the ProjectName file exists
if [ ! -f "ProjectName" ]; then
    echo "Error: 'ProjectName' file not found in the current directory."
    exit 1
fi

# Check if the new name is provided as an argument
if [ -z "$1" ]; then
    echo "Usage: $0 <new_project_name>"
    exit 1
fi

# Read the old project name from the ProjectName file
OLD_NAME=$(sed -n '1p' ProjectName | tr -d '\n')
NEW_NAME="$1"

# Check if the old name is empty
if [ -z "$OLD_NAME" ]; then
    echo "Error: ProjectName file is empty."
    exit 1
fi

rm -rfd "$OLD_NAME/Library"

# Hardcoded excluded directories
EXCLUDED_DIRS=("$NEW_NAME/Assets/DoTween" ".git")

echo "Replacing '$OLD_NAME' with '$NEW_NAME'..."

# Build the exclude path arguments for find
EXCLUDE_ARGS=()
for DIR in "${EXCLUDED_DIRS[@]}"; do
    EXCLUDE_ARGS+=(-path "./$DIR" -prune -o)
done

# Rename files and directories (only rename the basename, not full path)
find . -depth -name "*$OLD_NAME*" | while read -r FILE; do
    DIR=$(dirname "$FILE")
    BASENAME=$(basename "$FILE")
    NEW_BASENAME=$(echo "$BASENAME" | sed "s/$OLD_NAME/$NEW_NAME/g")
    mv "$FILE" "$DIR/$NEW_BASENAME"
    echo "Renamed: $FILE -> $DIR/$NEW_BASENAME"
done

# Replace contents in all non-excluded files
# Note: sed -i '' is required for macOS (empty string for no backup)
find . "${EXCLUDE_ARGS[@]}" -type f -print | while read -r FILE; do
    if grep -q "$OLD_NAME" "$FILE"; then
        sed -i '' "s/$OLD_NAME/$NEW_NAME/g" "$FILE"
        echo "Updated contents in: $FILE"
    fi
done


echo "All replacements completed successfully."