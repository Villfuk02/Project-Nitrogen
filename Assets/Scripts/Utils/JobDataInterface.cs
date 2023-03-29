using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Assets.Scripts.Utils
{
    public class JobDataInterface
    {
        readonly Allocator _allocator;
        readonly Dictionary<Array, (bool output, NativeArrayWrapper native)> _arrayMap = new();
        readonly Dictionary<IList, (bool output, NativeListWrapper native)> _listMap = new();
        bool _finished;
        readonly bool[] _failed;
        public bool IsFinished { get => _finished; }
        public bool Failed { get => _failed[0]; }

        public JobDataInterface(in Allocator allocator)
        {
            _allocator = allocator;
            _failed = new bool[1];
        }

        public NativeArray<T> Register<T>(in T[] array, bool output) where T : struct
        {
            if (_arrayMap.ContainsKey(array))
                throw new Exception("Array already registered.");
            NativeArrayWrapper<T> native = new(array, _allocator);
            _arrayMap.Add(array, (output, native));
            return native.Native;
        }
        public NativeList<T> Register<T>(in List<T> list, bool output) where T : unmanaged
        {
            if (_listMap.ContainsKey(list))
                throw new Exception("List already registered.");
            NativeListWrapper<T> native = new(list, _allocator);
            _listMap.Add(list, (output, native));
            return native.Native;
        }

        public NativeArray<bool> RegisterFailed()
        {
            return Register(_failed, true);
        }

        public void RegisterHandle(MonoBehaviour owner, JobHandle handle)
        {
            owner.StartCoroutine(WaitAndFinish(handle));
        }
        IEnumerator WaitAndFinish(JobHandle handle)
        {
            yield return new WaitUntil(() => handle.IsCompleted);
            handle.Complete();
            foreach (var pair in _arrayMap)
            {
                (Array array, (bool output, NativeArrayWrapper native)) = pair;
                if (output)
                {
                    native.CopyTo(array);
                }
                native.Dispose();
            }
            foreach (var pair in _listMap)
            {
                (IList list, (bool output, NativeListWrapper native)) = pair;
                if (output)
                {
                    native.CopyTo(list);
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

        public abstract class NativeListWrapper
        {
            public abstract Type Type();
            public abstract void CopyTo(IList list);
            public abstract void Dispose();
        }

        public class NativeListWrapper<T> : NativeListWrapper where T : unmanaged
        {
            NativeList<T> _native;
            public NativeList<T> Native { get => _native; }
            public NativeListWrapper(List<T> list, in Allocator allocator)
            {
                _native = new NativeList<T>(list.Count, allocator);
                for (int i = 0; i < list.Count; i++)
                {
                    _native.AddNoResize(list[i]);
                }
            }
            public override Type Type()
            {
                return typeof(T);
            }

            public override void CopyTo(IList list)
            {
                List<T> realList = (List<T>)list;
                realList.Clear();
                realList.AddRange(_native.ToArray());
            }

            public override void Dispose()
            {
                _native.Dispose();
            }
        }
    }
}
