using AutoTask.Api;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Exceptions;
using PanoramicData.ConnectMagic.Service.Interfaces;
using PanoramicData.ConnectMagic.Service.Models;
using System.Linq;
using System.Threading;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	internal class AutoTaskConnectedSystemManager : ConnectedSystemManagerBase, IConnectedSystemManager
	{
		private readonly Client autoTaskClient;

		public AutoTaskConnectedSystemManager(
			ConnectedSystem connectedSystem,
			State state
			) : base(connectedSystem, state)
		{
			autoTaskClient = new Client(connectedSystem.Credentials.PublicText, connectedSystem.Credentials.PrivateText);
		}

		public async System.Threading.Tasks.Task RefreshDataSetsAsync(CancellationToken cancellationToken)
		{
			foreach (var dataSet in ConnectedSystem.Datasets)
			{
				var inputText = dataSet.QueryConfig.Query ?? throw new ConfigurationException($"Missing Query in QueryConfig for dataSet '{dataSet.Name}'");
				var query = new SubstitutionString(inputText);

				var connectedSystemItems = (await autoTaskClient
					.ExecuteQueryAsync(query.ToString())
					.ConfigureAwait(false))
					.Select(entity => new JObject(entity))
					.ToList();

				ProcessConnectedSystemItems(dataSet, connectedSystemItems);
			}
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