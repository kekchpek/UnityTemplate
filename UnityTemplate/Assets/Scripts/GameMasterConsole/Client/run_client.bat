@echo off
echo Installing dependencies...
pip install -r requirements.txt

echo.
echo Starting GameMaster Console Client...
python gamemaster_client.py

pause
