/**
* Node.net is a .NET implementation of Node.js and is free 
*	software provided under the MIT license.
*
*	See LICENSE file for full text of the license.
*	Copyright 2010 Dan Newcome.
*/

using System.Threading;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System;
using IronJS;
using IronJS.Native;

public class Server 
{
	// temp hack global
	public static Server instance;
	
	// dispatch queue and signalling primitive for main event loop
	private ManualResetEvent manualResetEvent = new ManualResetEvent( false );
	private Queue workItems = new Queue();

	// JS execution context used for all JS code
	private static IronJS.Hosting.CSharp.Context ctx = new IronJS.Hosting.CSharp.Context();

	private static net netObj = new net( ctx.Environment );
	// TODO: re-add after httpserver.cs is converted to IJS 0.2
	// private static http httpObj = new http( ctx.Environment );

	// path of current js file
	private static string path = "";

	public static void Main() {
		// eval js file given as first commandline arg and 
		// run the event loop - runEventLoop() always blocks, unlike node.js
		// which returns if no more callbacks are registered
		instance = new Server();
		instance.evalCommandlineArgument();
		instance.runEventLoop();
	}

	private static List<string> requireStack = new List<string>();

	// implements require() for importing js files/namespaces
	public static IronJS.CommonObject Require( string file ) {

		string pathpart = Path.GetDirectoryName( file );
		path = Directory.GetCurrentDirectory() + pathpart;
		string filename = Path.GetFileName( file );
		Console.WriteLine( "require() path of js file: " + path );

		// for compiled-in functionality, return existing references.
		// TODO: clean up the global namespace, `http' et al. are directly visible
		if( file == "http" ) {
			// TODO: re-add after httpserver.cs is converted to IJS 0.2
			// return httpObj;
		}
		if( file == "net" ) {
			return netObj;
		}
		/*
		if( file == "sys" ) {
			return sys;
		}
		*/

		Console.WriteLine( "requiring: " + file );
		FileStream fs;
		string filepath;
		try {
			filepath = path + "\\" + filename + ".js";
			Console.WriteLine( "full file name: " + filepath );
			fs = new FileStream( filepath, FileMode.Open, FileAccess.Read );
		}
		catch {
			filepath = path + "\\node_modules\\" + filename + "\\index.js";
			Console.WriteLine( "full file name: " + filepath );
			fs = new FileStream( path + "\\node_modules\\" + file + "\\index.js", FileMode.Open, FileAccess.Read );
		}

		string code = new StreamReader( fs ).ReadToEnd();
		// extra semicolon provided after file contents.. just in case
		code = "var exports = {}; " + code + "; exports;";
		return ctx.Execute<IronJS.CommonObject>( code );
	}

	// threadsafe enqueue function
	public void queueWorkItem( object item ) {
			Console.WriteLine( "queueWorkItem(): queuing item: " + ((Callback)item).name );
			Console.WriteLine( "queueWorkItem(): acquiring lock: " + workItems );
		Monitor.Enter( workItems );
		try {
			workItems.Enqueue( item );
			Console.WriteLine( "queueWorkItem(): item queued" );
			manualResetEvent.Set();
			Console.WriteLine( "queueWorkItem(): queue set to runnable" );
		}
		finally {
			Monitor.Exit( workItems );
			Console.WriteLine( "queueWorkItem: released lock" );
		}
	}

	private string readFile( string filename ) {
		string fileContents;
		StreamReader streamReader = new StreamReader( filename );
		try {
			fileContents = streamReader.ReadToEnd();
			return fileContents;
		}
		finally {
			streamReader.Close();
		}
	}

	public void evalCommandlineArgument() {
		string[] args = System.Environment.GetCommandLineArgs();
		if( args.Length != 2 ) {
			Console.WriteLine( "usage: node <file.js>" );
			System.Environment.Exit(1);
		}
		// string script = readFile( args[1] );
		ReadJsFile( args[1] );
	}

