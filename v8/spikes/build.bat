:: may need to update paths to v8 include path and vc installation

if "%1"=="env" (
call "c:\Program Files (x86)\Microsoft Visual Studio 10.0\vc\bin\vcvars32.bat"
)

cl /IC:\Users\dan\Desktop\sandbox\v8\include  /clr %1 ..\v8.lib wsock32.lib winmm.lib ws2_32.lib