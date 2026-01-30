using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace GMConsole
{
    /// <summary>
    /// HTTP server that accepts game master commands and executes them
    /// </summary>
    public class GameMasterServer : IDisposable, IGameMasterCommandRegistry, IGameMasterServer
    {
        private const string SetGameSpeedCommand = "SetGameSpeed";
        private const string HelpCommand = "help";

        private int _port;
        private bool _logRequests;
        
        private HttpListener httpListener;
        private CancellationTokenSource cancellationTokenSource;
        private bool isRunning = false;
        private GameMasterSsdpServer _ssdpServer;
        private readonly Dictionary<string, (string desc, Action<GMArgs> handler)> _registeredCommands;
        private SynchronizationContext _mainThreadSynchronizationContext;
        
        public event Action<string, Dictionary<string, string>> OnCommandReceived;
        
        /// <summary>
        /// Initializes a new instance of the GameMasterServer
        /// </summary>
        public GameMasterServer()
        {
            _registeredCommands = new Dictionary<string, (string desc, Action<GMArgs> handler)>(StringComparer.OrdinalIgnoreCase);
            RegisterCommand(HelpCommand, "Shows all comands", HandleHelpCommand);

            RegisterCommand(
                SetGameSpeedCommand, "Устанавливает множитель скорости игры. Пример: \"SetGameSpeed 2\" для двойной скорости, \"SetGameSpeed 0.5\" для половинной скорости",
                HandleSetGameSpeedCommand);
        }
        
        /// <summary>
        /// Starts the HTTP server
        /// </summary>
        /// <param name="port">Port to listen on</param>
        /// <param name="allowNetworkAccess">Whether to allow connections from other computers on the network</param>
        /// <param name="logRequests">Whether to log incoming requests</param>
        /// <param name="enableSsdp">Whether to enable SSDP discovery service</param>
        public void StartServer(int port = 54345, bool allowNetworkAccess = true, bool logRequests = true, bool enableSsdp = true)
        {
            if (isRunning)
            {
                Debug.LogWarning("GameMasterServer is already running");
                return;
            }
            
            this._port = port;
            this._logRequests = logRequests;
            
            // Capture the current synchronization context (Unity main thread)
            _mainThreadSynchronizationContext = SynchronizationContext.Current;
            
            try
            {
                httpListener = new HttpListener();
                
                // Choose prefix based on network access setting
                string prefix = allowNetworkAccess ? $"http://+:{port}/" : $"http://localhost:{port}/";
                
                Debug.Log($"Attempting to start HTTP listener with prefix: {prefix}");
                
                try
                {
                    httpListener.Prefixes.Add(prefix);
                    httpListener.Start();
                    Debug.Log($"HTTP listener started successfully with prefix: {prefix}");
                }
                catch (HttpListenerException httpEx)
                {
                    Debug.LogWarning($"Failed to start with '{prefix}': {httpEx.Message}. Falling back to localhost only.");
                    
                    // Fallback to localhost if network access fails
                    httpListener.Prefixes.Clear();
                    httpListener.Prefixes.Add($"http://localhost:{port}/");
                    httpListener.Start();
                    
                    Debug.LogWarning($"GameMasterServer started on localhost only due to permission issues. " +
                                   $"To enable network access, you may need to:\n" +
                                   $"- On Windows: Run Unity as Administrator\n" +
                                   $"- On macOS: Allow Unity in System Preferences → Security & Privacy → Firewall\n" +
                                   $"- Or use 'netsh http add urlacl' (Windows) / 'sudo' (macOS/Linux)");
                    
                    allowNetworkAccess = false; // Update flag to reflect actual state
                }
                
                cancellationTokenSource = new CancellationTokenSource();
                isRunning = true;
                
                string accessInfo = allowNetworkAccess ? "all network interfaces" : "localhost only";
                Debug.Log($"GameMasterServer started on port {port} ({accessInfo})");
                
                if (allowNetworkAccess)
                {
                    var localIPs = GetLocalIPAddresses();
                    if (localIPs.Any())
                    {
                        Debug.Log($"Server accessible from network at: {string.Join(", ", localIPs.Select(ip => $"http://{ip}:{port}"))}");
                    }
                    else
                    {
                        Debug.LogWarning("No local IP addresses found for network access");
                    }
                }
                
                // Start listening for requests in background
                Task.Run(() => ListenForRequests(cancellationTokenSource.Token));
                
                // Start SSDP discovery service if enabled
                if (enableSsdp && allowNetworkAccess)
                {
                    Debug.Log("Starting SSDP discovery service...");
                    _ssdpServer = new GameMasterSsdpServer(_port, _logRequests);
                    _ssdpServer.StartServer();
                }
                else if (enableSsdp && !allowNetworkAccess)
                {
                    Debug.LogWarning("SSDP discovery disabled because server is running on localhost only");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start GameMasterServer: {ex.Message}\nStack trace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Stops the HTTP server
        /// </summary>
        public void StopServer()
        {
            if (!isRunning)
                return;
                
            try
            {
                // Stop SSDP server first
                _ssdpServer?.StopServer();
                _ssdpServer?.Dispose();
                _ssdpServer = null;
                
                cancellationTokenSource?.Cancel();
                httpListener?.Stop();
                httpListener?.Close();
                isRunning = false;
                
                Debug.Log("GameMasterServer stopped");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error stopping GameMasterServer: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Disposes the server resources
        /// </summary>
        public void Dispose()
        {
            StopServer();
        }
        
        /// <summary>
        /// Listens for incoming HTTP requests
        /// </summary>
        private async Task ListenForRequests(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && httpListener.IsListening)
            {
                try
                {
                    var context = await httpListener.GetContextAsync();
                    
                    // Handle request on Unity main thread using captured synchronization context
                    if (_mainThreadSynchronizationContext != null)
                    {
                        _mainThreadSynchronizationContext.Post(_ => HandleRequest(context), null);
                    }
                    else
                    {
                        // Fallback to current thread if no synchronization context is available
                        Debug.LogWarning("No synchronization context available, handling request on background thread");
                        HandleRequest(context);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Expected when stopping the server
                    break;
                }
                catch (HttpListenerException ex)
                {
                    if (ex.ErrorCode != 995) // ERROR_OPERATION_ABORTED
                    {
                        Debug.LogError($"HTTP Listener error: {ex.Message}");
                        Debug.LogException(ex);
                    }
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Unexpected error in GameMasterServer: {ex.Message}");
                    Debug.LogException(ex);
                }
            }
        }
        
        /// <summary>
        /// Handles incoming HTTP requests
        /// </summary>
        private void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            
            try
            {
                if (_logRequests)
                {
                    Debug.Log($"Received {request.HttpMethod} request to {request.Url}");
                }
                
                // Set CORS headers to allow requests from any origin
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
                
                // Handle OPTIONS request for CORS preflight
                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 200;
                    response.Close();
                    return;
                }
                
                if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/command")
                {
                    HandleCommandRequest(request, response);
                }
                else if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/")
                {
                    HandleStatusRequest(response);
                }
                else if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/description.xml")
                {
                    HandleServiceDescriptionRequest(response);
                }
                else
                {
                    // Not found
                    response.StatusCode = 404;
                    WriteResponse(response, "Endpoint not found");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error handling request: {ex.Message}");
                
                try
                {
                    response.StatusCode = 500;
                    WriteResponse(response, $"Internal server error: {ex.Message}");
                }
                catch
                {
                    // Ignore errors when writing error response
                }
            }
        }
        
        /// <summary>
        /// Handles command execution requests
        /// </summary>
        private void HandleCommandRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                // Read request body
                string requestBody;
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    requestBody = reader.ReadToEnd();
                }
                
                if (string.IsNullOrEmpty(requestBody))
                {
                    response.StatusCode = 400;
                    WriteResponse(response, "Request body is required");
                    return;
                }
                
                // Parse command data
                var commandData = JsonConvert.DeserializeObject<CommandRequest>(requestBody);
                
                if (string.IsNullOrEmpty(commandData?.Command))
                {
                    response.StatusCode = 400;
                    WriteResponse(response, "Command is required");
                    return;
                }
                
                if (_logRequests)
                {
                    Debug.Log($"Executing command: {commandData.Command} with {commandData.Arguments?.Count ?? 0} arguments");
                }
                
                // Execute command (placeholder for now)
                var result = ExecuteCommand(commandData.Command, commandData.Arguments ?? new Dictionary<string, string>());
                
                // Return response
                response.StatusCode = 200;
                response.ContentType = "application/json";
                WriteResponse(response, JsonConvert.SerializeObject(result));
            }
            catch (JsonException ex)
            {
                response.StatusCode = 400;
                WriteResponse(response, $"Invalid JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                WriteResponse(response, $"Command execution failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles status requests
        /// </summary>
        private void HandleStatusRequest(HttpListenerResponse response)
        {
            var status = new
            {
                Status = "Running",
                Port = _port,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                RegisteredCommands = GetRegisteredCommands(),
                Endpoints = new[]
                {
                    "GET / - Server status",
                    "POST /command - Execute command",
                    "GET /description.xml - Service description (UPnP)"
                }
            };
            
            response.StatusCode = 200;
            response.ContentType = "application/json";
            WriteResponse(response, JsonConvert.SerializeObject(status, Formatting.Indented));
        }
        
        /// <summary>
        /// Handles service description requests (UPnP device description)
        /// </summary>
        private void HandleServiceDescriptionRequest(HttpListenerResponse response)
        {
            var localIPs = GetLocalIPAddresses();
            var serverIP = localIPs.FirstOrDefault() ?? "127.0.0.1";
            
            var description = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<root xmlns=""urn:schemas-upnp-org:device-1-0"">
    <specVersion>
        <major>1</major>
        <minor>0</minor>
    </specVersion>
    <device>
        <deviceType>urn:schemas-armor-guild:device:GameMasterConsole:1</deviceType>
        <friendlyName>Armor Guild Game Master Console</friendlyName>
        <modelDescription>Strategic Crafting Game Master Console for remote administration</modelDescription>
        <serialNumber>GM-{DateTime.UtcNow:yyyyMMdd}</serialNumber>
        <UDN>uuid:{Guid.NewGuid()}</UDN>
        <presentationURL>http://{serverIP}:{_port}/</presentationURL>
        <serviceList>
            <service>
                <serviceType>urn:schemas-armor-guild:service:GameMaster:1</serviceType>
                <serviceId>urn:armor-guild:serviceId:GameMaster</serviceId>
                <controlURL>/command</controlURL>
                <eventSubURL>/events</eventSubURL>
                <SCPDURL>/service.xml</SCPDURL>
            </service>
        </serviceList>
    </device>
</root>";
            
            response.StatusCode = 200;
            response.ContentType = "text/xml";
            WriteResponse(response, description);
        }
        
        /// <summary>
        /// Executes a game master command
        /// </summary>
        private CommandResult ExecuteCommand(string command, Dictionary<string, string> arguments)
        {
            try
            {
                // Fire event for other systems to handle (backward compatibility)
                OnCommandReceived?.Invoke(command, arguments);
                
                // Check if command is registered
                if (_registeredCommands.TryGetValue(command, out var commandData))
                {
                    var gmArgs = new GMArgs(arguments);
                    try {
                        commandData.handler.Invoke(gmArgs);
                        
                        // Use the result message from GMArgs if provided, otherwise use default message
                        var resultMessage = !string.IsNullOrEmpty(gmArgs.ResultMessage) 
                            ? gmArgs.ResultMessage 
                            : $"Command '{command}' executed successfully";
                        
                        // Create result data combining default data with any result data from the command
                        var resultData = new Dictionary<string, object>
                        {
                            ["command"] = command,
                            ["arguments"] = arguments,
                            ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            ["status"] = "completed"
                        };
                        
                        // Add any custom result data from the command
                        foreach (var kvp in gmArgs.ResultData)
                        {
                            resultData[kvp.Key] = kvp.Value;
                        }
                        
                        return new CommandResult
                        {
                            Success = true,
                            Message = resultMessage,
                            Data = resultData
                        };
                    }
                    catch (Exception ex)
                    {
                        return new CommandResult
                        {
                            Success = false,
                            Message = $"Error executing command '{command}': {ex.Message}",
                            Data = new Dictionary<string, object>
                            {
                                ["command"] = command,
                                ["arguments"] = arguments,
                                ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                ["status"] = "error",
                                ["error"] = ex.ToString()
                            }
                        };
                    }
                }
                else
                {
                    return new CommandResult
                    {
                        Success = false,
                        Message = $"Command '{command}' is not registered",
                        Data = new Dictionary<string, object>
                        {
                            ["command"] = command,
                            ["arguments"] = arguments,
                            ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            ["status"] = "failed",
                            ["error"] = "Command not found"
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing command '{command}': {ex.Message}");
                
                return new CommandResult
                {
                    Success = false,
                    Message = $"Error executing command '{command}': {ex.Message}",
                    Data = new Dictionary<string, object>
                    {
                        ["command"] = command,
                        ["arguments"] = arguments,
                        ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        ["status"] = "error",
                        ["error"] = ex.Message
                    }
                };
            }
        }
        
        /// <summary>
        /// Gets local IP addresses for network access information
        /// </summary>
        private List<string> GetLocalIPAddresses()
        {
            var addresses = new List<string>();
            
            try
            {
                foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (netInterface.OperationalStatus == OperationalStatus.Up &&
                        netInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        foreach (var addrInfo in netInterface.GetIPProperties().UnicastAddresses)
                        {
                            if (addrInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                addresses.Add(addrInfo.Address.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to get local IP addresses: {ex.Message}");
            }
            
            return addresses;
        }
        
        /// <summary>
        /// Writes response data to the HTTP response
        /// </summary>
        private void WriteResponse(HttpListenerResponse response, string content)
        {
            var buffer = Encoding.UTF8.GetBytes(content);
            response.ContentLength64 = buffer.Length;
            
            using (var output = response.OutputStream)
            {
                output.Write(buffer, 0, buffer.Length);
            }
            
            response.Close();
        }
        
        #region IGameMasterCommandRegistry Implementation
        
        /// <summary>
        /// Registers a debug command with the GameMaster system
        /// Commands can optionally set result messages using GMArgs.SetResult()
        /// </summary>
        /// <param name="command">Command name/identifier</param>
        /// <param name="handler">Action to execute when command is called</param>
        public void RegisterCommand(string command, string description, Action<GMArgs> handler)
        {
            if (string.IsNullOrEmpty(command))
            {
                throw new ArgumentException("Command name cannot be null or empty", nameof(command));
            }
            
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }
            
            // Store command with original casing, dictionary will handle case-insensitive lookup
            _registeredCommands[command] = (description, handler);
            
            if (_logRequests)
            {
                Debug.Log($"Registered command: {command}");
            }
        }
        
        /// <summary>
        /// Unregisters a debug command from the GameMaster system
        /// </summary>
        /// <param name="command">Command name/identifier to remove</param>
        /// <returns>True if command was found and removed, false otherwise</returns>
        public bool UnregisterCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                return false;
            }
            
            // Find the actual key in the dictionary (case-insensitive lookup)
            var actualKey = _registeredCommands.Keys.FirstOrDefault(k => 
                string.Equals(k, command, StringComparison.OrdinalIgnoreCase));
                
            bool removed = actualKey != null && _registeredCommands.Remove(actualKey);
            
            if (removed && _logRequests)
            {
                Debug.Log($"Unregistered command: {command}");
            }
            
            return removed;
        }
        
        /// <summary>
        /// Checks if a command is registered
        /// </summary>
        /// <param name="command">Command name to check</param>
        /// <returns>True if command is registered, false otherwise</returns>
        public bool IsCommandRegistered(string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                return false;
            }
            
            // ContainsKey will use the case-insensitive comparer we set up
            return _registeredCommands.ContainsKey(command);
        }
        
        /// <summary>
        /// Gets all registered command names
        /// </summary>
        /// <returns>Array of registered command names</returns>
        public string[] GetRegisteredCommands()
        {
            return _registeredCommands.Keys.ToArray();
        }

        private void HandleHelpCommand(GMArgs args) {
            args.SetResult("List of commands:\n" + string.Join('\n', _registeredCommands.Select(x => $"{x.Key} - {x.Value.desc}")));
        }

        private void HandleSetGameSpeedCommand(GMArgs args)
        {
            float speed = args.GetFloat();
            Time.timeScale = speed;
            args.SetResult($"Скорость игры установлена на {speed}x");
        }
        
        #endregion
        
        /// <summary>
        /// Data structure for incoming command requests
        /// </summary>
        [Serializable]
        private class CommandRequest
        {
            public string Command { get; set; }
            public Dictionary<string, string> Arguments { get; set; }
        }
        
        /// <summary>
        /// Data structure for command execution results
        /// </summary>
        [Serializable]
        private class CommandResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public Dictionary<string, object> Data { get; set; }
        }
    }
}
