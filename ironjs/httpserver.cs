using System.Net.Sockets;
using System.Net;
using System;
using System.Collections;
using System.IO;

using IronJS.Runtime;
using IronJS.Runtime.Js;
using IronJS.Runtime.Utils;

using Net;
/**
* Http package implements the `http" module in the Node.js API 
*/
namespace Http 
{
	public class HttpServer : Obj
	{
		TcpListener tcpServer;
		ArrayList listeningCallbacks = new ArrayList();
		ArrayList connectionCallbacks = new ArrayList();
		ArrayList closeCallbacks = new ArrayList();

		public HttpServer( UserFunction callback, Context context ) {
			// have to set this stuff up. not sure why yet
			Context = context;
			Prototype = context.ObjectConstructor.Object_prototype;
			Class = ObjClass.Array;

			Console.WriteLine( "creating HttpServer" );
			this.SetOwnProperty( "listen", new Action<object, string>( listen ) );
			this.addListener( "connection", callback );
		}
		
		// in Node.js, listen is async, here it blocks on call to Start()
		public void listen( object in_port, string host ) {
			int port = Convert.ToInt32( in_port );
			Console.WriteLine( "http.Server.listen()" );
			IPAddress ipAddress = Dns.Resolve( host ).AddressList[0];
			this.tcpServer = new TcpListener( ipAddress, port );
			Console.WriteLine( "http.Server.listen(): starting http server" );
			this.tcpServer.Start();
			Server.instance.queueWorkItem( new Callback { name = "raiseListeningEvent", callback = raiseListeningEvent, args = new object[]{} } );
			this.tcpServer.BeginAcceptTcpClient( listenerCallback, null );
		}
		
		public void listenerCallback( IAsyncResult result ) {
			Console.WriteLine( "http.server.listenerCallback()" );
			// TODO: not sure where we should put the call to start listening
			// for the next connection, here for now
			this.tcpServer.BeginAcceptTcpClient( listenerCallback, null );
			
			TcpClient client = this.tcpServer.EndAcceptTcpClient( result );
			NetStream stream = new NetStream( client.GetStream(), Context );
			Server.instance.queueWorkItem( new Callback { name = "raiseConnectionEvent", callback = raiseConnectionEvent, args = new object[]{ stream } } );
			
			// kick off async read
			stream.read();
		}
		
		// TODO: pull this stuff out to an event class
		public void raiseListeningEvent( object[] args ) {
			Console.WriteLine( "http.server.raiseListeningEvent()" );
			for( var i=0; i < listeningCallbacks.Count; i++ ) {
				( ( IFunction )listeningCallbacks[i] ).Call( this, new object[]{} );
			}
		}
		
		public void raiseConnectionEvent( object[] args ) {
			NetStream stream = ( NetStream )args[0];
			Console.WriteLine( "http.server.raiseConnectionEvent()" );
			Console.WriteLine( "http.server.raiseConnectionEvent() - calling " + connectionCallbacks.Count + " callbacks" );
			
			HttpServerRequest req = new HttpServerRequest( stream, Context );
			HttpServerResponse resp = new HttpServerResponse( stream, Context );

			foreach( object callback in connectionCallbacks ) {
				( ( IFunction )callback ).Call( this, new object[] { req, resp } );
			}
		}
		
		public void addListener( string eventname, IFunction callback) {
			Console.WriteLine( "HttpServer - adding listener: " + eventname );
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

	class HttpServerRequest : Obj {
		private NetStream netStream;		
		private Parser m_parser;
		public HttpServerRequest( NetStream in_stream, Context in_context ) {
			netStream = in_stream;	
			m_parser = new Parser();
			Context = in_context;
			Prototype = in_context.ObjectConstructor.Object_prototype;
			Class = ObjClass.Array;
			this.SetOwnProperty( "addListener", new Action<string, IFunction>( this.addListener ) );
		}

		public void Read() {
			netStream.read();
		}

		public void addListener( string eventname, IFunction callback) {
			Console.WriteLine( "HttpServerRequest - adding listener: " + eventname );
			if( eventname == "data" ) {
				netStream.addListener( "data", callback );
			}
			else if( eventname == "end" ) {
				netStream.addListener( "end", callback );
			}
			else {
				throw new Exception( "addListener called for unsupported event" );
			}
		}
	} // class

	class HttpServerResponse : Obj {
		private NetStream netStream;
		public HttpServerResponse( NetStream in_stream, Context in_context ) {
			netStream = in_stream;	
			Context = in_context;
			this.SetOwnProperty( "write", new Action<string>( write ) );
			this.SetOwnProperty( "end", new Action( end ) );
		} 
		public void end() {
			netStream.end();
		}
		public void write( string chunk ) {
			netStream.write( chunk );
		}
	} // class

} // package
