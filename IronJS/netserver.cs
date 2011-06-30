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
	public class NetStream : IronJS.CommonObject
	{
		// XXX making stream public to work around incomplete design - fix this
		public Stream stream;

		// TODO: callbacks will be JS functions, should probably use 
		// List<IFunction>
		ArrayList dataCallbacks = new ArrayList();
		ArrayList endCallbacks = new ArrayList();
		
		// TODO: this should be defined somewhere else
		// Using 1k buffer - experimentally, seems to be what node.js uses
		int bufferSize = 1024; 
		
		public NetStream( Stream stream, IronJS.Environment env ) : base( env, env.Maps.Base, env.Prototypes.Object ) {
			this.stream = stream;
			var objMethod = Utils.CreateFunction<Action<string, IronJS.FunctionObject>>( env, 0, addListener );
			this.Put( "addListener", objMethod, DescriptorAttrs.Immutable );

			var objRemoveMethod = Utils.CreateFunction<Action<string>>( env, 0, removeAllListeners );
			this.Put( "removeAllListeners", objRemoveMethod, DescriptorAttrs.Immutable );
		}

		public void read() {
			byte[] buffer = new byte[ bufferSize ];
			this.stream.BeginRead( buffer, 0, bufferSize, ReadCallback, buffer );
		}
			
		public void ReadCallback( IAsyncResult result ) {
			byte[] buffer = ( byte[] )result.AsyncState;
			int bytesRead = this.stream.EndRead( result );
			log.Trace( "ReadCallback(): read " + bytesRead + "bytes from stream" );
			// TODO: need to implement setEncoding still, all data returned as utf8
			string chunk = System.Text.Encoding.UTF8.GetString( buffer, 0, bytesRead );
			
			if( bytesRead > 0 ) {
				// queue work before calling another read, could be out of order otw
				Server.instance.queueWorkItem( new Callback() { name = "stream:raiseDataEvent", callback = raiseDataEvent, args = new object[] { chunk } } );
				byte[] nextBuffer = new byte[ bufferSize ];
				this.stream.BeginRead( nextBuffer, 0, bufferSize, ReadCallback, nextBuffer );
			}
			else {
				// TODO: node.js docs say end is raised when FIN is sent by 
				// remote end of socket, not sure if raising "end" when there
				// is no more data is the right thing to do. It does the right thing
				// when using Telnet as the client 
				Server.instance.queueWorkItem( new Callback() { name = "stream:raiseEndEvent", callback = raiseEndEvent, args = new object[]{} } );
			}
		}

		// TODO: encoding is assumed to be utf8, need to implement encoding types
		public void write( string chunk ) {
			byte[] buffer = System.Text.Encoding.UTF8.GetBytes( chunk );
			this.stream.Write( buffer, 0, buffer.Length );
		}
		
		public void end() {
            log.Trace( "closing stream" );
			this.stream.Close();
		}
		public void addListener( string eventname, IronJS.FunctionObject callback ) {
			Server.instance.Listeners++;
			log.Trace( "NetStream: adding listener: " + eventname );
			if( eventname == "data" ) {
				dataCallbacks.Add( callback );
			}
			else if( eventname == "end" ) {
				endCallbacks.Add( callback );
			}
		}
		public void removeAllListeners( string eventname ) {
			if( eventname == "data" ) {
				Server.instance.Listeners -= dataCallbacks.Count;
				dataCallbacks.Clear();
			}
			else if( eventname == "end" ) {
				Server.instance.Listeners -= endCallbacks.Count;
				endCallbacks.Clear();
			}
		}
		
		public void raiseDataEvent( object[] args ) {
			string chunk = ( string )args[0];
			log.Trace( "net.stream.raiseDataEvent():" + chunk );
			for( var i=0; i < dataCallbacks.Count; i++ ) {
				// ( ( IronJS.FunctionObject )dataCallbacks[i] ).Call( this, new object[] { chunk } );
				IronJS.FunctionObject func = ( IronJS.FunctionObject )dataCallbacks[i];
				/*
				Action<IronJS.FunctionObject,IronJS.CommonObject,string> fun = 
					func.Compiler.compileAs<Action<IronJS.FunctionObject,IronJS.CommonObject,string>>(func);
				// fun.Invoke(func, func.Env.Globals, new object[] {chunk} );
				log.Trace( "invoking JS raiseDataEvent callback" );
				fun.Invoke(func, this, chunk );
				 */
				func.Call<string>( this, chunk );
			}
		}
		
		public void raiseEndEvent( object[] args ) {
			log.Trace( "net.stream.raiseEndEvent()" );
			for( var i=0; i < endCallbacks.Count; i++ ) {
				// ( ( IFunction )endCallbacks[i] ).Call( this, new object[] {} );
				IronJS.FunctionObject func = ( IronJS.FunctionObject )endCallbacks[i];
				/*
				Action<IronJS.FunctionObject,IronJS.CommonObject,object[]> fun = 
					func.Compiler.compileAs<Action<IronJS.FunctionObject,IronJS.CommonObject,object[]>>(func);
				fun.Invoke(func, func.Env.Globals, new object[] {} );
				 */
				func.Call( this );
			}
		}
		
	} // class

	public class NetServer : IronJS.CommonObject
	{
		TcpListener tcpServer;

		ArrayList listeningCallbacks = new ArrayList();
		ArrayList connectionCallbacks = new ArrayList();
		ArrayList closeCallbacks = new ArrayList();

		public NetServer( IronJS.FunctionObject callback, IronJS.Environment env ) : base( env, env.Maps.Base, env.Prototypes.Object ) {
			// have to set this stuff up. not sure why yet
			
			// Prototype = context.ObjectConstructor.Object_prototype;
			// Class = ObjClass.Array;

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
				// ( ( IFunction )listeningCallbacks[i] ).Call( this, new object[]{} );
				IronJS.FunctionObject func = ( IronJS.FunctionObject )listeningCallbacks[i];
				/*
				Action<IronJS.FunctionObject,IronJS.CommonObject,object[]> fun = 
					func.Compiler.compileAs<Action<IronJS.FunctionObject,IronJS.CommonObject,object[]>>(func);
				fun.Invoke(func, func.Env.Globals, new object[] {} );
				*/
				func.Call( this );
			}
		}
		
		public void raiseConnectionEvent( object[] args ) {
			NetStream stream = ( NetStream )args[0];
			log.Trace( "http.server.raiseConnectionEvent() - calling " + connectionCallbacks.Count + " callbacks" );
			
			foreach( object callback in connectionCallbacks ) {
				log.Trace( "calling js function callback" );
				// ( ( IFunction )callback ).Call( this, new object[] { stream } );
				IronJS.FunctionObject func = ( IronJS.FunctionObject )callback;
				/*
				Action<IronJS.FunctionObject,IronJS.CommonObject,IronJS.CommonObject> fun = 
					func.Compiler.compileAs<Action<IronJS.FunctionObject,IronJS.CommonObject,IronJS.CommonObject>>(func);
				fun.Invoke(func, this, stream );
				*/
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
