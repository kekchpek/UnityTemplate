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

echo "Replacing '$OLD_NAME' with '$NEW_NAME'..."

# Rename files and directories
find . -depth -name "*$OLD_NAME*" | while read -r FILE; do
    NEW_FILE=$(echo "$FILE" | sed "s/$OLD_NAME/$NEW_NAME/g")
    mv "$FILE" "$NEW_FILE"
    echo "Renamed: $FILE -> $NEW_FILE"
done

# Replace contents in all files
grep -rl "$OLD_NAME" . | while read -r FILE; do
    sed -i "s/$OLD_NAME/$NEW_NAME/g" "$FILE"
    echo "Updated contents in: $FILE"
done

echo "All replacements completed successfully."
