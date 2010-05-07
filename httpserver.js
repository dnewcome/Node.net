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
			throw "addListener called for unsupported event:" + eventname;
		}
	}
	
	function HttpServer( requestCallback ) {
		var httpListener : HttpListener = new HttpListener();
		this.httpListener = httpListener;
		this.addListener( 'request', requestCallback );
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
		
		// we can start reading now without queueing on dispatch, since the data
		// will not be dispatched until after the request event fires
		httpServerRequest.Read();
	}

} // class


// wraps .net request with node.js interface
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

}

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
	// default encoding is UTF8
	/*
	function write( chunk, encoding ) {
		// TODO: not sure how .net handles js strings. everything works if we 
		// don't explicitly type chunkString, but I'm not sure about things yet
		var chunkString : String = chunk;
		var buffer : byte[] = System.Text.Encoding.UTF8.GetBytes( chunkString );
		httpResponse.ContentLength64 += buffer.Length;
		
		var output : Stream = httpResponse.OutputStream;
		
		// TODO: we are using a blocking call to write() - need to make this
		// async
		output.Write( buffer, 0, buffer.Length );
	}
	*/
}

// createServer must be encapsulated in a class. We choose http as the class
// name to match the api, not sure if this is a good idea or not now that we
// have 'require' working
class http {
	static function createServer( callback ) {
		return new HttpServer( callback );
	}
}
} // package