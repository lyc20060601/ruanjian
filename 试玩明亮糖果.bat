@echo off
if not exist "%~dp0Builds\CandyOpen\JellyWorld.exe" (
  echo Build the game in Unity first: Jelly World ^> Build Windows playtest.
  pause
  exit /b 1
)
start "" /D "%~dp0Builds\CandyOpen" "%~dp0Builds\CandyOpen\JellyWorld.exe" -screen-fullscreen 0 -screen-width 1600 -screen-height 900
