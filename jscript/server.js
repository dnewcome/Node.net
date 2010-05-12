/**
* Node.net is a .NET implementation of Node.js and is free 
*	software provided under the MIT license.
*
*	See LICENSE file for full text of the license.
*	Copyright 2010 Dan Newcome.
*/

import System.Threading;
import System.IO;

// TODO: would like to import Node.net API namespaces using require()
import Http;
import Net;

// dispatch queue and signalling primitive for main event loop
var manualResetEvent : ManualResetEvent = new ManualResetEvent( false );
var workItems = [];

// eval js file given as first commandline arg and 
// run the event loop - runEventLoop() always blocks, unlike node.js
// which returns if no more callbacks are registered
evalCommandlineArgument() 
runEventLoop();


// implements require() for importing js files/namespaces
function require( file ) {
	// for compiled-in functionality, return existing references.
	// TODO: clean up the global namespace, `http' et al. are directly visible
	if( file == 'http' ) {
		return http;
	}
	if( file == 'net' ) {
		return net;
	}
	if( file == 'sys' ) {
		return sys;
	}
	print( 'requiring: ' + file );
	var fileContents = readFile( file + '.js' );
	// extra semicolon provided after file contents.. just in case
	var code = 'var exports = {}; ' + fileContents + '; exports;';
	print( 'evaluating require: ' + code );
	return eval( code, 'unsafe' );
}

// quick hack to add sys.puts();
// TODO: `sys' should be implemented in its own class
var sys = { puts: function( string ){ print( string ); } };

// threadsafe enqueue function
function queueWorkItem( item ) {
		print( 'queueWorkItem(): queuing item: ' + item );
		print( 'queueWorkItem(): acquiring lock: ' + workItems );
	Monitor.Enter( workItems );
	try {
		workItems.push( item );
		print( 'queueWorkItem(): item queued' );
		manualResetEvent.Set();
	}
	finally {
		Monitor.Exit( workItems );
		print( 'queueWorkItem: released lock' );
	}
}

function readFile( filename ) {
	var fileContents;
	var streamReader : StreamReader = new StreamReader( filename );
	try {
		fileContents = streamReader.ReadToEnd();
		return fileContents;
	}
	finally {
		streamReader.Close();
	}
}

function evalCommandlineArgument() {
	var args : String[] = Environment.GetCommandLineArgs();
	if( args.length != 2 ) {
		print( 'usage: node <file.js>' );
		Environment.Exit(1);
	}
	var script = readFile( args[1] );
	eval( script, 'unsafe' );
}

function runEventLoop() {
	while( true ) {
		print( 'event loop running' );
		var callback = null;
		
		// TODO: can we improve locking method?
		Monitor.Enter( workItems );
		try {
			print( 'event loop: acquired lock' );
			callback = workItems.shift();
			if( callback == null ) {
				manualResetEvent.Reset();
			}
		}
		finally {
			Monitor.Exit( workItems );
			print( 'event loop: released lock' );
		}
		
		if( callback != null ) {
			print( 'event loop: dispatching callback' );
			// TODO: figure out what 'this' should be passed, currently passing server
			// which is almost certainly the wrong thing.
			callback.callback.apply( this, callback.args );
		}
		print( 'event loop waiting' );
		manualResetEvent.WaitOne();
	} // while
}