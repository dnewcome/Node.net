import System;
import System.Threading;
import System.Net;
import System.IO;

package Http {
class HttpServer 
{
	var httpListener : HttpListener;
	var prefix;
	var requestCallbacks = [];
	var requestCallback;
	
	function raiseRequestEvent( req, res ) {
		print( 'http.server.raiseRequestEvent()' );
		for( var i=0; i < requestCallbacks.length; i++ ) {
			requestCallbacks[i]( req, res );
		}
	}
	
	function addListener( eventname, callback) {
		if( eventname == 'request' ) {
			requestCallbacks.push( callback );
		}
		else {
			throw "addListener called for unsupported event";
		}
	}
	
	function HttpServer( requestCallback ) {
		var httpListener : HttpListener = new HttpListener();
		this.httpListener = httpListener;
		this.addListener( 'request', requestCallback );
		this.requestCallback = requestCallback;
	}

	function listen( port, host ) {
		print( 'HttpServer.listen():' );
		var prefix = 'http://' + host + ':' + port + '/';
		this.httpListener.Prefixes.Add( prefix );
		this.httpListener.Start();
		var result : IAsyncResult = this.httpListener.BeginGetContext( ListenerCallback, httpListener );
	}
	
	function ListenerCallback( result : IAsyncResult ) {
		print( 'ListenerCallback():' );
		
		// TODO: we should probably set up the next call somewhere else
		this.httpListener.BeginGetContext( ListenerCallback, httpListener );
		
		var listener : HttpListener = HttpListener( result.AsyncState );
		var context : HttpListenerContext = listener.EndGetContext( result );
		var request : HttpListenerRequest = context.Request;
		var response : HttpListenerResponse = context.Response;
		
		var httpServerRequest = new HttpServerRequest( request );
		var httpServerResponse = new HttpServerResponse( response );
		queueWorkItem( { callback: raiseRequestEvent, args: [ httpServerRequest, httpServerResponse ] } );
		
		httpServerRequest.Read();
	}

} // class


// wraps .net request with node.js interface
class HttpServerRequest 
{
	// var dataCallbacks = [];
	// var endCallbacks = [];
	var netStream;
	var httpListenerRequest : HttpListenerRequest;
	
	function HttpServerRequest( request : HttpListenerRequest ) {
		httpListenerRequest = request;
		netStream = new NetStream( request.InputStream );
	}
	
	// Note: is not part of the original Node.js api, used only to 
	// kick off data events after server request event is queued
	function Read() {
		netStream.Read();
	}
	
	// pass through event listeners to underlying stream
	function addListener( eventname, callback) {
		if( eventname == 'data' ) {
			netStream.addListener( 'data', callback );
		}
		else if( eventname == 'end' ) {
			netStream.addListener( 'end', callback );
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

// createServer must be encapsulated in a class
class http {
	static function createServer( callback ) {
		return new HttpServer( callback );
	}
}
} // package