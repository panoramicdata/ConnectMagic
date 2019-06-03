using Certify.Api;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Exceptions;
using PanoramicData.ConnectMagic.Service.Interfaces;
using PanoramicData.ConnectMagic.Service.Models;
using System.Threading;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	internal class CertifyConnectedSystemManager : ConnectedSystemManagerBase, IConnectedSystemManager
	{
		private CertifyClient _certifyClient;

		public CertifyConnectedSystemManager(
			ConnectedSystem connectedSystem,
			State state
			) : base(connectedSystem, state)
		{
			_certifyClient = new CertifyClient(connectedSystem.Credentials.PublicText, connectedSystem.Credentials.PrivateText);
		}

		public System.Threading.Tasks.Task RefreshDataSetsAsync(CancellationToken cancellationToken)
		{
			foreach (var dataSet in ConnectedSystem.Datasets)
			{
				var query = new SubstitutionString(new JObject(dataSet.QueryConfig)["Query"]?.ToString() ?? throw new ConfigurationException($"Missing Query in QueryConfig for dataSet '{dataSet.Name}'"));

				//var items = await autoTaskClient
				//	.ExecuteQueryAsync(query.ToString())
				//	.ConfigureAwait(false);
			}

			return System.Threading.Tasks.Task.CompletedTask;
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