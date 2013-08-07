echo off
set pwd=%~dp0
echo Cleaning old battles...
cd ../../harness
rmdir battle*-*
echo Launching battlecity harness...
start launch.bat
rem ping 1.1.1.1 -n1 -w5000 > nul
choice /C X /T 3 /D X > nul
echo Activating first player...
cd %pwd%
start start.bat http://localhost:7070/Challenge/ChallengeService
echo Activating second player...
start start.bat http://localhost:7071/Challenge/ChallengeService
