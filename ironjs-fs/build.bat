csc ^
	/r:lib\IronJS.dll ^
	/r:lib\IronJS.Runtime.dll ^
	/r:lib\http_parser.dll ^
	/debug:full ^
	/out:bin\node.exe ^
	netserver.cs ^
	httpserver.cs ^
	server.cs 

copy lib\*.dll bin
