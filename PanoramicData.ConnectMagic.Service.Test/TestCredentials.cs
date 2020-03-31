using System;

namespace PanoramicData.ConnectMagic.Service.Test
{
	public class TestCredentials
	{
		public string AutoTaskPublicText { get; set; } = null!;
		public string AutoTaskPrivateText { get; set; } = null!;
		public string AutoTaskIntegrationCode { get; set; } = null!;
		public string ServiceNowAccount { get; set; } = null!;
		public string ServiceNowPublicText { get; set; } = null!;
		public string ServiceNowPrivateText { get; set; } = null!;

		public void Validate()
		{
			if (string.IsNullOrWhiteSpace(AutoTaskPublicText))
			{
				throw new Exception($"{nameof(AutoTaskPublicText)} must be set");
			}
			if (string.IsNullOrWhiteSpace(AutoTaskPrivateText))
			{
				throw new Exception($"{nameof(AutoTaskPrivateText)} must be set");
			}
			if (string.IsNullOrWhiteSpace(AutoTaskIntegrationCode))
			{
				throw new Exception($"{nameof(AutoTaskIntegrationCode)} must be set");
			}
			if (string.IsNullOrWhiteSpace(ServiceNowAccount))
			{
				throw new Exception($"{nameof(ServiceNowAccount)} must be set");
			}
			if (string.IsNullOrWhiteSpace(ServiceNowPublicText))
			{
				throw new Exception($"{nameof(ServiceNowPublicText)} must be set");
			}
			if (string.IsNullOrWhiteSpace(ServiceNowPrivateText))
			{
				throw new Exception($"{nameof(ServiceNowPrivateText)} must be set");
			}
		}
	}
}
