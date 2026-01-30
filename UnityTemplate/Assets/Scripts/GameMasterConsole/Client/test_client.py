#!/usr/bin/env python3
"""
Test script for GameMaster Console Client

This script can be used to test the SSDP discovery functionality
without requiring a full interactive session.
"""

import sys
import time
from gamemaster_client import SSDPClient, GameMasterClient

def test_ssdp_discovery():
    """Test SSDP discovery functionality"""
    print("Testing SSDP Discovery...")
    print("=" * 40)
    
    ssdp_client = SSDPClient()
    
    print("Searching for GameMaster servers (5 seconds)...")
    servers = ssdp_client.discover_servers(5.0)
    
    if not servers:
        print("❌ No GameMaster servers found")
        return False
    
    print(f"✅ Found {len(servers)} server(s):")
    for i, server in enumerate(servers, 1):
        print(f"  {i}. {server.get('friendly_name', 'GameMaster')} "
              f"at {server['ip']}:{server['port']}")
        print(f"     Server: {server.get('server', 'Unknown')}")
        print(f"     Location: {server['location']}")
    
    return True

def test_server_connection(server_ip: str, server_port: int):
    """Test connection to a specific server"""
    print(f"\nTesting connection to {server_ip}:{server_port}...")
    print("=" * 40)
    
    client = GameMasterClient()
    
    # Manually set server info
    client.current_server = {
        'ip': server_ip,
        'port': server_port,
        'friendly_name': 'Test Server'
    }
    
    # Test a simple command (assuming server has a STATUS command)
    print("Sending STATUS command...")
    success = client.send_command('STATUS', [])
    
    if success:
        print("✅ Connection test successful")
    else:
        print("❌ Connection test failed")
    
    return success

def main():
    """Main test function"""
    print("🧪 GameMaster Console Client Test")
    print("=" * 50)
    
    if len(sys.argv) > 2:
        # Test specific server
        try:
            server_ip = sys.argv[1]
            server_port = int(sys.argv[2])
            test_server_connection(server_ip, server_port)
        except ValueError:
            print("Error: Invalid port number")
            sys.exit(1)
    else:
        # Test discovery
        test_ssdp_discovery()
        
        if len(sys.argv) == 1:
            print("\nUsage:")
            print("  python test_client.py                    # Test SSDP discovery")
            print("  python test_client.py <ip> <port>        # Test specific server")

if __name__ == "__main__":
    main()
