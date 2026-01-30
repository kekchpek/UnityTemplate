#!/bin/bash

echo "Installing dependencies..."
pip3 install -r requirements.txt

echo ""
echo "Starting GameMaster Console Client..."
python3 gamemaster_client.py
