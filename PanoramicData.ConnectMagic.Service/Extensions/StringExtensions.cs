namespace PanoramicData.ConnectMagic.Service.Extensions
{
	public static class StringExtensions
	{
		public static string EmptyStringIfNull(this object? @object)
			=> @object?.ToString() ?? string.Empty;
	}
}
