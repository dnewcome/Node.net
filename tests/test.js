var	sys = require( 'sys' ),
	http = require( 'http' ),
	net = require( 'net' );
	
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


setTimeout( 
	function() {
		// TODO: sys.puts not working here
		print( 'timeout working' );
		// sys.puts( 'timout working' );
	},
	5000 
);
setTimeout( 
	function() {
		// TODO: sys.puts not working here
		print( 'timeout2 working' );
		// sys.puts( 'timout working' );
	},
	10000 
);