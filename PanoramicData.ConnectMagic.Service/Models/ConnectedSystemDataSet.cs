﻿using PanoramicData.ConnectMagic.Service.Exceptions;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace PanoramicData.ConnectMagic.Service.Models
{
	/// <summary>
	/// A connected system data class
	/// </summary>
	[DataContract]
	public class ConnectedSystemDataSet : DataSet
	{
		/// <summary>
		/// The name of the State DataSet to sync with
		/// </summary>
		public string StateDataSetName { get; set; } = string.Empty;

		/// <summary>
		/// The expression by which the connected system is queried
		/// The language for this will vary per system
		/// </summary>
		[DataMember(Name = "QueryConfig")]
		public QueryConfig QueryConfig { get; set; } = new QueryConfig();

		/// <summary>
		/// Whether the DataSet is enabled
		/// </summary>
		[DataMember(Name = "IsEnabled")]
		public bool IsEnabled { get; set; } = true;

		/// <summary>
		/// Whether to output the first data fetch to a workbook
		/// </summary>
		[DataMember(Name = "OutputToWorkbook")]
		public bool OutputToWorkbook { get; set; }

		/// <summary>
		/// Creation direction
		/// - None
		///   - items are not added to either the ConnectedSystem or State
		///   - items are not removed from either the ConnectedSystem or State
		/// - In
		///   - items are added to State if they are in the ConnectedSystem
		///   - items are removed from State if they are not in the ConnectedSystem
		/// - Out
		///   - items are added the ConnectedSystem if they are in State
		///   - items are removed from the ConnectedSystem if they are not in State
		/// - Join
		///	- Not supported
		/// </summary>
		[DataMember(Name = "CreateDeleteDirection")]
		public CreateDeleteDirection CreateDeleteDirection { get; set; }

		/// <summary>
		/// The mappings
		/// </summary>
		[DataMember(Name = "Constants")]
		public List<Constant> Constants { get; set; } = new List<Constant>();

		/// <summary>
		/// The mappings
		/// </summary>
		[DataMember(Name = "Mappings")]
		public List<Mapping> Mappings { get; set; } = new List<Mapping>();

		/// <summary>
		/// Permissions
		/// </summary>
		public Permissions Permissions { get; set; } = new Permissions();

		/// <summary>
		/// Whether to delete all but the first item when duplicates are found
		/// </summary>
		public DuplicateHandling DuplicateHandling { get; set; }

		internal void Validate()
		{
			if (string.IsNullOrWhiteSpace(Name))
			{
				throw new ConfigurationException($"{nameof(ConnectedSystemDataSet)}'s {nameof(Name)} must not be null or empty.");
			}

			if (string.IsNullOrWhiteSpace(StateDataSetName))
			{
				throw new ConfigurationException($"{nameof(ConnectedSystemDataSet)} {Name}'s {nameof(StateDataSetName)} must not be null or empty.");
			}

			foreach (var constant in Constants)
			{
				constant.Validate(Name);
			}

			foreach (var mapping in Mappings)
			{
				mapping.Validate(Name);
			}

			if (!Mappings.Any(m => m.Direction != MappingType.None))
			{
				throw new ConfigurationException($"{nameof(ConnectedSystemDataSet)} {Name}'s non-{nameof(MappingType.None)} {nameof(Mapping.Direction)} {nameof(Mappings)} must not be empty.");
			}

			if (!Mappings.Any(m => m.Direction == MappingType.Join))
			{
				throw new ConfigurationException($"{nameof(ConnectedSystemDataSet)} {Name} does not any Join mappings.");
			}

			switch (CreateDeleteDirection)
			{
				case CreateDeleteDirection.None:
					break;
				case CreateDeleteDirection.In:
					if (!Mappings.Any(m => m.Direction == MappingType.In))
					{
						throw new ConfigurationException($"{nameof(ConnectedSystemDataSet)} '{Name}' has {nameof(CreateDeleteDirection)} {CreateDeleteDirection} but no inbound mappings.");
					}
					break;
				case CreateDeleteDirection.Out:
					if (!Mappings.Any(m => m.Direction == MappingType.Out))
					{
						throw new ConfigurationException($"{nameof(ConnectedSystemDataSet)} '{Name}' has {nameof(CreateDeleteDirection)} {CreateDeleteDirection} but no outbound mappings.");
					}
					break;
				case CreateDeleteDirection.CreateBoth:
					if (!Mappings.Any(m => m.Direction == MappingType.In))
					{
						throw new ConfigurationException($"{nameof(ConnectedSystemDataSet)} '{Name}' has {nameof(CreateDeleteDirection)} {CreateDeleteDirection} but no inbound mappings.");
					}
					if (!Mappings.Any(m => m.Direction == MappingType.Out))
					{
						throw new ConfigurationException($"{nameof(ConnectedSystemDataSet)} '{Name}' has {nameof(CreateDeleteDirection)} {CreateDeleteDirection} but no outbound mappings.");
					}
					break;
				default:
					throw new ConfigurationException($"{nameof(ConnectedSystemDataSet)} '{Name}' has {nameof(CreateDeleteDirection)} {CreateDeleteDirection} which is not supported.");
			}
		}

		/// <summary>
		/// Replaces tokens in queryconfig and mappings with the values defined in Constants
		/// </summary>
		internal void SubstituteConstants()
		{
			foreach (var constant in Constants)
			{
				var token = $"##{constant.Token}##";
				var value = constant.Value;

				QueryConfig.Query = QueryConfig.Query?.Replace(token, value);
				QueryConfig.Options = QueryConfig.Options?.Replace(token, value);

				foreach (var mapping in Mappings)
				{
					mapping.StateExpression = mapping.StateExpression.Replace(token, value);
					mapping.SystemExpression = mapping.SystemExpression.Replace(token, value);
					mapping.ConditionExpression = mapping.ConditionExpression?.Replace(token, value);
					mapping.FunctionExpression = mapping.FunctionExpression?.Replace(token, value);
				}
			}
		}
	}
}