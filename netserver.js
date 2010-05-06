/**
* socket implementation for node.net
*/

import System;
import System.Threading;
import System.Net;
import System.Net.Sockets;
import System.IO;

package Net 
{

class net 
{
	static function createServer( callback ) {
		return new NetServer( callback );
	}
}

// wrapper around .net stream
class NetStream
{
	// .NET Stream type
	var stream : Stream;
	var dataCallbacks = [];
	var endCallbacks = [];
	
	// TODO: this should be defined somewhere else
	// Using 1k buffer, experimentally seems to be what node.js uses
	var bufferSize = 1024; 
	
	function NetStream( stream : Stream ) {
		this.stream = stream;
	}

	function Read() {
		var buffer : byte[] = new byte[ bufferSize ];
		this.stream.BeginRead( buffer, 0, bufferSize, ReadCallback, buffer );
	}
	
	function ReadCallback( result : IAsyncResult ) {
		var buffer = byte[]( result.AsyncState );
		var bytesRead = this.stream.EndRead( result );
		
		// TODO: need to implement setEncoding still, all data returned as utf8
		var chunk = System.Text.Encoding.UTF8.GetString( buffer, 0, bytesRead );
		
		// TODO: better checking of buffer size would shave off an extra read
		if( bytesRead > 0 ) {
			// queue work before calling another read, could be out of order otw
			queueWorkItem( { callback: raiseDataEvent, args: [ chunk ] } );
			var nextBuffer : byte[] = new byte[ bufferSize ];
			this.stream.BeginRead( nextBuffer, 0, bufferSize, ReadCallback, nextBuffer );
		}
		else {
			// TODO: node.js docs say end is raised when FIN is sent by 
			// remote end of socket, not sure if raising 'end' when there
			// is no more data is the right thing to do
			raiseEndEvent();
		}
		
	}
	
	function addListener( eventname, callback) {
		if( eventname == 'data' ) {
			dataCallbacks.push( callback );
		}
		else if( eventname == 'end' ) {
			endCallbacks.push( callback );
		}
	}
		
	function raiseDataEvent( chunk ) {
		print( 'net.stream.raiseDataEvent()' );
		for( var i=0; i < dataCallbacks.length; i++ ) {
			dataCallbacks[i]( chunk );
		}
		// Should we delete the chunk after we call listeners?
	}
	
	function raiseEndEvent() {
		print( 'net.stream.raiseEndEvent()' );
		for( var i=0; i < endCallbacks.length; i++ ) {
			endCallbacks[i]();
		}
	}
	

}

class NetServer 
{
	var tcpServer : TcpListener;
	var listeningCallbacks = [];
	var connectionCallbacks = [];
	var closeCallbacks = [];

	function NetServer( callback ) {
		this.addListener( 'connection', callback );
	}
	
	// in Node.js, listen is async, here it blocks on Start()
	function listen( port, host ) {
		var ipAddress : IPAddress = Dns.Resolve( host ).AddressList[0];
		this.tcpServer = new TcpListener( ipAddress, port );
		print( 'net.Server.listen(): starting tcp server' );
		this.tcpServer.Start();
		queueWorkItem( { callback: raiseListeningEvent, args: [] } );
		this.tcpServer.BeginAcceptTcpClient( listenerCallback, null );
	}
	
	function listenerCallback( result : IAsyncResult ) {
		print( 'net.server.listenerCallback()' );
		// TODO: not sure where we should put the call to start listening
		// for the next connection, here for now
		this.tcpServer.BeginAcceptTcpClient( listenerCallback, null );
		
		var client : TcpClient = this.tcpServer.EndAcceptTcpClient( result );
		var stream = new NetStream( client.GetStream() );
		queueWorkItem( { callback: raiseConnectionEvent, args: [ stream ] } );
		
		// kick off async read
		stream.Read();
	}
	
	// TODO: pull this stuff out to an event class
	function raiseListeningEvent() {
		print( 'http.server.raiseListeningEvent()' );
		for( var i=0; i < listeningCallbacks.length; i++ ) {
			listeningCallbacks[i]();
		}
	}
	
	function raiseConnectionEvent( stream ) {
		print( 'http.server.raiseConnectionEvent()' );
		for( var i=0; i < connectionCallbacks.length; i++ ) {
			connectionCallbacks[i]( stream );
		}
	}
	
	function close() {
		
	}
	
	function addListener( eventname, callback) {
		if( eventname == 'listening' ) {
			listeningCallbacks.push( callback );
		}
		else if( eventname == 'connection' ) {
			connectionCallbacks.push( callback );
		}
		else if( eventname == 'close' ) {
			closeCallbacks.push( callback );
		}
		else {
			throw "addListener called for unsupported event";
		}
	}
}
} // package