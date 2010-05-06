/**
* Server is a .net executable written in js, compiled to .net using jsc
*/
import System;
import System.Threading;
import System.Net;
import System.IO;

import Http;

// takes a .js file on the commandline, eval it 
var args : String[] = Environment.GetCommandLineArgs();
var sr : StreamReader = new StreamReader( args[1] );
var script = sr.ReadToEnd();
eval( script, 'unsafe' );
sr.Close();