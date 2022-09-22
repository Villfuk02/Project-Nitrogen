using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.Utils
{
    public class JobDataInterface
    {
        readonly Allocator _allocator;
        readonly Dictionary<Array, (bool output, NativeArrayWrapper native)> _map = new();
        bool _finished;
        public bool IsFinished { get => _finished; }

        public JobDataInterface(in Allocator allocator)
        {
            _allocator = allocator;
        }

        public NativeArray<T> Register<T>(in T[] array, bool output) where T : struct
        {
            if (_map.ContainsKey(array))
                throw new Exception("Array already registered.");
            NativeArrayWrapper<T> native = new(array, _allocator);
            _map.Add(array, (output, native));
            return native.Native;
        }

        public NativeArray<T> GetNative<T>(in T[] array) where T : struct
        {
            if (!_map.ContainsKey(array))
                throw new Exception("Array not foud.");
            if (typeof(T) != _map[array].native.Type())
                throw new Exception("Types don't match");
            return ((NativeArrayWrapper<T>)_map[array].native).Native;
        }

        public void RegisterHandle(MonoBehaviour owner, JobHandle handle)
        {
            owner.StartCoroutine(WaitAndFinish(handle));
        }
        IEnumerator WaitAndFinish(JobHandle handle)
        {
            yield return new WaitUntil(() => handle.IsCompleted);
            handle.Complete();
            foreach (var pair in _map)
            {
                (Array array, (bool output, NativeArrayWrapper native)) = pair;
                if (output)
                {
                    native.CopyTo(array);
                }
                native.Dispose();
            }
            _finished = true;
        }

        public abstract class NativeArrayWrapper
        {
            public abstract Type Type();
            public abstract void CopyTo(Array array);
            public abstract void Dispose();
        }

        public class NativeArrayWrapper<T> : NativeArrayWrapper where T : struct
        {
            NativeArray<T> _native;
            public NativeArray<T> Native { get => _native; }
            public NativeArrayWrapper(T[] array, in Allocator allocator)
            {
                _native = new NativeArray<T>(array, allocator);
            }
            public override Type Type()
            {
                return typeof(T);
            }

            public override void CopyTo(Array array)
            {
                _native.CopyTo((T[])array);
            }

            public override void Dispose()
            {
                _native.Dispose();
            }
        }
    }
}
