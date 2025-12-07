using System;
using Object = UnityEngine.Object;

namespace CizaUniTask
{
	/// <summary>
	/// Thrown upon cancellation of an async operation due to Unity object being destroyed.
	/// </summary>
	public class AsyncOperationDestroyedException : AsyncOperationCanceledException
	{
		public AsyncOperationDestroyedException(Object obj)
		{
			if (obj != null)
				throw new ArgumentException("Specified object is not destroyed.", nameof(obj));
		}
	}
}