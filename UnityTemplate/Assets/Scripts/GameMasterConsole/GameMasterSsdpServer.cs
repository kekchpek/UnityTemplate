using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GMConsole
{
    /// <summary>
    /// SSDP (Simple Service Discovery Protocol) server for advertising GameMaster service on the network
    /// </summary>
    public class GameMasterSsdpServer : IDisposable
    {
        private const string SSDP_MULTICAST_ADDRESS = "239.255.255.250";
        private const int SSDP_MULTICAST_PORT = 1900;
        private const string SERVICE_TYPE = "urn:schemas-armor-guild:service:GameMaster:1";
        
        private readonly string _uniqueServiceName;
        private readonly List<string> _serverLocations;
        private readonly int _serverPort;
        private readonly bool _logMessages;
        private readonly string _computerName;
        
        private UdpClient _udpClient;
        private IPEndPoint _multicastEndpoint;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning;
        private Timer _announcementTimer;
        
        /// <summary>
        /// Creates a new SSDP server for the GameMaster service
        /// </summary>
        /// <param name="serverPort">Port where the GameMaster HTTP server is running</param>
        /// <param name="logMessages">Whether to log SSDP messages</param>
        public GameMasterSsdpServer(int serverPort, bool logMessages = false)
        {
            _serverPort = serverPort;
            _logMessages = logMessages;
            _uniqueServiceName = $"uuid:{Guid.NewGuid()}";
            _computerName = Environment.MachineName;
            
            // Get all local IP addresses for server locations
            var localIPs = GetAllLocalIPAddresses();
            _serverLocations = localIPs.Select(ip => $"http://{ip}:{serverPort}/description.xml").ToList();
        }
        
        /// <summary>
        /// Starts the SSDP server
        /// </summary>
        public void StartServer()
        {
            if (_isRunning)
            {
                Debug.LogWarning("GameMasterSsdpServer is already running");
                return;
            }
            
            try
            {
                _multicastEndpoint = new IPEndPoint(IPAddress.Parse(SSDP_MULTICAST_ADDRESS), SSDP_MULTICAST_PORT);
                _udpClient = new UdpClient();
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, SSDP_MULTICAST_PORT));
                _udpClient.JoinMulticastGroup(IPAddress.Parse(SSDP_MULTICAST_ADDRESS));
                
                _cancellationTokenSource = new CancellationTokenSource();
                _isRunning = true;
                
                // Send initial ALIVE announcements
                SendAliveAnnouncements();
                
                // Set up periodic announcements (every 30 seconds)
                _announcementTimer = new Timer(SendPeriodicAnnouncements, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
                
                // Start listening for M-SEARCH requests
                Task.Run(() => ListenForMSearchRequests(_cancellationTokenSource.Token));
                
                if (_logMessages)
                {
                    Debug.Log($"GameMasterSsdpServer started - advertising service at {_serverLocations.Count} locations: {string.Join(", ", _serverLocations)}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start GameMasterSsdpServer: {ex.Message}");
                StopServer();
            }
        }
        
        /// <summary>
        /// Stops the SSDP server
        /// </summary>
        public void StopServer()
        {
            if (!_isRunning)
                return;
                
            try
            {
                // Send BYEBYE announcements
                SendByebyeAnnouncements();
                
                _announcementTimer?.Dispose();
                _announcementTimer = null;
                
                _cancellationTokenSource?.Cancel();
                
                _udpClient?.DropMulticastGroup(IPAddress.Parse(SSDP_MULTICAST_ADDRESS));
                _udpClient?.Close();
                _udpClient?.Dispose();
                _udpClient = null;
                
                _isRunning = false;
                
                if (_logMessages)
                {
                    Debug.Log("GameMasterSsdpServer stopped");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error stopping GameMasterSsdpServer: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Disposes the SSDP server
        /// </summary>
        public void Dispose()
        {
            StopServer();
            _cancellationTokenSource?.Dispose();
        }
        
        /// <summary>
        /// Listens for M-SEARCH requests and responds appropriately
        /// </summary>
        private async Task ListenForMSearchRequests(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _isRunning)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync();
                    var message = Encoding.UTF8.GetString(result.Buffer);
                    
                    if (_logMessages)
                    {
                        Debug.Log($"Received SSDP message from {result.RemoteEndPoint}: {message}");
                    }
                    
                    if (message.StartsWith("M-SEARCH") && ShouldRespondToMSearch(message))
                    {
                        await SendMSearchResponse(result.RemoteEndPoint);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Expected when stopping
                    break;
                }
                catch (Exception ex)
                {
                    if (_logMessages)
                    {
                        Debug.LogError($"Error in SSDP listener: {ex.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Determines if we should respond to an M-SEARCH request
        /// </summary>
        private bool ShouldRespondToMSearch(string message)
        {
            var lines = message.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("ST:", StringComparison.OrdinalIgnoreCase))
                {
                    var searchTarget = line.Substring(3).Trim();
                    
                    // Respond only to searches for our service type or ssdp:all
                    return searchTarget.Equals(SERVICE_TYPE, StringComparison.OrdinalIgnoreCase) ||
                           searchTarget.Equals("ssdp:all", StringComparison.OrdinalIgnoreCase);
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Sends M-SEARCH response
        /// </summary>
        private async Task SendMSearchResponse(IPEndPoint remoteEndpoint)
        {
            // Send a response for each network interface
            foreach (var serverLocation in _serverLocations)
            {
                var response = BuildSearchResponse(serverLocation);
                var responseBytes = Encoding.UTF8.GetBytes(response);
                
                try
                {
                    using (var responseClient = new UdpClient())
                    {
                        await responseClient.SendAsync(responseBytes, responseBytes.Length, remoteEndpoint);
                        
                        if (_logMessages)
                        {
                            Debug.Log($"Sent M-SEARCH response to {remoteEndpoint} for {serverLocation}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (_logMessages)
                    {
                        Debug.LogError($"Failed to send M-SEARCH response for {serverLocation}: {ex.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Sends initial ALIVE announcements
        /// </summary>
        private void SendAliveAnnouncements()
        {
            // Send SERVICE_TYPE notification for each network interface
            foreach (var serverLocation in _serverLocations)
            {
                var announcement = BuildNotifyMessage(SERVICE_TYPE, serverLocation);
                SendMulticastMessage(announcement);
            }
        }
        
        /// <summary>
        /// Sends BYEBYE announcements
        /// </summary>
        private void SendByebyeAnnouncements()
        {
            // Send SERVICE_TYPE bye-bye notification for each network interface
            foreach (var serverLocation in _serverLocations)
            {
                var announcement = BuildByebyeMessage(SERVICE_TYPE, serverLocation);
                SendMulticastMessage(announcement);
            }
        }
        
        /// <summary>
        /// Sends periodic announcements (called by timer)
        /// </summary>
        private void SendPeriodicAnnouncements(object state)
        {
            if (_isRunning)
            {
                SendAliveAnnouncements();
            }
        }
        
        /// <summary>
        /// Builds NOTIFY message for ALIVE announcements
        /// </summary>
        private string BuildNotifyMessage(string notificationType, string serverLocation)
        {
            return $"NOTIFY * HTTP/1.1\r\n" +
                   $"HOST: {SSDP_MULTICAST_ADDRESS}:{SSDP_MULTICAST_PORT}\r\n" +
                   $"CACHE-CONTROL: max-age=1800\r\n" +
                   $"LOCATION: {serverLocation}\r\n" +
                   $"NT: {notificationType}\r\n" +
                   $"NTS: ssdp:alive\r\n" +
                   $"USN: {_uniqueServiceName}::{notificationType}\r\n" +
                   $"SERVER: GameMaster/1.0 ({_computerName})\r\n" +
                   $"COMPUTER-NAME: {_computerName}\r\n" +
                   $"\r\n";
        }
        
        /// <summary>
        /// Builds NOTIFY message for BYEBYE announcements
        /// </summary>
        private string BuildByebyeMessage(string notificationType, string serverLocation)
        {
            return $"NOTIFY * HTTP/1.1\r\n" +
                   $"HOST: {SSDP_MULTICAST_ADDRESS}:{SSDP_MULTICAST_PORT}\r\n" +
                   $"NT: {notificationType}\r\n" +
                   $"NTS: ssdp:byebye\r\n" +
                   $"USN: {_uniqueServiceName}::{notificationType}\r\n" +
                   $"\r\n";
        }
        
        /// <summary>
        /// Builds M-SEARCH response message
        /// </summary>
        private string BuildSearchResponse(string serverLocation)
        {
            return $"HTTP/1.1 200 OK\r\n" +
                   $"CACHE-CONTROL: max-age=1800\r\n" +
                   $"LOCATION: {serverLocation}\r\n" +
                   $"SERVER: GameMaster/1.0 ({_computerName})\r\n" +
                   $"ST: {SERVICE_TYPE}\r\n" +
                   $"USN: {_uniqueServiceName}::{SERVICE_TYPE}\r\n" +
                   $"COMPUTER-NAME: {_computerName}\r\n" +
                   $"\r\n";
        }
        
        /// <summary>
        /// Sends a multicast message
        /// </summary>
        private void SendMulticastMessage(string message)
        {
            try
            {
                var messageBytes = Encoding.UTF8.GetBytes(message);
                _udpClient.Send(messageBytes, messageBytes.Length, _multicastEndpoint);
                
                if (_logMessages)
                {
                    Debug.Log($"Sent SSDP message: {message.Split('\r')[0]}");
                }
            }
            catch (Exception ex)
            {
                if (_logMessages)
                {
                    Debug.LogError($"Failed to send SSDP message: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Gets all local IP addresses for service locations
        /// </summary>
        private List<string> GetAllLocalIPAddresses()
        {
            var ipAddresses = new List<string>();
            
            try
            {
                foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (netInterface.OperationalStatus == OperationalStatus.Up &&
                        netInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        foreach (var addrInfo in netInterface.GetIPProperties().UnicastAddresses)
                        {
                            if (addrInfo.Address.AddressFamily == AddressFamily.InterNetwork &&
                                !IPAddress.IsLoopback(addrInfo.Address))
                            {
                                ipAddresses.Add(addrInfo.Address.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to get local IP addresses: {ex.Message}");
            }
            
            // Fallback to localhost if no network interfaces found
            if (ipAddresses.Count == 0)
            {
                ipAddresses.Add("127.0.0.1");
            }
            
            return ipAddresses;
        }
    }
}
