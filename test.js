http.createServer( function( request, response ) {
	request.addListener( 'data', function( data ) {
		sys.puts( data );
	});
	request.addListener( 'end', function() {
		response.write( '<html><body><p>All finished!<p></body></html>' );
		response.end();
	});
	
}).listen( 9981, 'localhost' );

net.createServer( function( stream ) {
	stream.addListener( 'data', function( data ) {
		print( data );
	});
}).listen( 9982, 'localhost' );