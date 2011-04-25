import System.IO;

String.prototype.trim = function() {
	return this
		.replace(/^\s+/, '')
		.replace(/\s+$/, '');
};

/*
* Parse HTTP request line and header block from stream
*
* http://www.w3.org/Protocols/rfc2616/rfc2616-sec4.html#sec4.2
* 
*/
class HttpParser 
{
	// note that we don't handle any ugly situations like extra 
	// crlf sequences
	function Parse( in_streamReader : StreamReader) {
		var requestline;
		var headers;
		
		var line = in_streamReader.ReadLine();
		requestline = ParseRequestLine( line );

		var headerblock = "";
		while( true ) {
			var line = in_streamReader.ReadLine();
			if( line == "" ) { break; }
			// hackish - we add line breaks back in
			headerblock += line + "\r\n";
		}
		headers = parseHeaders( headerblock );
		
		return { requestline: requestline, headers: headers };
	}
	
	function ParseRequestLine( in_request : System.String ) {
		print( "parsing request line" );
		var tokens = in_request.Split( " " );
		var requestLine = { 
			verb: tokens[0], 
			path: tokens[1],
			version: tokens[2]
		};
		print( "verb: [" + requestLine.verb + "] path: [" + requestLine.path + "] version: " + requestLine.version + "]");
		return requestLine;
	}
	
	function parseHeaders( headerblock ) {
		print( "parsing headers" );
		var headers = {};
		var lines = headerblock.split( /\r\n(?![\s])/ );
		
		for( var i=0; i < lines.length; i++ ) {
			var line = lines[i].replace( /\r\n/g, "" );
			var key = line.substring( 0, line.indexOf(":") );
			var value = line.substring( line.indexOf( ":" ) + 1 );
			print( "[" + key.trim() + "], [" + value.trim() + "]" );
			headers[ key ] = value;
		}
		return headers;
	}
} // class