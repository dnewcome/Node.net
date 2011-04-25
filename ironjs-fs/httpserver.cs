using System.Net.Sockets;
using System.Net;
using System;
using System.Collections;
using System.IO;

using IronJS;
using IronJS.Native;

using Net;
/**
* Http package implements the `http" module in the Node.js API 
*/
namespace Http 
{
	public class HttpServer : IronJS.Object
	{
		TcpListener tcpServer;
		ArrayList listeningCallbacks = new ArrayList();
		ArrayList connectionCallbacks = new ArrayList();
		ArrayList closeCallbacks = new ArrayList();

		public HttpServer( IronJS.Function callback, IronJS.Environment env ) : base( env, env.Maps.Base, env.Prototypes.Object, IronJS.Classes.Object ) {
			// have to set this stuff up. not sure why yet
			Env = env;
			Methods = Env.Methods.Object;
			
			Console.WriteLine( "creating HttpServer" );
			var objMethod = IronJS.Api.HostFunction.create<Action<object, string>>(env, listen);
            this.Methods.PutRefProperty(this, "listen", objMethod, IronJS.TypeTags.Function);
           
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
			NetStream stream = new NetStream( client.GetStream(), Env );
			Server.instance.queueWorkItem( new Callback { name = "raiseConnectionEvent", callback = raiseConnectionEvent, args = new object[]{ stream } } );
			
			// kick off async read
			// stream.read();
		}
		
		// TODO: pull this stuff out to an event class
		public void raiseListeningEvent( object[] args ) {
			Console.WriteLine( "http.server.raiseListeningEvent()" );
			for( var i=0; i < listeningCallbacks.Count; i++ ) {
				// ( ( IronJS.Function )listeningCallbacks[i] ).Call( this, new object[]{} );
				IronJS.Function func = ( IronJS.Function )listeningCallbacks[i];
				Action<IronJS.Function,IronJS.Object,object[]> fun = 
					func.Compiler.compileAs<Action<IronJS.Function,IronJS.Object,object[]>>(func);
				fun.Invoke(func, func.Env.Globals, new object[] {} );
			}
		}

        // TODO: testing only remove these 
        HttpServerRequest req;
        HttpServerResponse resp;

		public void raiseConnectionEvent( object[] args ) {
			NetStream stream = ( NetStream )args[0];
			Console.WriteLine( "http.server.raiseConnectionEvent()" );
			Console.WriteLine( "http.server.raiseConnectionEvent() - calling " + connectionCallbacks.Count + " callbacks" );
			
			// TODO; update to use locals here - won't work for multiple outstanding requests
            req = new HttpServerRequest( stream, Env );
			resp = new HttpServerResponse( stream, Env );
			req.ParseHeaders( raiseConnectionEventCont );
		}
		
		public void raiseConnectionEventCont() {
            Console.WriteLine("raiseConnectionEventCont()");
			foreach( object callback in connectionCallbacks ) {
				// ( ( IFunction )callback ).Call( this, new object[] { req, resp } );
				IronJS.Function func = ( IronJS.Function )callback;
				Action<IronJS.Function,IronJS.Object,IronJS.Object,IronJS.Object> fun = 
					func.Compiler.compileAs<Action<IronJS.Function,IronJS.Object,IronJS.Object,IronJS.Object>>(func);
				fun.Invoke(func, this, req, resp );
			}
		}

		public void addListener( string eventname, IronJS.Function callback) {
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

	class HttpServerRequest : IronJS.Object {
		private NetStream netStream;		
		private Parser m_parser;
		public string m_path;

		public HttpServerRequest( NetStream in_stream, IronJS.Environment env ) : base( env, env.Maps.Base, env.Prototypes.Object, IronJS.Classes.Object ) {
			netStream = in_stream;	
			m_parser = new Parser();
			
			Env = env;
			Methods = Env.Methods.Object;
			
			// this.SetOwnProperty( "addListener", new Action<string, IFunction>( this.addListener ) );
			var objMethod = IronJS.Api.HostFunction.create<Action<string, IronJS.Function>>(env, addListener);
            var objMethod2 = IronJS.Api.HostFunction.create<Func<string>>(env, getPath);
            this.Methods.PutRefProperty(this, "addListener", objMethod, IronJS.TypeTags.Function);
            this.Methods.PutRefProperty(this, "getPath", objMethod2, IronJS.TypeTags.Function);
		}

        public string getPath() {
            return m_path;
            
        }

		~HttpServerRequest() {
            Console.WriteLine( "Disposing parser" );
			m_parser.Dispose();
		}

		public void ParseHeaders( HttpDelegate in_callback ) {
            Console.WriteLine( "Parsing headers" );
			// todo: if header is bigger than 1k this fails
			byte[] buffer = new byte[1024];
			int bytesread = netStream.stream.Read( buffer, 0, 1024 );
			m_parser.Path += new HttpDataDelegate( PathDataCallback );
			m_parser.HeadersComplete += in_callback;
			m_parser.Parse( buffer );

            if( bytesread < buffer.Length ) {
                netStream.raiseEndEvent( new object[]{} );
            }
		}

		private void PathDataCallback( string in_path ) {
			m_path = in_path;
            Console.WriteLine("setting path: " + m_path);
		}

		public void Read() {
			netStream.read();
		}

		public void addListener( string eventname, IronJS.Function callback) {
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

	class HttpServerResponse : IronJS.Object {
		private NetStream netStream;
		public HttpServerResponse( NetStream in_stream, IronJS.Environment env ) : base( env, env.Maps.Base, env.Prototypes.Object, IronJS.Classes.Object ) {
			netStream = in_stream;	
			// Context = in_context;
			Env = env;
			Methods = Env.Methods.Object;
			
			// this.SetOwnProperty( "write", new Action<string>( write ) );
			var writeMethod = IronJS.Api.HostFunction.create<Action<string>>(env, write);
            this.Methods.PutRefProperty(this, "write", writeMethod, IronJS.TypeTags.Function);
			
			// this.SetOwnProperty( "end", new Action( end ) );
			var endMethod = IronJS.Api.HostFunction.create<Action>(env, end);
            this.Methods.PutRefProperty(this, "end", endMethod, IronJS.TypeTags.Function);
			
		} 
		public void end() {
			Console.WriteLine( "HttpServerResponse.end()" );
			netStream.end();
		}
		public void write( string chunk ) {
			Console.WriteLine( "HttpServerResponse.write(): " + chunk );
			netStream.write( chunk );
		}
	} // class

} // package
