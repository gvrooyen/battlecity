echo off
set pwd=%~dp0
echo Launching battlecity harness...
cd ../../harness
start launch.bat
ping 1.1.1.1 -n 1 - w 3000 > nul
echo Activating first player...
cd %pwd%
start start.bat http://localhost:7070/Challenge/ChallengeService
echo Activating second player...
start start.bat http://localhost:7071/Challenge/ChallengeService
