echo off
copy ..\monitor\bin\Debug\monitor.exe .
set pwd=%~dp0
echo Cleaning old battles...
cd ../../harness
rmdir battle*-* /s /q
echo Launching battlecity harness...
start launch.bat
choice /C X /T 3 /D X > nul
echo Activating first player...
cd %pwd%
start start_debug.bat http://localhost:7070/Challenge/ChallengeService ctf
echo Activating second player...
start start_debug.bat http://localhost:7071/Challenge/ChallengeService random
choice /C X /T 3 /D X > nul
echo Starting Player One's monitor
start run_monitor.bat
choice /C X /T 3 /D X > nul
echo Done.
