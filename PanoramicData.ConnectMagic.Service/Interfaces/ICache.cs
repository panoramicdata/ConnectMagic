namespace PanoramicData.ConnectMagic.Service.Interfaces
{
	public interface ICache<T>
	{
		void Store(string key, T @object);

		bool TryGet(string key, out T @object);

		void Clear();
	}
}
