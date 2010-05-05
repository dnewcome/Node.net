http.createServer( function( request, response ) {
	response.write( '<html><body> Hello world!</body></html>' );
	response.end();
}).listen( 9981, 'localhost' );

net.createServer( function( stream ) {
	
}).listen( 9982, 'localhost' );