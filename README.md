# Goals

- Same theory of operation as Node.js (single threaded eventing front-end, non-blocking IO back-end)
- API compatibility with Node.js
- Written entirely in Javascript (JScript.NET)
- Runs on the .NET runtime

# What is implemented

- Enough of the `http’ module to implement a basic server that handles post data and sends back response
- Enough of the `net' module to implement a TCP server that echoes requests to stdout
- Enough of the `stream' api to support the above
-require()
-sys.puts()

# Limitations

HTTP requests are cached by .NET even though the stream is read async. This was 
a limitation of using HttpListener in .NET. The TCP server is fully streaming however.
Writes are implemented as blocking calls currently. I didn't have time to 
implement write queues to enforce write ordering.

# Usage

On Windows:

If you have the .NET 2.0 framework installed (likely if you are on a Windows box), 
just run the build script. If you'd rather build against a different framework 
version, you can alter the path to jsc.exe.

	C:\> node.exe server.js

On Linux, under Mono:

	C:\> mono node.exe server.js

In order to run under Mono, you'll need Microsoft's version of Microsoft.JScript.dll, 
which is included in the .NET framework. I can't provide it here since it is not 
redistributable individually.

# Examples

Running an HTTP server that prints post data to the console and replies with an 
`All finished' message when the request completes:

	var sys = require( 'sys' ), http = require( 'http' );
	http.createServer( function( request, response ) {
		request.addListener( 'data', function( data ) {
			sys.puts( data );
		});
		request.addListener( 'end', function() {
			response.write( '<html><body><p>All finished!<p></body></html>' );
			response.end();
		});
	}).listen( 9981, 'localhost' );

To test it out, try something like this:

	C:\>curl http://localhost:9981 -d "hello"
	<html><body><p>All finished!<p></body></html>

Running a TCP server that listens on port 9982 and writes data sent to the console:

	var sys = require( 'sys' ), net = require( 'net' );
	net.createServer( function( stream ) {
		stream.addListener( 'data', function( data ) {
			sys.puts( data );
		});
	}).listen( 9982, 'localhost' );

To test out the TCP server try sending it some data using telnet:

	C:\>telnet localhost 9982

The .NET framework can be accessed from within the Javascript that runs under Node.net.
For example, the TCP server shown above could have used Console.WriteLine() in 
order to write its output to the console:

	var net = require( 'net' );
	net.createServer( function( stream ) {
		stream.addListener( 'data', function( data ) {
			System.Console.WriteLine( data );
		});
	}).listen( 9982, 'localhost' );

# Building from source

On Windows:

C:\> build.bat

On Linux

Mono's mjs compiler doesn't compile Node.net.

# Future work

- Implement non-blocking writes
- HTTP implementation should be rewritten to use the `net' module to allow streaming
- Implement more of the Node.js API (filesystem, process, etc.)

# License 

Node.js is provided under the MIT free software license. See the file LICENSE 
for the full text.
Copyright 2010 Dan Newcome