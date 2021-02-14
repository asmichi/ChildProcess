@echo off
setlocal
rem Invoke-CommandInVsDevCmd.cmd [path to VsDevCmd.bat] [path to command] [arg1] [arg2] ... [arg7]
rem Invoke a command within the VsDevCmd environment
call %1 -no_logo -arch=amd64 -host_arch=amd64
%2 %3 %4 %5 %6 %7 %8 %9

endlocal