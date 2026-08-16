@echo off
cd /d E:\Github\LearnTool
dotnet test KidTool.slnx -c Debug --no-build --logger "trx;LogFileName=test_results.trx" --results-directory TestResults > test_run_trx.log 2>&1
echo EXIT=%ERRORLEVEL%
