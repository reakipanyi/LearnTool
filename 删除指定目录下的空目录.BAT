@echo off
for /f "tokens=*" %%a in ('dir /b /ad /s "E:\Github\LearnTool\"^|sort /r') do rd "%%a" 2>nul
pause
goto :eof
@echo off
for /f "delims=" %%i in ('dir /ad /b /s') do call :rd_dir_empty "%%i"
goto :eof

rem ======= 删除空目录 =========
:rd_dir_empty
rd %1 2>nul||goto :eof
set dir_route=%1
for /f "delims=" %%i in (%dir_route%) do (
set dir_route="%%~dpi"
for /f "delims=" %%j in ('dir /ad /b "%%~dpi"')do rd "%%~dpi%%j" 2>nul||goto :eof
)
if "%dir_route:~-2,1%"=="\" set dir_route="%dir_route:~1,-2%"
if /i not "%cd%"==%dir_route% call :rd_dir_empty %dir_route%
goto :eof