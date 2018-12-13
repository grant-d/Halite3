@rem https://github.com/victorkirov/HaliteCSharpStarter
dotnet build
@rem -c Debug
halite.exe --replay-directory replays/ -vvv --width 32 --height 32 "dotnet %cd%\Halite3\bin\Debug\netcoreapp2.0\MyBot.dll" "dotnet %cd%\Halite3\bin\Debug\netcoreapp2.0\MyBot.dll" -s 1544507810
@rem -s 1544687260 64x64 @rem 115k vs 64k https://halite.io/play/?game_id=3067594&replay_class=0&replay_name=replay-20181213-075002%2B0000-1544687260-64-64-3067594
@rem -s 1544507810
@rem -s 1544514780
