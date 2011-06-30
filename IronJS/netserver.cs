using System.Net.Sockets;
using System.Net;
using System;
using System.Collections;
using System.IO;

using IronJS;
using IronJS.Native;
// using IronJS.Native.Js;
// using IronJS.Native.Utils;

/**
* Net package implements the `net" module in the Node.js API, currently
*	Stream and Server - using the .NET equivalents Stream and TcpListener
*/
namespace Node.Net 
{

	public class NetServer : IronJS.CommonObject
	{
		TcpListener tcpServer;

		ArrayList listeningCallbacks = new ArrayList();
		ArrayList connectionCallbacks = new ArrayList();
		ArrayList closeCallbacks = new ArrayList();

		public NetServer( IronJS.FunctionObject callback, IronJS.Environment env ) : base( env, env.Maps.Base, env.Prototypes.Object ) {
		
			log.Trace( "creating NetServer" );
			
			var objMethod = Utils.createHostFunction<Func<object, string, CommonObject>>(env, listen);
			
			var removeAllListenersMethod = Utils.createHostFunction<Action<string>>( env, removeAllListeners );

			this.Put( "listen", objMethod, TypeTags.Function );
			this.Put( "removeAllListeners", removeAllListenersMethod, TypeTags.Function );
			this.addListener( "connection", callback );
		}
		
		// in Node.js, listen is async, here it blocks on call to Start()
		public CommonObject listen( object in_port, string host ) {
			int port = Convert.ToInt32( in_port );
			log.Trace( "net.Server.listen()" );
			IPAddress ipAddress = Dns.Resolve( host ).AddressList[0];
			this.tcpServer = new TcpListener( ipAddress, port );
			log.Trace( "net.Server.listen(): starting tcp server" );
			this.tcpServer.Start();
			Server.instance.queueWorkItem( new Callback { callback = raiseListeningEvent, args = new object[]{} } );
			this.tcpServer.BeginAcceptTcpClient( listenerCallback, null );
			return this;
		}
		
		public void listenerCallback( IAsyncResult result ) {
			log.Trace( "net.server.listenerCallback()" );
			// TODO: not sure where we should put the call to start listening
			// for the next connection, here for now
			this.tcpServer.BeginAcceptTcpClient( listenerCallback, null );
			
			TcpClient client = this.tcpServer.EndAcceptTcpClient( result );
			NetStream stream = new NetStream( client.GetStream(), Env );
			Server.instance.queueWorkItem( new Callback { callback = raiseConnectionEvent, args = new object[]{ stream } } );
			
			// kick off async read
			stream.read();
		}
		
		// TODO: pull this stuff out to an event class
		public void raiseListeningEvent( object[] args ) {
			log.Trace( "http.server.raiseListeningEvent()" );
			for( var i=0; i < listeningCallbacks.Count; i++ ) {
				IronJS.FunctionObject func = ( IronJS.FunctionObject )listeningCallbacks[i];
				func.Call( this );
			}
		}
		
		public void raiseConnectionEvent( object[] args ) {
			NetStream stream = ( NetStream )args[0];
			log.Trace( "http.server.raiseConnectionEvent() - calling " + connectionCallbacks.Count + " callbacks" );
			
			foreach( object callback in connectionCallbacks ) {
				log.Trace( "calling js function callback" );
				IronJS.FunctionObject func = ( IronJS.FunctionObject )callback;
				func.Call<NetStream>( this, stream );
			}
		}

		public void removeAllListeners( string eventname ) {
			if( eventname == "listening" ) {
				Server.instance.Listeners -= listeningCallbacks.Count;
				listeningCallbacks.Clear();
			}
			else if( eventname == "connection" ) {
				Server.instance.Listeners -= connectionCallbacks.Count;
				connectionCallbacks.Clear();
			}
			else if( eventname == "close" ) {
				Server.instance.Listeners -= closeCallbacks.Count;
				closeCallbacks.Clear();
			}
		}

		public void addListener( string eventname, IronJS.FunctionObject callback) {
			log.Trace( "NetServer - adding listener: " + eventname );
			Server.instance.Listeners++;
			if( eventname == "listening" ) {
				listeningCallbacks.Add( callback );
			}
			else if( eventname == "connection" ) {
				connectionCallbacks.Add( callback );
			}
			else if( eventname == "close" ) {
				closeCallbacks.Add( callback );
			}
			else {
				throw new Exception( "addListener called for unsupported event" );
			}
		}
	} // class

	// provides the 'net' namespace
	// TODO: rename this class 
	public class net : IronJS.CommonObject
	{
		public net( IronJS.Environment env )
			: base( env, env.Maps.Base, env.Prototypes.Object ) {

			var objMethod = Utils.createHostFunction<Func<IronJS.FunctionObject, IronJS.CommonObject>>( Env, CreateServer );
			log.Trace( objMethod );
			this.Put( "createServer", objMethod );
		}

		public IronJS.CommonObject CreateServer( IronJS.FunctionObject callback ) {
			log.Trace( "net.createServer() called." );
			Net.NetServer server = new Net.NetServer( callback, Env );
			log.Trace( server );
			return ( server );
		}
	}

} // package
