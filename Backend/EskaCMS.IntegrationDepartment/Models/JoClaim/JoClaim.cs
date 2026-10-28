using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Integrations.Models.JoClaim
{
	public class JoClaim
	{
		public string BaseURL { get; set; }
		public string CallBackUrl { get; set; }
		public string Client_Id { get; set; }
		public string Client_Secret { get; set; }
		public string Grant_Type { get; set; }
		public string hostName { get; set; }
	}
}
