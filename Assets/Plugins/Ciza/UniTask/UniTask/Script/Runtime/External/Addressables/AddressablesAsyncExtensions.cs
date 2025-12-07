// asmdef Version Defines, enabled when com.unity.addressables is imported.

#if UNITASK_ADDRESSABLE_SUPPORT

using System;
using System.Threading;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CizaUniTask
{
	public static class AddressablesAsyncExtensions
	{
		#region AsyncOperationHandle

		public static UniTask.Awaiter GetAwaiter(this AsyncOperationHandle handle)
		{
			return ToUniTask(handle).GetAwaiter();
		}

		public static UniTask WithCancellation(this AsyncOperationHandle handle, CancellationToken cancellationToken)
		{
			return ToUniTask(handle, cancellationToken: cancellationToken);
		}

		public static UniTask ToUniTask(this AsyncOperationHandle handle, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (cancellationToken.IsCancellationRequested) return UniTask.FromCanceled(cancellationToken);

			if (!handle.IsValid())
			{
				// autoReleaseHandle:true handle is invalid(immediately internal handle == null) so return completed.
				return UniTask.CompletedTask;
			}

			if (handle.IsDone)
			{
				if (handle.Status == AsyncOperationStatus.Failed)
				{
					return UniTask.FromException(handle.OperationException);
				}

				return UniTask.CompletedTask;
			}

			return new AsyncOperationHandlePromise(handle, progress, timing, cancellationToken).Task;
			// return new UniTask(AsyncOperationHandleConfiguredSource.Create(handle, timing, progress, cancellationToken, cancelImmediately, autoReleaseWhenCanceled, out var token), token);
		}


		private sealed class AsyncOperationHandlePromise : PlayerLoopReusablePromiseBase
		{
			private AsyncOperationHandle _handle;
			private readonly IProgress<float> _progress;
			private bool isCompleted;

			public AsyncOperationHandlePromise(AsyncOperationHandle handle, IProgress<float> progress, PlayerLoopTiming timing, CancellationToken cancellationToken) : base(timing, cancellationToken)
			{
				_handle = handle;
				_handle.Completed += HandleCompleted;
				_progress = progress;
			}

			protected override void OnRunningStart()
			{
				isCompleted = false;
			}

			public override bool MoveNext()
			{
				if (isCompleted) return false;

				if (cancellationToken.IsCancellationRequested)
				{
					isCompleted = true;
					Complete();
					TrySetCanceled();
					return false;
				}

				if (_progress != null && _handle.IsValid())
					_progress.Report(_handle.GetDownloadStatus().Percent);

				return true;
			}

			private void HandleCompleted(AsyncOperationHandle handle)
			{
				if (isCompleted)
					return;

				isCompleted = true;

				Complete();
				if (cancellationToken.IsCancellationRequested)
					TrySetCanceled();

				else
					TrySetResult();
			}
		}

		#region Old

		// sealed class AsyncOperationHandleConfiguredSource : IUniTaskSource, IPlayerLoopItem, ITaskPoolNode<AsyncOperationHandleConfiguredSource>
		// {
		// 	static TaskPool<AsyncOperationHandleConfiguredSource> pool;
		// 	AsyncOperationHandleConfiguredSource nextNode;
		// 	public ref AsyncOperationHandleConfiguredSource NextNode => ref nextNode;
		//
		// 	static AsyncOperationHandleConfiguredSource()
		// 	{
		// 		TaskPool.RegisterSizeGetter(typeof(AsyncOperationHandleConfiguredSource), () => pool.Size);
		// 	}
		//
		// 	readonly Action<AsyncOperationHandle> completedCallback;
		// 	AsyncOperationHandle handle;
		// 	CancellationToken cancellationToken;
		// 	CancellationTokenRegistration cancellationTokenRegistration;
		// 	IProgress<float> progress;
		// 	bool autoReleaseWhenCanceled;
		// 	bool cancelImmediately;
		// 	bool completed;
		//
		// 	UniTaskCompletionSourceCore<AsyncUnit> core;
		//
		// 	AsyncOperationHandleConfiguredSource()
		// 	{
		// 		completedCallback = HandleCompleted;
		// 	}
		//
		// 	public static IUniTaskSource Create(AsyncOperationHandle handle, PlayerLoopTiming timing, IProgress<float> progress, CancellationToken cancellationToken, bool cancelImmediately, bool autoReleaseWhenCanceled, out short token)
		// 	{
		// 		if (cancellationToken.IsCancellationRequested)
		// 		{
		// 			return AutoResetUniTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
		// 		}
		//
		// 		if (!pool.TryPop(out var result))
		// 		{
		// 			result = new AsyncOperationHandleConfiguredSource();
		// 		}
		//
		// 		result.handle = handle;
		// 		result.progress = progress;
		// 		result.cancellationToken = cancellationToken;
		// 		result.cancelImmediately = cancelImmediately;
		// 		result.autoReleaseWhenCanceled = autoReleaseWhenCanceled;
		// 		result.completed = false;
		//
		// 		if (cancelImmediately && cancellationToken.CanBeCanceled)
		// 		{
		// 			result.cancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(state =>
		// 			{
		// 				var promise = (AsyncOperationHandleConfiguredSource)state;
		// 				if (promise.autoReleaseWhenCanceled && promise.handle.IsValid())
		// 				{
		// 					Addressables.Release(promise.handle);
		// 				}
		//
		// 				promise.core.TrySetCanceled(promise.cancellationToken);
		// 			}, result);
		// 		}
		//
		// 		TaskTracker.TrackActiveTask(result, 3);
		//
		// 		PlayerLoopHelper.AddAction(timing, result);
		//
		// 		handle.Completed += result.completedCallback;
		//
		// 		token = result.core.Version;
		// 		return result;
		// 	}
		//
		// 	void HandleCompleted(AsyncOperationHandle _)
		// 	{
		// 		if (handle.IsValid())
		// 		{
		// 			handle.Completed -= completedCallback;
		// 		}
		//
		// 		if (completed)
		// 		{
		// 			return;
		// 		}
		//
		// 		completed = true;
		// 		if (cancellationToken.IsCancellationRequested)
		// 		{
		// 			if (autoReleaseWhenCanceled && handle.IsValid())
		// 			{
		// 				Addressables.Release(handle);
		// 			}
		//
		// 			core.TrySetCanceled(cancellationToken);
		// 		}
		// 		else if (handle.Status == AsyncOperationStatus.Failed)
		// 		{
		// 			core.TrySetException(handle.OperationException);
		// 		}
		// 		else
		// 		{
		// 			core.TrySetResult(AsyncUnit.Default);
		// 		}
		// 	}
		//
		// 	public void GetResult(short token)
		// 	{
		// 		try
		// 		{
		// 			core.GetResult(token);
		// 		}
		// 		finally
		// 		{
		// 			if (!(cancelImmediately && cancellationToken.IsCancellationRequested))
		// 			{
		// 				TryReturn();
		// 			}
		// 			else
		// 			{
		// 				TaskTracker.RemoveTracking(this);
		// 			}
		// 		}
		// 	}
		//
		// 	public UniTaskStatus GetStatus(short token)
		// 	{
		// 		return core.GetStatus(token);
		// 	}
		//
		// 	public UniTaskStatus UnsafeGetStatus()
		// 	{
		// 		return core.UnsafeGetStatus();
		// 	}
		//
		// 	public void OnCompleted(Action<object> continuation, object state, short token)
		// 	{
		// 		core.OnCompleted(continuation, state, token);
		// 	}
		//
		// 	public bool MoveNext()
		// 	{
		// 		if (completed)
		// 		{
		// 			return false;
		// 		}
		//
		// 		if (cancellationToken.IsCancellationRequested)
		// 		{
		// 			completed = true;
		// 			if (autoReleaseWhenCanceled && handle.IsValid())
		// 			{
		// 				Addressables.Release(handle);
		// 			}
		//
		// 			core.TrySetCanceled(cancellationToken);
		// 			return false;
		// 		}
		//
		// 		if (progress != null && handle.IsValid())
		// 		{
		// 			progress.Report(handle.GetDownloadStatus().Percent);
		// 		}
		//
		// 		return true;
		// 	}
		//
		// 	bool TryReturn()
		// 	{
		// 		TaskTracker.RemoveTracking(this);
		// 		core.Reset();
		// 		handle = default;
		// 		progress = default;
		// 		cancellationToken = default;
		// 		cancellationTokenRegistration.Dispose();
		// 		return pool.TryPush(this);
		// 	}
		// }

		#endregion

		#endregion

		#region AsyncOperationHandle_T

		public static UniTask<T>.Awaiter GetAwaiter<T>(this AsyncOperationHandle<T> handle)
		{
			return ToUniTask(handle).GetAwaiter();
		}

		public static UniTask<T> WithCancellation<T>(this AsyncOperationHandle<T> handle, CancellationToken cancellationToken)
		{
			return ToUniTask(handle, cancellationToken: cancellationToken);
		}

		public static UniTask<T> ToUniTask<T>(this AsyncOperationHandle<T> handle, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (cancellationToken.IsCancellationRequested) return UniTask.FromCanceled<T>(cancellationToken);

			if (!handle.IsValid())
			{
				throw new Exception("Attempting to use an invalid operation handle");
			}

			if (handle.IsDone)
			{
				if (handle.Status == AsyncOperationStatus.Failed)
				{
					return UniTask.FromException<T>(handle.OperationException);
				}

				return UniTask.FromResult(handle.Result);
			}

			return new AsyncOperationHandlePromise<T>(handle, progress, timing, cancellationToken).Task;
			// return new UniTask<T>(AsyncOperationHandleConfiguredSource<T>.Create(handle, timing, progress, cancellationToken, cancelImmediately, autoReleaseWhenCanceled, out var token), token);
		}

		private class AsyncOperationHandlePromise<T> : PlayerLoopReusablePromiseBase<T>
		{
			private readonly AsyncOperationHandle<T> _handle;
			private readonly IProgress<float> _progress;
			private bool isCompleted;

			public AsyncOperationHandlePromise(AsyncOperationHandle<T> handle, IProgress<float> progress, PlayerLoopTiming timing, CancellationToken cancellationToken) : base(timing, cancellationToken)
			{
				_handle = handle;
				_handle.Completed += HandleCompleted;
				_progress = progress;
			}

			protected override void OnRunningStart()
			{
				isCompleted = false;
			}

			public override bool MoveNext()
			{
				if (isCompleted) return false;

				if (cancellationToken.IsCancellationRequested)
				{
					isCompleted = true;
					Complete();
					TrySetCanceled();
					return false;
				}

				if (_progress != null && _handle.IsValid())
					_progress.Report(_handle.GetDownloadStatus().Percent);

				return true;
			}

			private void HandleCompleted(AsyncOperationHandle<T> handle)
			{
				if (isCompleted)
					return;

				isCompleted = true;

				Complete();
				if (cancellationToken.IsCancellationRequested)
					TrySetCanceled();

				else
					TrySetResult(handle.Result);
			}
		}


		#region Old

		// sealed class AsyncOperationHandleConfiguredSource<T> : IUniTaskSource<T>, IPlayerLoopItem, ITaskPoolNode<AsyncOperationHandleConfiguredSource<T>>
		// {
		// 	static TaskPool<AsyncOperationHandleConfiguredSource<T>> pool;
		// 	AsyncOperationHandleConfiguredSource<T> nextNode;
		// 	public ref AsyncOperationHandleConfiguredSource<T> NextNode => ref nextNode;
		//
		// 	static AsyncOperationHandleConfiguredSource()
		// 	{
		// 		TaskPool.RegisterSizeGetter(typeof(AsyncOperationHandleConfiguredSource<T>), () => pool.Size);
		// 	}
		//
		// 	readonly Action<AsyncOperationHandle<T>> completedCallback;
		// 	AsyncOperationHandle<T> handle;
		// 	CancellationToken cancellationToken;
		// 	CancellationTokenRegistration cancellationTokenRegistration;
		// 	IProgress<float> progress;
		// 	bool autoReleaseWhenCanceled;
		// 	bool cancelImmediately;
		// 	bool completed;
		//
		// 	UniTaskCompletionSourceCore<T> core;
		//
		// 	AsyncOperationHandleConfiguredSource()
		// 	{
		// 		completedCallback = HandleCompleted;
		// 	}
		//
		// 	public static IUniTaskSource<T> Create(AsyncOperationHandle<T> handle, PlayerLoopTiming timing, IProgress<float> progress, CancellationToken cancellationToken, bool cancelImmediately, bool autoReleaseWhenCanceled, out short token)
		// 	{
		// 		if (cancellationToken.IsCancellationRequested)
		// 		{
		// 			return AutoResetUniTaskCompletionSource<T>.CreateFromCanceled(cancellationToken, out token);
		// 		}
		//
		// 		if (!pool.TryPop(out var result))
		// 		{
		// 			result = new AsyncOperationHandleConfiguredSource<T>();
		// 		}
		//
		// 		result.handle = handle;
		// 		result.cancellationToken = cancellationToken;
		// 		result.completed = false;
		// 		result.progress = progress;
		// 		result.autoReleaseWhenCanceled = autoReleaseWhenCanceled;
		// 		result.cancelImmediately = cancelImmediately;
		//
		// 		if (cancelImmediately && cancellationToken.CanBeCanceled)
		// 		{
		// 			result.cancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(state =>
		// 			{
		// 				var promise = (AsyncOperationHandleConfiguredSource<T>)state;
		// 				if (promise.autoReleaseWhenCanceled && promise.handle.IsValid())
		// 				{
		// 					Addressables.Release(promise.handle);
		// 				}
		//
		// 				promise.core.TrySetCanceled(promise.cancellationToken);
		// 			}, result);
		// 		}
		//
		// 		TaskTracker.TrackActiveTask(result, 3);
		//
		// 		PlayerLoopHelper.AddAction(timing, result);
		//
		// 		handle.Completed += result.completedCallback;
		//
		// 		token = result.core.Version;
		// 		return result;
		// 	}
		//
		// 	void HandleCompleted(AsyncOperationHandle<T> argHandle)
		// 	{
		// 		if (handle.IsValid())
		// 		{
		// 			handle.Completed -= completedCallback;
		// 		}
		//
		// 		if (completed)
		// 		{
		// 			return;
		// 		}
		//
		// 		completed = true;
		// 		if (cancellationToken.IsCancellationRequested)
		// 		{
		// 			if (autoReleaseWhenCanceled && handle.IsValid())
		// 			{
		// 				Addressables.Release(handle);
		// 			}
		//
		// 			core.TrySetCanceled(cancellationToken);
		// 		}
		// 		else if (argHandle.Status == AsyncOperationStatus.Failed)
		// 		{
		// 			core.TrySetException(argHandle.OperationException);
		// 		}
		// 		else
		// 		{
		// 			core.TrySetResult(argHandle.Result);
		// 		}
		// 	}
		//
		// 	public T GetResult(short token)
		// 	{
		// 		try
		// 		{
		// 			return core.GetResult(token);
		// 		}
		// 		finally
		// 		{
		// 			if (!(cancelImmediately && cancellationToken.IsCancellationRequested))
		// 			{
		// 				TryReturn();
		// 			}
		// 			else
		// 			{
		// 				TaskTracker.RemoveTracking(this);
		// 			}
		// 		}
		// 	}
		//
		// 	void IUniTaskSource.GetResult(short token)
		// 	{
		// 		GetResult(token);
		// 	}
		//
		// 	public UniTaskStatus GetStatus(short token)
		// 	{
		// 		return core.GetStatus(token);
		// 	}
		//
		// 	public UniTaskStatus UnsafeGetStatus()
		// 	{
		// 		return core.UnsafeGetStatus();
		// 	}
		//
		// 	public void OnCompleted(Action<object> continuation, object state, short token)
		// 	{
		// 		core.OnCompleted(continuation, state, token);
		// 	}
		//
		// 	public bool MoveNext()
		// 	{
		// 		if (completed)
		// 		{
		// 			return false;
		// 		}
		//
		// 		if (cancellationToken.IsCancellationRequested)
		// 		{
		// 			completed = true;
		// 			if (autoReleaseWhenCanceled && handle.IsValid())
		// 			{
		// 				Addressables.Release(handle);
		// 			}
		//
		// 			core.TrySetCanceled(cancellationToken);
		// 			return false;
		// 		}
		//
		// 		if (progress != null && handle.IsValid())
		// 		{
		// 			progress.Report(handle.GetDownloadStatus().Percent);
		// 		}
		//
		// 		return true;
		// 	}
		//
		// 	bool TryReturn()
		// 	{
		// 		TaskTracker.RemoveTracking(this);
		// 		core.Reset();
		// 		handle = default;
		// 		progress = default;
		// 		cancellationToken = default;
		// 		cancellationTokenRegistration.Dispose();
		// 		return pool.TryPush(this);
		// 	}
		// }

		#endregion

		#endregion
	}
}

#endif