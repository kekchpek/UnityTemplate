using System;

namespace GMConsole
{
    /// <summary>
    /// Interface for registering debug commands in the GameMaster system
    /// </summary>
    public interface IGameMasterCommandRegistry
    {
        /// <summary>
        /// Registers a debug command with the GameMaster system
        /// Commands can optionally set result messages using GMArgs.SetResult()
        /// </summary>
        /// <param name="command">Command name/identifier</param>
        /// <param name="handler">Action to execute when command is called</param>
        void RegisterCommand(string command, string description, Action<GMArgs> handler);
        
        /// <summary>
        /// Unregisters a debug command from the GameMaster system
        /// </summary>
        /// <param name="command">Command name/identifier to remove</param>
        /// <returns>True if command was found and removed, false otherwise</returns>
        bool UnregisterCommand(string command);
        
        /// <summary>
        /// Checks if a command is registered
        /// </summary>
        /// <param name="command">Command name to check</param>
        /// <returns>True if command is registered, false otherwise</returns>
        bool IsCommandRegistered(string command);
        
        /// <summary>
        /// Gets all registered command names
        /// </summary>
        /// <returns>Array of registered command names</returns>
        string[] GetRegisteredCommands();
    }
}
