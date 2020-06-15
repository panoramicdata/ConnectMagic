// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
	 "Usage",
	 "RCS1113:Use 'string.IsNullOrEmpty' method.",
	 Justification = "This rule is nonsense.",
	 Scope = "member",
	 Target = "~M:PanoramicData.ConnectMagic.Service.ConnectedSystemManagers.SalesforceConnectedSystemManager.#ctor(PanoramicData.ConnectMagic.Service.Models.ConnectedSystem,PanoramicData.ConnectMagic.Service.Models.State,System.TimeSpan,Microsoft.Extensions.Logging.ILoggerFactory)")]
