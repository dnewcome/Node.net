/**
* Server is a .net executable written in js, compiled to .net using jsc
*/
import System;
import System.Net;
import System.IO;

var http = {};

class HttpServer {

var httpListener : HttpListener;
var prefix;

function HttpServer( prefix ) {
	var httpListener : HttpListener = new HttpListener();
	this.httpListener = httpListener;
	this.prefix = prefix;
	httpListener.Prefixes.Add( prefix );
	// return this;
}

function ListenerCallback( result : IAsyncResult ) : void {
	// print( 'ListenerCallback():' );
}

// doesn't work because member vars can't hold functions for some reason.
// var ListenerCallback : Function = function( result: IAsyncResult ) { print( 'ListenerCallback():' ); };

function listen() {
	print( 'HttpServer.listen():' );
	this.httpListener.Start();
	print( 'listener started successfully' );
	print( 'httpListener: ' + this.httpListener );
	var result : IAsyncResult = this.httpListener.BeginGetContext( new AsyncCallback( this.ListenerCallback ), httpListener );
	print( 'BeginGetContext called successfully' );
}
} // class

/*
Non working solution for Async callback
var _ListenerCallback = function( result : IAsyncResult ) {
	print( 'ListenerCallback():' );
}
var ListenerCallback : AsyncCallback = AsyncCallback( _ListenerCallback );
*/

http.createServer = function( request_listener, prefix ) {
	return new HttpServer( prefix );
}

// takes a .js file on the commandline, eval it 
var args : String[] = Environment.GetCommandLineArgs();
var sr : StreamReader = new StreamReader( args[1] );
var script = sr.ReadToEnd();
eval( script, 'unsafe' );
sr.Close();