@rem https://github.com/victorkirov/HaliteCSharpStarter
dotnet build
@rem -c Debug
halite.exe --replay-directory replays/ -vvv --width 32 --height 32 "dotnet %cd%\Halite3\bin\Debug\netcoreapp2.0\MyBot.dll" "dotnet %cd%\Halite3\bin\Debug\netcoreapp2.0\MyBot.dll" -s 1544507810
@rem -s 1544507810
@rem -s 1544514780
