import System;
import System.Net;

/**
* Http package implements the `http' module in the Node.js API, currently
*	Server, ServerReqeust, ServerResponse - essentially wrapping the Node.js
*	interface around the .NET equivalents
*/
package Http {

// Forms the `http' namespace
class http {
	static function createServer( callback ) {
		return new HttpServer( callback );
	}
}

class HttpServer 
{
	var httpListener : HttpListener;
	var requestCallbacks = [];
	
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
			throw "addListener called for unsupported event: " + eventname;
		}
	}
	
	function HttpServer( requestCallback ) {
		var httpListener : HttpListener = new HttpListener();
		this.httpListener = httpListener;
		addListener( 'request', requestCallback );
	}

	function listen( port, host ) {
		print( 'HttpServer.listen():' );
		var prefix = 'http://' + host + ':' + port + '/';
		httpListener.Prefixes.Add( prefix );
		httpListener.Start();
		httpListener.BeginGetContext( ListenerCallback, httpListener );
	}
	
	function ListenerCallback( result : IAsyncResult ) {
		print( 'ListenerCallback():' );
		
		// TODO: we should probably set up the next call somewhere else
		this.httpListener.BeginGetContext( ListenerCallback, httpListener );
		
		var context : HttpListenerContext = httpListener.EndGetContext( result );		
		var httpServerRequest = new HttpServerRequest( context.Request );
		var httpServerResponse = new HttpServerResponse( context.Response );
		queueWorkItem( { 
			callback: raiseRequestEvent, 
			args: [ httpServerRequest, httpServerResponse ] 
		} );
		
		// we can start reading now without queueing on dispatch, since the data
		// will not be dispatched until after the request event fires
		httpServerRequest.Read();
	}

} // class

class HttpServerRequest 
{
	var netStream;
	var httpListenerRequest : HttpListenerRequest;
	
	function HttpServerRequest( request : HttpListenerRequest ) {
		httpListenerRequest = request;
		netStream = new NetStream( request.InputStream );
	}
	
	// Note: is not part of the original Node.js api, used only to 
	// kick off data events after server request event is queued. We denote
	// this by using uppercase method name
	function Read() {
		print( 'HttpServerRequest.Read()' );
		netStream.read();
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

} // class

class HttpServerResponse
{
	var httpResponse : HttpListenerResponse;
	var netStream;
	
	function HttpServerResponse( httpResponse ) {
		this.httpResponse = httpResponse;
		this.netStream = new NetStream( httpResponse.OutputStream );
	}
	
	function writeHead( statusCode, headers ) {
		throw "writeHead unimplemented";
	}
	
	function end() {
		var output : Stream = httpResponse.OutputStream;
		output.Close();
	}
	
	function write( chunk, encoding ) {
		print( 'HttpServerResponse.write(): ' + chunk );
		netStream.write( chunk, encoding );
	}
} // class

} // package