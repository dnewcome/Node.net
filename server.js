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

	function HttpServer( prefix ) {
		var httpListener : HttpListener = new HttpListener();
		this.httpListener = httpListener;
		this.prefix = prefix;
		httpListener.Prefixes.Add( prefix );
		// return this;
	}

	function listen() {
		print( 'HttpServer.listen():' );
		this.httpListener.Start();
		print( 'listener started successfully' );
		print( 'httpListener: ' + this.httpListener );
		var result : IAsyncResult = this.httpListener.BeginGetContext( ListenerCallback, httpListener );
		print( 'BeginGetContext called successfully' );
		Thread.Sleep( Timeout.Infinite );
	}
	function ListenerCallback( result : IAsyncResult ) {
		print( 'ListenerCallback():' );
		var listener : HttpListener = HttpListener( result.AsyncState );
		var context : HttpListenerContext = listener.EndGetContext( result );
		var request : HttpListenerRequest = context.Request;
		var response : HttpListenerResponse = context.Response;
		var responseString : String = "<HTML><BODY> Hello world!</BODY></HTML>";
		var buffer : byte[] = System.Text.Encoding.UTF8.GetBytes( responseString );
		response.ContentLength64 = buffer.Length;
		var output : Stream = response.OutputStream;
		output.Write( buffer,0,buffer.Length );
		output.Close();
	}

} // class

http.createServer = function( callback, prefix ) {
	return new HttpServer( prefix );
}

// takes a .js file on the commandline, eval it 
var args : String[] = Environment.GetCommandLineArgs();
var sr : StreamReader = new StreamReader( args[1] );
var script = sr.ReadToEnd();
eval( script, 'unsafe' );
sr.Close();