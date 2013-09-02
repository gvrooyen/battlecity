echo off
set pwd=%~dp0
mode con cols=132 lines=1024
echo Cleaning old battles...
cd ../../harness
rmdir battle*-* /s /q
echo Launching battlecity harness...
start launch.bat
choice /C X /T 3 /D X > nul
echo Activating opponent...
cd %pwd%
start start.bat http://localhost:7071/Challenge/ChallengeService random
