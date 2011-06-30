using System.Net.Sockets;
using System.Net;
using System;
using System.Collections;
using System.Collections.Generic;
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

	public class NetServer : EventEmitter
	{
		TcpListener tcpServer;

		
		public NetServer( IronJS.FunctionObject callback, IronJS.Environment env ) : base( env ) {

			// TODO: move as much of this as possible to EventEmitter
			Callbacks = new Dictionary<string, ArrayList>() { 
				// { "listen", new ArrayList() },
				{ "connect", new ArrayList() },
				{ "close", new ArrayList() }
			};

			log.Trace( "creating NetServer" );
			
			var objMethod = Utils.createHostFunction<Func<object, string, CommonObject>>(env, listen);
			
			var removeAllListenersMethod = Utils.createHostFunction<Action<string>>( env, removeAllListeners );

			this.Put( "listen", objMethod, TypeTags.Function );
			this.Put( "removeAllListeners", removeAllListenersMethod, TypeTags.Function );
			this.addListener( "connect", callback );
		}
		
		// in Node.js, listen is async, here it blocks on call to Start()
		public CommonObject listen( object in_port, string host ) {
			int port = Convert.ToInt32( in_port );
			log.Trace( "net.Server.listen()" );
			IPAddress ipAddress = Dns.Resolve( host ).AddressList[0];
			this.tcpServer = new TcpListener( ipAddress, port );
			log.Trace( "net.Server.listen(): starting tcp server" );
			this.tcpServer.Start();
			
			// TODO: 'listening' is not an event. Slated for removal.
			// Server.instance.queueWorkItem( new Callback { callback = raiseListeningEvent, args = new object[]{} } );
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

		// TODO: 'listen/listening' is not an event. Not sure why this is in here
		// slated for removal.
		/*
		public void raiseListeningEvent( object[] args ) {
			log.Trace( "http.server.raiseListeningEvent()" );
			for( var i=0; i < Callbacks["listen"].Count; i++ ) {
				IronJS.FunctionObject func = ( IronJS.FunctionObject )Callbacks["listen"][i];
				func.Call( this );
			}
		}
		*/

		public void raiseConnectionEvent( object[] args ) {
			NetStream stream = ( NetStream )args[0];
			log.Trace( "http.server.raiseConnectionEvent() - calling " + Callbacks["connect"].Count + " callbacks" );
			emit( "connect", new BoxedValue[] { IronJS.TypeConverter.ToBoxedValue( stream ) } );
			/*
			foreach( object callback in Callbacks["connect"] ) {
				log.Trace( "calling js function callback" );
				IronJS.FunctionObject func = ( IronJS.FunctionObject )callback;
				func.Call<NetStream>( this, stream );
			}
			*/
		}

		public void addListener( string eventname, IronJS.FunctionObject callback) {
			log.Trace( "NetServer - adding listener: " + eventname );
			on( eventname, callback );
			/*
			Server.instance.Listeners++;
			// TODO: listen is not an event .. remove this
			if( eventname == "listen" ) {
				Callbacks["listen"].Add( callback );
			}
			else if( eventname == "connect" ) {
				Callbacks["connect"].Add( callback );
			}
			else if( eventname == "close" ) {
				Callbacks["close"].Add( callback );
			}
			else {
				throw new Exception( "addListener called for unsupported event" );
			}
			*/
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
