:: may need to update paths to v8 include path and vc installation
:: pass `env' as the first arg to set up compiler paths for microsoft cl.exe
if "%1"=="env" (
call "c:\Program Files (x86)\Microsoft Visual Studio 10.0\vc\bin\vcvars32.bat"
)

cl /DUSING_V8_SHARED /IC:\Users\dan\Desktop\sandbox\v8\include /clr TcpServer.cc v8.lib wsock32.lib winmm.lib ws2_32.lib