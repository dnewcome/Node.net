puts( "working" );

http.createServer( function( request, response ) {
	puts("javsacript:running connection callback");
	puts("javsacript:request: " + typeof request );
	puts("javsacript:request.addListener: " + typeof request.addListener );
	puts("javsacript:response: " + typeof response );
	request.addListener( 'end', function() {
		response.write( '<html><body>' );
		response.write( '<p>All finished!<p>' );
		response.write( '<p>path: ' + request.getPath() + '<p>' );
		response.write( '</body></html>' );
		response.end();
	});
	
}).listen( 9981, 'localhost' );


