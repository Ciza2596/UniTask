using System.Threading;
using CizaUniTask;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class AddressableLoadAssetExample : MonoBehaviour
{
	[SerializeField]
	protected string _prefabAddress = "AddressableLoadAssetExamplePrefab";

	protected GameObject _prefab;

	protected GameObject _prefabInstance;

	public virtual async void OnEnable()
	{
		_prefab = await Addressables.LoadAssetAsync<GameObject>(_prefabAddress).WithCancellation(CancellationToken.None);
		_prefabInstance = Instantiate(_prefab, transform);
	}


	public virtual void OnDisable()
	{
		Destroy(_prefabInstance);
		Addressables.Release(_prefab);
	}
}