using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Node.Web
{
	public partial class _default : System.Web.UI.Page
	{
		protected void Page_Load( object sender, EventArgs e ) {
			System.Diagnostics.Debug.WriteLine( "starting" );
			Node.Net.Server instance = new Node.Net.Server();
			Node.Net.Server.instance = instance;
			string dir = System.IO.Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Replace( "file:///", "" ) );
			instance.evalCommandlineArgument( new string[] { "node.exe", dir + "\\testnet.js" } );
			instance.runEventLoop();		
		}
		protected void Page_LoadComplete( object sender, EventArgs e ) {
		}
	}
}