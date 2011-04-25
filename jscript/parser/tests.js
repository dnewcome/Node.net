import System.IO;



var parser = new HttpParser();

var http_get_request = "\
GET /fiddler2/updatecheck.asp?isBeta=False HTTP/1.1\r\n\
User-Agent: Fiddler/2.2.7.5 (.NET 2.0.50727.3603; Microsoft Windows NT 6.0.6001 Service Pack 1)\r\n\
Pragma: no-cache\r\n\
Host: www.fiddler2.com\r\n\
Connection: Close\r\n\
\r\n";

var http_post_request = "\
POST /fiddler2/updatecheck.asp?isBeta=False HTTP/1.1\r\n\
User-Agent: Fiddler/2.2.7.5 (.NET 2.0.50727.3603; Microsoft Windows NT 6.0.6001 Service Pack 1)\r\n\
Pragma: no-cache\r\n\
Host: www.fiddler2.com\r\n\
Connection: Close\r\n\
\r\n\
Some content";

var ms1 : MemoryStream = new MemoryStream( System.Text.Encoding.ASCII.GetBytes( http_get_request ) );
var ms2 : MemoryStream = new MemoryStream( System.Text.Encoding.ASCII.GetBytes( http_post_request ) );

parser.Parse( new StreamReader( ms1 ) );
parser.Parse( new StreamReader( ms2 ) );
