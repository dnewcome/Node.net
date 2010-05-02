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

	function HttpServer( requestCallback, prefix ) {
		var httpListener : HttpListener = new HttpListener();
		this.httpListener = httpListener;
		this.prefix = prefix;
		this.requestCallback = requestCallback;
		httpListener.Prefixes.Add( prefix );
		// return this;
	}

	function listen() {
		print( 'HttpServer.listen():' );
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
		this.requestCallback( request, response );
	}

} // class

http.createServer = function( callback, prefix ) {
	return new HttpServer( callback, prefix );
}

// takes a .js file on the commandline, eval it 
var args : String[] = Environment.GetCommandLineArgs();
var sr : StreamReader = new StreamReader( args[1] );
var script = sr.ReadToEnd();
eval( script, 'unsafe' );
sr.Close();