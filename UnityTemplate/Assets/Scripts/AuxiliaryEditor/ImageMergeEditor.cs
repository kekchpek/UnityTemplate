using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace kekchpek.Auxiliary.Editor
{
    public class ImageMergeEditor
    {
        [MenuItem("Assets/Merge Images Horizontally", false, 200)]
        private static void MergeImagesHorizontally()
        {
            // Get selected objects
            Object[] selectedObjects = Selection.objects;
            
            // Validate selection
            if (selectedObjects.Length != 2)
            {
                EditorUtility.DisplayDialog("Invalid Selection", "Please select exactly 2 images to merge.", "OK");
                return;
            }
            
            Texture2D[] textures = new Texture2D[2];
            string[] assetPaths = new string[2];
            
            // Validate that both selected objects are textures
            for (int i = 0; i < 2; i++)
            {
                string assetPath = AssetDatabase.GetAssetPath(selectedObjects[i]);
                assetPaths[i] = assetPath;
                
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                {
                    EditorUtility.DisplayDialog("Invalid Selection", $"Selected object '{selectedObjects[i].name}' is not an image.", "OK");
                    return;
                }
                
                // Temporarily make texture readable
                bool wasReadable = importer.isReadable;
                importer.isReadable = true;
                AssetDatabase.ImportAsset(assetPath);
                
                textures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                
                if (textures[i] == null)
                {
                    EditorUtility.DisplayDialog("Error", $"Failed to load texture: {selectedObjects[i].name}", "OK");
                    return;
                }
            }
            
            try
            {
                // Create merged texture
                // Right part = first image, Left part = second image
                Texture2D rightTexture = textures[0]; // First selected image goes to right
                Texture2D leftTexture = textures[1];  // Second selected image goes to left
                
                int mergedWidth = rightTexture.width + leftTexture.width;
                int mergedHeight = Mathf.Max(rightTexture.height, leftTexture.height);
                
                Texture2D mergedTexture = new Texture2D(mergedWidth, mergedHeight, TextureFormat.RGBA32, false);
                
                // Fill with transparent pixels
                Color[] emptyPixels = new Color[mergedWidth * mergedHeight];
                for (int i = 0; i < emptyPixels.Length; i++)
                {
                    emptyPixels[i] = Color.clear;
                }
                mergedTexture.SetPixels(emptyPixels);
                
                // Copy left texture to left side
                Color[] leftPixels = leftTexture.GetPixels();
                for (int y = 0; y < leftTexture.height; y++)
                {
                    for (int x = 0; x < leftTexture.width; x++)
                    {
                        int sourceIndex = y * leftTexture.width + x;
                        int targetX = x;
                        int targetY = y;
                        mergedTexture.SetPixel(targetX, targetY, leftPixels[sourceIndex]);
                    }
                }
                
                // Copy right texture to right side
                Color[] rightPixels = rightTexture.GetPixels();
                for (int y = 0; y < rightTexture.height; y++)
                {
                    for (int x = 0; x < rightTexture.width; x++)
                    {
                        int sourceIndex = y * rightTexture.width + x;
                        int targetX = leftTexture.width + x;
                        int targetY = y;
                        mergedTexture.SetPixel(targetX, targetY, rightPixels[sourceIndex]);
                    }
                }
                
                mergedTexture.Apply();
                
                // Save the merged texture
                byte[] pngData = mergedTexture.EncodeToPNG();
                
                // Generate a unique filename
                string baseName = $"{leftTexture.name}_{rightTexture.name}_merged";
                string directory = Path.GetDirectoryName(assetPaths[0]);
                string fileName = $"{baseName}.png";
                string fullPath = Path.Combine(directory, fileName);
                
                // Make sure the filename is unique
                int counter = 1;
                while (File.Exists(fullPath))
                {
                    fileName = $"{baseName}_{counter}.png";
                    fullPath = Path.Combine(directory, fileName);
                    counter++;
                }
                
                File.WriteAllBytes(fullPath, pngData);
                
                // Refresh the asset database
                AssetDatabase.Refresh();
                
                // Select the newly created merged image
                Object mergedAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(directory, fileName).Replace('\\', '/'));
                if (mergedAsset != null)
                {
                    Selection.activeObject = mergedAsset;
                    EditorGUIUtility.PingObject(mergedAsset);
                }
                
                Debug.Log($"Images merged successfully: {fileName}");
                
                // Clean up
                Object.DestroyImmediate(mergedTexture);
            }
            finally
            {
                // Restore original import settings
                for (int i = 0; i < 2; i++)
                {
                    TextureImporter importer = AssetImporter.GetAtPath(assetPaths[i]) as TextureImporter;
                    if (importer != null)
                    {
                        // Only restore if we changed it
                        if (!importer.isReadable)
                        {
                            importer.isReadable = false;
                            AssetDatabase.ImportAsset(assetPaths[i]);
                        }
                    }
                }
            }
        }

        [MenuItem("Assets/Merge Folders Horizontally", false, 201)]
        private static void MergeFoldersHorizontally()
        {
            // Get selected objects
            Object[] selectedObjects = Selection.objects;
            
            // Validate selection
            if (selectedObjects.Length != 2)
            {
                EditorUtility.DisplayDialog("Invalid Selection", "Please select exactly 2 folders to merge images from.", "OK");
                return;
            }

            // Validate that both selected objects are folders
            string[] folderPaths = new string[2];
            for (int i = 0; i < 2; i++)
            {
                string assetPath = AssetDatabase.GetAssetPath(selectedObjects[i]);
                if (!AssetDatabase.IsValidFolder(assetPath))
                {
                    EditorUtility.DisplayDialog("Invalid Selection", $"Selected object '{selectedObjects[i].name}' is not a folder.", "OK");
                    return;
                }
                folderPaths[i] = assetPath;
            }

            // Get all texture files from both folders
            string[] leftFolderFiles = Directory.GetFiles(folderPaths[0], "*.png", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .ToArray();
            string[] rightFolderFiles = Directory.GetFiles(folderPaths[1], "*.png", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .ToArray();

            // Find matching filenames
            var matchingFiles = leftFolderFiles.Intersect(rightFolderFiles).ToArray();

            if (matchingFiles.Length == 0)
            {
                EditorUtility.DisplayDialog("No Matches", "No matching image files found in the selected folders.", "OK");
                return;
            }

            int successCount = 0;
            int failCount = 0;

            string outputFolder = string.Concat(folderPaths[0], "_", folderPaths[1].Split('/').Last() + "_merged");

            foreach (string fileName in matchingFiles)
            {
                string leftPath = Path.Combine(folderPaths[0], fileName + ".png");
                string rightPath = Path.Combine(folderPaths[1], fileName + ".png");

                // Load textures
                Texture2D leftTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(leftPath);
                Texture2D rightTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(rightPath);

                if (leftTexture == null || rightTexture == null)
                {
                    failCount++;
                    continue;
                }

                try
                {
                    // Make textures readable
                    TextureImporter leftImporter = AssetImporter.GetAtPath(leftPath) as TextureImporter;
                    TextureImporter rightImporter = AssetImporter.GetAtPath(rightPath) as TextureImporter;

                    bool leftWasReadable = leftImporter.isReadable;
                    bool rightWasReadable = rightImporter.isReadable;

                    leftImporter.isReadable = true;
                    rightImporter.isReadable = true;
                    AssetDatabase.ImportAsset(leftPath);
                    AssetDatabase.ImportAsset(rightPath);

                    // Create merged texture
                    int mergedWidth = leftTexture.width + rightTexture.width;
                    int mergedHeight = Mathf.Max(leftTexture.height, rightTexture.height);

                    Texture2D mergedTexture = new Texture2D(mergedWidth, mergedHeight, TextureFormat.RGBA32, false);

                    // Fill with transparent pixels
                    Color[] emptyPixels = new Color[mergedWidth * mergedHeight];
                    for (int i = 0; i < emptyPixels.Length; i++)
                    {
                        emptyPixels[i] = Color.clear;
                    }
                    mergedTexture.SetPixels(emptyPixels);

                    // Copy left texture
                    Color[] leftPixels = leftTexture.GetPixels();
                    for (int y = 0; y < leftTexture.height; y++)
                    {
                        for (int x = 0; x < leftTexture.width; x++)
                        {
                            int sourceIndex = y * leftTexture.width + x;
                            mergedTexture.SetPixel(x, y, leftPixels[sourceIndex]);
                        }
                    }

                    // Copy right texture
                    Color[] rightPixels = rightTexture.GetPixels();
                    for (int y = 0; y < rightTexture.height; y++)
                    {
                        for (int x = 0; x < rightTexture.width; x++)
                        {
                            int sourceIndex = y * rightTexture.width + x;
                            mergedTexture.SetPixel(leftTexture.width + x, y, rightPixels[sourceIndex]);
                        }
                    }

                    mergedTexture.Apply();

                    // Save the merged texture
                    byte[] pngData = mergedTexture.EncodeToPNG();
                    string outputPath = Path.Combine(outputFolder, $"{fileName}.png");
                    if (!Directory.Exists(outputFolder))
                    {
                        Directory.CreateDirectory(outputFolder);
                    }
                    File.WriteAllBytes(outputPath, pngData);

                    // Clean up
                    Object.DestroyImmediate(mergedTexture);

                    // Restore original import settings
                    leftImporter.isReadable = leftWasReadable;
                    rightImporter.isReadable = rightWasReadable;
                    AssetDatabase.ImportAsset(leftPath);
                    AssetDatabase.ImportAsset(rightPath);

                    successCount++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to merge {fileName}: {e.Message}");
                    failCount++;
                }
            }

            // Refresh the asset database
            AssetDatabase.Refresh();

            // Show results
            EditorUtility.DisplayDialog("Merge Complete", 
                $"Successfully merged {successCount} image pairs.\nFailed to merge {failCount} image pairs.", 
                "OK");
        }
    } 
}