	public void runEventLoop() {
		while( true ) {
			Console.WriteLine( "event loop running" );
			Callback callback = null;
			
			// TODO: can we improve locking method?
			Monitor.Enter( workItems );
			try {
				Console.WriteLine( "event loop: acquired lock" );
				callback = ( Callback )workItems.Dequeue();
				if( callback == null ) {
					manualResetEvent.Reset();
				}
			}
			catch( Exception e ) {
				Console.WriteLine( e );
				manualResetEvent.Reset();
			}
			finally {
				Monitor.Exit( workItems );
				Console.WriteLine( "event loop: released lock" );
			}
			
			if( callback != null ) {
				Console.WriteLine( "event loop: dispatching callback: " + callback.name );
				Console.WriteLine( "event loop: dispatching callback: " + callback.callback );
				Console.WriteLine( "event loop: dispatching args: " + callback.args );
				// TODO: not sure what this callback delegate looks like yet
				callback.callback.Invoke( callback.args );
			}
			Console.WriteLine( "event loop waiting" );
			manualResetEvent.WaitOne();
		} // while
	}
	
	public void ReadJsFile( string in_filename ) {

		string pathpart = Path.GetDirectoryName( in_filename );
		path = Directory.GetCurrentDirectory() + pathpart;
		Console.WriteLine( "path of js file: " + path );

		var emit =
		Utils.createHostFunction<Action<IronJS.BoxedValue>>( 
			ctx.Environment, ( obj ) => { 
				Console.WriteLine(IronJS.TypeConverter.ToString(obj) ); 
			} 
		);
		ctx.SetGlobal<IronJS.FunctionObject>( "puts", emit );
		
		var require =
		Utils.createHostFunction<Func<string,IronJS.CommonObject>>( 
			ctx.Environment, ( obj ) => { 
				return Require( obj );
			} 
		);
		ctx.SetGlobal<IronJS.FunctionObject>("require", require );

		// Forms the `net" namespace
		ctx.SetGlobal<IronJS.CommonObject>( "net", netObj );
		
		// Forms the `http" namespace
		// TODO: re-add after httpserver.cs is converted to IJS 0.2
		// ctx.SetGlobal<IronJS.CommonObject>( "http", httpObj );
	
		ctx.ExecuteFile( in_filename );
	}
} // class

// provides the 'net' namespace
class net : IronJS.CommonObject
{
	public net( IronJS.Environment env ) : base( env, env.Maps.Base, env.Prototypes.Object ) {
		
		var objMethod = Utils.createHostFunction<Func<IronJS.FunctionObject, IronJS.CommonObject>>(Env, CreateServer);
		Console.WriteLine( objMethod );
		this.Put( "createServer", objMethod );
	}
	
	public IronJS.CommonObject CreateServer( IronJS.FunctionObject callback ) {
		Console.WriteLine( "net.createServer() called." );
		Net.NetServer server = new Net.NetServer( callback, Env );
		Console.WriteLine( server );
		return( server );
	}
}
	
// provides the 'http' namespace
/** TODO: re-add after httpserver.cs is converted to IJS 0.2
class http : IronJS.CommonObject
{
	public http( IronJS.Environment env ) : base( env, env.Maps.Base, env.Prototypes.Object ) {
		// have to set context to satisfy IronJS Obj
		// Env = env;
		// Methods = Env.Methods.Object;
		
		// SetOwnProperty( "createServer", new Fn_CreateHttpServer( context ) );
		var objMethod = Utils.createHostFunction<Func<IronJS.FunctionObject, IronJS.CommonObject>>(Env, CreateServer);
		Console.WriteLine( objMethod );
        // this.Methods.PutRefProperty(this, "createServer", objMethod, IronJS.TypeTags.Function);
		this.Put( "createServer", objMethod );
	}
	public IronJS.CommonObject CreateServer( IronJS.FunctionObject callback ) {
		Console.WriteLine( "http.createServer() called." );
		Http.HttpServer server = new Http.HttpServer( callback, Env );
		Console.WriteLine( server );
		return( server );
	}
}
*/

// note that this encapsulates callbacks on the .net side. The dispatch queue
// is a Queue of these.
// We use IFunction for the js callbacks
public class Callback 
{
	public string name = "";
	public VarDelegate callback;
	public object[] args;
}

public delegate void VarDelegate( params object[] args );
