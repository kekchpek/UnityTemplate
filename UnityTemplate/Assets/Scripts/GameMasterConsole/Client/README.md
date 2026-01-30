# GameMaster Console Client

A Python console application for discovering and communicating with GameMaster servers in the Strategic Crafting game.

## Features

- **SSDP Discovery**: Automatically discovers GameMaster servers on the network using SSDP protocol
- **Server Selection**: Handles single server auto-selection or presents a list for manual selection when multiple servers are found
- **Command Interface**: Simple command-line interface for sending commands to the GameMaster server
- **Case Insensitive**: All commands are case-insensitive
- **Client Commands**: Built-in commands for application control

## Installation

1. Make sure you have Python 3.6+ installed
2. Install dependencies:
   ```bash
   pip install -r requirements.txt
   ```

## Usage

Run the client application:
```bash
python gamemaster_client.py
```

### Application Flow

1. **Server Discovery**: The application will search for GameMaster servers on the network
   - If no servers are found, it will continue searching and prompt you to continue or quit
   - If one server is found, it will be automatically selected
   - If multiple servers are found, you'll be presented with a list to choose from

2. **Command Interface**: Once connected, you can send commands to the server
   - Commands follow the format: `COMMAND ARG1 ARG2 ARG3`
   - All commands are case-insensitive

### Built-in Client Commands

- `EXIT` - Close the application
- `RESTART` - Restart the server discovery process (useful if server goes offline or you want to connect to a different server)

### Server Commands

Any other commands will be sent to the GameMaster server. The exact commands available depend on what's registered on the server side.

Example server commands might include:
- `HELP` - Get list of available server commands
- `STATUS` - Get server status
- Custom game-specific commands registered by the game

## Example Session

```
🎮 GameMaster Console Client
========================================
Commands:
  EXIT     - Close application
  RESTART  - Restart server discovery
  <CMD> <ARGS> - Send command to server
========================================
🔍 Searching for GameMaster servers...
✅ Found GameMaster server: Armor Guild Game Master Console at 192.168.1.100:8080

GM[192.168.1.100:8080]> help
✅ Available commands: STATUS, RESTART_LEVEL, SET_GOLD, etc.

GM[192.168.1.100:8080]> status
✅ Server is running normally

GM[192.168.1.100:8080]> set_gold 1000
✅ Player gold set to 1000

GM[192.168.1.100:8080]> restart
🔄 Restarting server discovery...
🔍 Searching for GameMaster servers...
✅ Found GameMaster server: Armor Guild Game Master Console at 192.168.1.100:8080

GM[192.168.1.100:8080]> exit
👋 Goodbye!
```

## Technical Details

### SSDP Discovery Protocol

The client uses SSDP (Simple Service Discovery Protocol) to find GameMaster servers:
- Sends M-SEARCH requests to multicast address `239.255.255.250:1900`
- Looks for servers with service type `urn:schemas-armor-guild:service:GameMaster:1`
- Also searches for device type `urn:schemas-armor-guild:device:GameMasterConsole:1`
- Retrieves server descriptions from UPnP device description XML

### Communication Protocol

Commands are sent to the server via HTTP POST requests to `/command` endpoint:
- Content-Type: `application/json`
- Request format:
  ```json
  {
    "Command": "COMMAND_NAME",
    "Arguments": {
      "0": "arg1",
      "1": "arg2",
      "2": "arg3"
    }
  }
  ```

### Error Handling

The client handles various error conditions gracefully:
- Network connectivity issues
- Server timeouts
- Invalid server responses
- User interruption (Ctrl+C)

## Requirements

- Python 3.6+
- `requests` library (for HTTP communication)
- Network access to GameMaster server

## Troubleshooting

### No Servers Found
- Ensure the GameMaster server is running and has SSDP enabled
- Check that both client and server are on the same network
- Verify firewall settings allow multicast traffic on port 1900
- Try running the server discovery multiple times

### Connection Errors
- Verify the server IP and port are correct
- Check that the GameMaster server is accepting HTTP connections
- Ensure no firewall is blocking the connection

### Command Failures
- Check that the command name is correct (case doesn't matter)
- Verify the command is registered on the server
- Check the number and format of arguments
