function cb( request, response ) {
	print( 'request callback fired' );
	var responseString : String = "<HTML><BODY> Hello world!</BODY></HTML>";
		var buffer : byte[] = System.Text.Encoding.UTF8.GetBytes( responseString );
		response.ContentLength64 = buffer.Length;
		var output : Stream = response.OutputStream;
		output.Write( buffer, 0, buffer.Length );
		
		output.Close();
}

var server = http.createServer( cb, 'http://localhost:9980/' ).listen();
