http.createServer( function( request, response ) {
	response.write( '<html><body> Hello world!</body></html>' );
	response.end();
}).listen( 9981, 'localhost' );

net.createServer( function( stream ) {
	stream.addListener( 'data', function( data ) {
		print( data );
	});
}).listen( 9982, 'localhost' );