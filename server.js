/**
* Server is a .net executable written in js, compiled to .net using jsc
*/
import System;
import System.Threading;
import System.Net;
import System.IO;

var http = {};

class HttpServer 
{
	var httpListener : HttpListener;
	var prefix;
	var requestCallback;
	var autoResetEvent : AutoResetEvent = new AutoResetEvent( false );

	function HttpServer( requestCallback ) {
		var httpListener : HttpListener = new HttpListener();
		this.httpListener = httpListener;
		this.requestCallback = requestCallback;
	}

	function listen( port, host ) {
		print( 'HttpServer.listen():' );
		var prefix = 'http://' + host + ':' + port + '/';
		this.httpListener.Prefixes.Add( prefix );
		this.httpListener.Start();
		while( true ) {
			var result : IAsyncResult = this.httpListener.BeginGetContext( ListenerCallback, httpListener );
			autoResetEvent.WaitOne();
		}
	}
	
	function ListenerCallback( result : IAsyncResult ) {
		print( 'ListenerCallback():' );
		autoResetEvent.Set();

		var listener : HttpListener = HttpListener( result.AsyncState );
		var context : HttpListenerContext = listener.EndGetContext( result );
		var request : HttpListenerRequest = context.Request;
		var response : HttpListenerResponse = context.Response;
		
		// var httpServerRequest = new HttpServerRequest( request );
		var httpServerResponse = new HttpServerResponse( response );
		
		this.requestCallback( request, httpServerResponse );
	}

} // class


// wraps .net request with node.js interface
class HttpServerRequest 
{
	var dataCallbacks = [];
	var endCallbacks = [];
	
	function addListener( eventname, callback) {
		if( eventname == 'data' ) {
			dataCallbacks.push( callback );
		}
		else if( eventname == 'end' ) {
			endCallbacks.push( callback );
		}
		else {
			throw "addListener called for unsupported event";
		}

	}
}

class HttpServerResponse
{
	var httpResponse : HttpListenerResponse;
	
	function HttpServerResponse( httpResponse ) {
		this.httpResponse = httpResponse;
	}
	
	function writeHead( statusCode, headers ) {
	
	}
	
	function end() {
		var output : Stream = httpResponse.OutputStream;
		output.Close();
	}
	
	// default encoding is UTF8
	function write( chunk, encoding ) {
		// TODO: not sure how .net handles js strings. everything works if we 
		// don't explicitly type chunkString, but I'm not sure about things yet
		var chunkString : String = chunk;
		var buffer : byte[] = System.Text.Encoding.UTF8.GetBytes( chunkString );
		httpResponse.ContentLength64 += buffer.Length;
		
		var output : Stream = httpResponse.OutputStream;
		output.Write( buffer, 0, buffer.Length );
	}
}

http.createServer = function( callback ) {
	return new HttpServer( callback );
}

// takes a .js file on the commandline, eval it 
var args : String[] = Environment.GetCommandLineArgs();
var sr : StreamReader = new StreamReader( args[1] );
var script = sr.ReadToEnd();
eval( script, 'unsafe' );
sr.Close();