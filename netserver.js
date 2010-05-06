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
		this.tcpServer.BeginAcceptTcpClient( listenerCallback, null );
		queueWorkItem( { callback: raiseListeningEvent, args: [] } );
	}
	
	function listenerCallback( result : IAsyncResult ) {
		print( 'net.server.listenerCallback()' );
		var client : TcpClient = this.tcpServer.EndAcceptTcpClient( result );
		
		// I think we want to raise 'connect' here.
		// queueWorkItem( { callback: requestCallback, args: [ client.GetStream() ] } );
	}
	
	// TODO: pull this stuff out to an event class
	function raiseListeningEvent() {
		print( 'http.server.raiseListeningEvent()' );
		for( var i=0; i < listeningCallbacks.length; i++ ) {
			listeningCallbacks[i]();
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