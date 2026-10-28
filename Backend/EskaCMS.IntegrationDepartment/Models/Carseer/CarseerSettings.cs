using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Integrations.Models.Carseer
{
	public class CarseerSettings
	{
		public string ApiKey { get; set; }
		public string BaseURL { get; set; }
		public string ApprovalApiUrl { get; set; }
		public string UploadAttachmentUrl { get; set; }
	}
}
