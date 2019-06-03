using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Exceptions;
using PanoramicData.ConnectMagic.Service.Interfaces;
using PanoramicData.ConnectMagic.Service.Models;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	internal class SalesForceConnectedSystemManager : ConnectedSystemManagerBase, IConnectedSystemManager
	{
		public SalesForceConnectedSystemManager(
			ConnectedSystem connectedSystem,
			State state
			) : base(connectedSystem, state)
		{
			//salesForceClient = new Client(connectedSystem.Credentials.PublicText, connectedSystem.Credentials.PrivateText);
		}

		public Task RefreshDataSetsAsync(CancellationToken cancellationToken)
		{
			foreach (var dataSet in ConnectedSystem.Datasets)
			{
				var query = new SubstitutionString(new JObject(dataSet.QueryConfig)["Query"]?.ToString() ?? throw new ConfigurationException($"Missing Query in QueryConfig for dataSet '{dataSet.Name}'"));

				//var items = await autoTaskClient
				//	.ExecuteQueryAsync(query.ToString())
				//	.ConfigureAwait(false);
			}

			return Task.CompletedTask;
		}

		/// <inheritdoc />
		internal override void CreateOutwards(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> throw new System.NotImplementedException();

		/// <inheritdoc />
		internal override void DeleteOutwards(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> throw new System.NotImplementedException();

		/// <inheritdoc />
		internal override void UpdateOutwards(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> throw new System.NotImplementedException();
	}
}