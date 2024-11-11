echo Starting dotnet program
Start cmd /k dotnet run
Timeout 10
echo Starting python program
py __main__.py
PAUSE