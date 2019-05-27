using System;
using System.Threading;
using System.Threading.Tasks;
using PanoramicData.ConnectMagic.Service.Models;
using PanoramicData.ConnectMagic.Service.Interfaces;
using PanoramicData.ConnectMagic.Service.Exceptions;
using Newtonsoft.Json.Linq;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	internal class SalesForceConnectedSystemManager : IConnectedSystemManager
	{
		private readonly ConnectedSystem connectedSystem;

		public SalesForceConnectedSystemManager(ConnectedSystem connectedSystem)
		{
			this.connectedSystem = connectedSystem;
			//salesForceClient = new Client(connectedSystem.Credentials.PublicText, connectedSystem.Credentials.PrivateText);
		}

		public async System.Threading.Tasks.Task RefreshDataSetsAsync(CancellationToken cancellationToken)
		{
			foreach(var dataSet in connectedSystem.Datasets)
			{
				var query = new SubstitutionString(new JObject(dataSet.QueryConfig)["Query"]?.ToString() ?? throw new ConfigurationException($"Missing Query in QueryConfig for dataSet '{dataSet.Name}'"));

				//var items = await autoTaskClient
				//	.ExecuteQueryAsync(query.ToString())
				//	.ConfigureAwait(false);
			}
		}
	}
}