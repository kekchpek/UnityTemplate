using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameMasterConsole.Editor
{
    public static class GameMasterClientLauncher
    {
        [MenuItem("Window/Game Master/Launch Console Client")]
        public static void LaunchClient()
        {
            string clientPath = GetClientDirectoryPath();
            
            if (!Directory.Exists(clientPath))
            {
                UnityEngine.Debug.LogError($"GameMaster client directory not found at: {clientPath}");
                return;
            }
            
            string scriptPath = GetPlatformSpecificScript(clientPath);
            
            if (!File.Exists(scriptPath))
            {
                UnityEngine.Debug.LogError($"GameMaster client script not found at: {scriptPath}");
                return;
            }
            
            LaunchScript(scriptPath, clientPath);
        }
        
        private static string GetClientDirectoryPath()
        {
            string scriptPath = Path.GetFullPath(
                Path.Combine(Application.dataPath, "Scripts/GameMasterConsole/Client"));
            return scriptPath;
        }
        
        private static string GetPlatformSpecificScript(string clientPath)
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    return Path.Combine(clientPath, "run_client.bat");
                
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.LinuxEditor:
                    return Path.Combine(clientPath, "run_client.sh");
                
                default:
                    UnityEngine.Debug.LogError($"Unsupported platform: {Application.platform}");
                    return string.Empty;
            }
        }
        
        private static void LaunchScript(string scriptPath, string workingDirectory)
        {
            try
            {
                UnityEngine.Debug.Log($"Attempting to launch script: {scriptPath}");
                UnityEngine.Debug.Log($"Working directory: {workingDirectory}");
                
                ProcessStartInfo startInfo = new ProcessStartInfo();
                
                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    // On Windows, launch the .bat file directly
                    startInfo.FileName = scriptPath;
                    startInfo.UseShellExecute = true;
                    startInfo.WorkingDirectory = workingDirectory;
                }
                else
                {
                    // On macOS/Linux, we need to make the script executable and launch it via terminal
                    // First, make sure the script is executable
                    MakeScriptExecutable(scriptPath);
                    
                    if (Application.platform == RuntimePlatform.OSXEditor)
                    {
                        // Launch Terminal.app with the script on macOS using osascript
                        string command = $"cd '{workingDirectory}' && ./run_client.sh";
                        startInfo.FileName = "osascript";
                        startInfo.Arguments = $"-e 'tell application \"Terminal\" to do script \"{command}\"'";
                        startInfo.UseShellExecute = false;
                        startInfo.RedirectStandardError = true;
                        startInfo.RedirectStandardOutput = true;
                        
                        UnityEngine.Debug.Log($"Executing osascript command: {startInfo.FileName} {startInfo.Arguments}");
                    }
                    else
                    {
                        // Launch with xterm or gnome-terminal on Linux
                        startInfo.FileName = "x-terminal-emulator";
                        startInfo.Arguments = $"-e 'cd \"{workingDirectory}\" && \"{scriptPath}\"'";
                        startInfo.UseShellExecute = false;
                    }
                }
                
                Process process = Process.Start(startInfo);
                
                if (process != null && Application.platform == RuntimePlatform.OSXEditor)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    
                    if (!string.IsNullOrEmpty(output))
                    {
                        UnityEngine.Debug.Log($"Process output: {output}");
                    }
                    
                    if (!string.IsNullOrEmpty(error))
                    {
                        UnityEngine.Debug.LogWarning($"Process error: {error}");
                    }
                }
                
                UnityEngine.Debug.Log($"GameMaster Console Client launched successfully");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to launch GameMaster Console Client: {e.Message}\nStack trace: {e.StackTrace}");
            }
        }
        
        private static void MakeScriptExecutable(string scriptPath)
        {
            try
            {
                ProcessStartInfo chmodInfo = new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+x \"{scriptPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                Process chmodProcess = Process.Start(chmodInfo);
                chmodProcess?.WaitForExit();
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning($"Failed to make script executable: {e.Message}");
            }
        }
    }
}

