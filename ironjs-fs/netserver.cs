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
namespace Net 
{
	public class NetStream : IronJS.Object
	{
		Stream stream;
		// TODO: callbacks will be JS functions, should probably use 
		// List<IFunction>
		ArrayList dataCallbacks = new ArrayList();
		ArrayList endCallbacks = new ArrayList();
		
		// TODO: this should be defined somewhere else
		// Using 1k buffer - experimentally, seems to be what node.js uses
		int bufferSize = 1024; 
		
		public NetStream( Stream stream, IronJS.Environment env ) : base( env, env.Maps.Base, env.Prototypes.Object, IronJS.Classes.Object ) {

			// have to set this stuff up to satisfy IObj. not sure why yet
			Env = env;
			Methods = Env.Methods.Object;
			
			// IronJS objects set up the following also, not sure if we
			// really need this stuff though
			// Prototype = context.ObjectConstructor.Object_prototype;
			// Class = ObjClass.Array;
			
			this.stream = stream;
			
			var objMethod = IronJS.Api.HostFunction.create<Action<string, IronJS.Function>>(env, addListener);
            this.Methods.PutRefProperty(this, "addListener", objMethod, IronJS.TypeTags.Function);
           
			// this.SetOwnProperty( "addListener", new Action<string, IFunction>( addListener ) );
		}

		public void read() {
			byte[] buffer = new byte[ bufferSize ];
			this.stream.BeginRead( buffer, 0, bufferSize, ReadCallback, buffer );
		}
			
		public void ReadCallback( IAsyncResult result ) {
			byte[] buffer = ( byte[] )result.AsyncState;
			int bytesRead = this.stream.EndRead( result );
			Console.WriteLine( "ReadCallback(): read " + bytesRead + "bytes from stream" );
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
			this.stream.Close();
		}
		public void addListener( string eventname, IronJS.Function callback ) {
			if( eventname == "data" ) {
				dataCallbacks.Add( callback );
			}
			else if( eventname == "end" ) {
				endCallbacks.Add( callback );
			}
		}
			
		public void raiseDataEvent( object[] args ) {
			string chunk = ( string )args[0];
			Console.WriteLine( "net.stream.raiseDataEvent():" + chunk );
			for( var i=0; i < dataCallbacks.Count; i++ ) {
				// ( ( IronJS.Function )dataCallbacks[i] ).Call( this, new object[] { chunk } );
				IronJS.Function func = ( IronJS.Function )dataCallbacks[i];
				Action<IronJS.Function,IronJS.Object,object[]> fun = 
					func.Compiler.compileAs<Action<IronJS.Function,IronJS.Object,object[]>>(func);
				fun.Invoke(func, func.Env.Globals, new object[] {chunk} );
			}
		}
		
		public void raiseEndEvent( object[] args ) {
			Console.WriteLine( "net.stream.raiseEndEvent()" );
			for( var i=0; i < endCallbacks.Count; i++ ) {
				// ( ( IFunction )endCallbacks[i] ).Call( this, new object[] {} );
				IronJS.Function func = ( IronJS.Function )endCallbacks[i];
				Action<IronJS.Function,IronJS.Object,object[]> fun = 
					func.Compiler.compileAs<Action<IronJS.Function,IronJS.Object,object[]>>(func);
				fun.Invoke(func, func.Env.Globals, new object[] {} );
			}
		}
		
	} // class

	public class NetServer : IronJS.Object
	{
		TcpListener tcpServer;
		ArrayList listeningCallbacks = new ArrayList();
		ArrayList connectionCallbacks = new ArrayList();
		ArrayList closeCallbacks = new ArrayList();

		
		
		public NetServer( IronJS.Function callback, IronJS.Environment env ) : base( env, env.Maps.Base, env.Prototypes.Object, IronJS.Classes.Object ) {
			// have to set this stuff up. not sure why yet
			Env = env;
			Methods = Env.Methods.Object;
			
			// Prototype = context.ObjectConstructor.Object_prototype;
			// Class = ObjClass.Array;

			Console.WriteLine( "creating NetServer" );
			
			//this.SetOwnProperty( "listen", new Action<object, string>( listen ) );
			var objMethod = IronJS.Api.HostFunction.create<Action<object, string>>(env, listen);
            this.Methods.PutRefProperty(this, "listen", objMethod, IronJS.TypeTags.Function);
			
			this.addListener( "connection", callback );
		}
		
		// in Node.js, listen is async, here it blocks on call to Start()
		public void listen( object in_port, string host ) {
			int port = Convert.ToInt32( in_port );
			Console.WriteLine( "net.Server.listen()" );
			IPAddress ipAddress = Dns.Resolve( host ).AddressList[0];
			this.tcpServer = new TcpListener( ipAddress, port );
			Console.WriteLine( "net.Server.listen(): starting tcp server" );
			this.tcpServer.Start();
			Server.instance.queueWorkItem( new Callback { callback = raiseListeningEvent, args = new object[]{} } );
			this.tcpServer.BeginAcceptTcpClient( listenerCallback, null );
		}
		
		public void listenerCallback( IAsyncResult result ) {
			Console.WriteLine( "net.server.listenerCallback()" );
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
			Console.WriteLine( "http.server.raiseListeningEvent()" );
			for( var i=0; i < listeningCallbacks.Count; i++ ) {
				// ( ( IFunction )listeningCallbacks[i] ).Call( this, new object[]{} );
				IronJS.Function func = ( IronJS.Function )listeningCallbacks[i];
				Action<IronJS.Function,IronJS.Object,object[]> fun = 
					func.Compiler.compileAs<Action<IronJS.Function,IronJS.Object,object[]>>(func);
				fun.Invoke(func, func.Env.Globals, new object[] {} );

			}
		}
		
		public void raiseConnectionEvent( object[] args ) {
			NetStream stream = ( NetStream )args[0];
			Console.WriteLine( "http.server.raiseConnectionEvent()" );
			Console.WriteLine( "http.server.raiseConnectionEvent() - calling " + connectionCallbacks.Count + " callbacks" );
			
			foreach( object callback in connectionCallbacks ) {
				// ( ( IFunction )callback ).Call( this, new object[] { stream } );
				IronJS.Function func = ( IronJS.Function )callback;
				Action<IronJS.Function,IronJS.Object,object[]> fun = 
					func.Compiler.compileAs<Action<IronJS.Function,IronJS.Object,object[]>>(func);
				fun.Invoke(func, func.Env.Globals, new object[] { stream } );

			}
		}
		
		public void addListener( string eventname, IronJS.Function callback) {
			Console.WriteLine( "NetServer - adding listener: " + eventname );
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
} // package
