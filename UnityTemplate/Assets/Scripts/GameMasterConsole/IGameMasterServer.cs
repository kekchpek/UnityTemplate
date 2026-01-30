namespace GMConsole 
{
    public interface IGameMasterServer 
    {
        void StartServer(
            int port = 54345, 
            bool allowNetworkAccess = true, 
            bool logRequests = true,
            bool enableSsdp = true);
        void StopServer();
    }
}