using System;
using System.IO;
using System.Collections;

using IronJS;
using IronJS.Native;

namespace Node.Net
{
	/**
	* NetStream implements the Node.js net.Socket (formerly net.Stream)
	*	Used by net.Server and http.Server
	*/
	public class NetStream : IronJS.CommonObject
	{
		private Stream stream;

		// TODO: callbacks will be JS functions, should probably use 
		// List<IFunction>
		ArrayList dataCallbacks = new ArrayList();
		ArrayList endCallbacks = new ArrayList();

		int bufferSize = Config.BufferSize;

		public NetStream( Stream stream, IronJS.Environment env )
			: base( env, env.Maps.Base, env.Prototypes.Object ) {
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
				Server.instance.queueWorkItem( new Callback() { name = "stream:raiseEndEvent", callback = raiseEndEvent, args = new object[] { } } );
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
			string chunk = ( string )args[ 0 ];
			log.Trace( "net.stream.raiseDataEvent():" + chunk );
			for( var i = 0; i < dataCallbacks.Count; i++ ) {
				IronJS.FunctionObject func = ( IronJS.FunctionObject )dataCallbacks[ i ];
				func.Call<string>( this, chunk );
			}
		}

		public void raiseEndEvent( object[] args ) {
			log.Trace( "net.stream.raiseEndEvent()" );
			for( var i = 0; i < endCallbacks.Count; i++ ) {
				IronJS.FunctionObject func = ( IronJS.FunctionObject )endCallbacks[ i ];
				func.Call( this );
			}
		}

	} // class
} // namespace
