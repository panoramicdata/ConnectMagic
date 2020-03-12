using PanoramicData.ConnectMagic.Service.Exceptions;
using System.Runtime.Serialization;

namespace PanoramicData.ConnectMagic.Service.Models
{
	/// <summary>
	/// A constant that can be users in expressions
	/// </summary>
	[DataContract]
	public class Constant
	{
		/// <summary>
		/// The token to be replaced
		/// </summary>
		[DataMember(Name = "Token")]
		public string Token { get; set; } = string.Empty;

		/// <summary>
		/// The value to substitute in.
		/// </summary>
		[DataMember(Name = "Value")]
		public string Value { get; set; } = string.Empty;

		internal void Validate(string connectedSystemDataSetName)
		{
			if (string.IsNullOrWhiteSpace(Token))
			{
				throw new ConfigurationException($"{nameof(ConnectedSystemDataSet)} {connectedSystemDataSetName}'s {nameof(Constant)} {nameof(Token)}s must not be empty or whitespace.");
			}

			if (Value is null)
			{
				throw new ConfigurationException($"{nameof(ConnectedSystemDataSet)} {connectedSystemDataSetName}'s {nameof(Constant)} {nameof(Value)}s must not be null.");
			}
		}
	}
}