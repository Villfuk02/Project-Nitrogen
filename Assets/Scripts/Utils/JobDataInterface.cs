using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Utils
{
    public class JobDataInterface
    {
        readonly Allocator allocator_;
        readonly Dictionary<Array, (Mode mode, NativeArrayWrapper native)> arrayMap_ = new();
        readonly Dictionary<IList, (Mode mode, NativeListWrapper native)> listMap_ = new();
        readonly bool[] failed_;
        public bool IsFinished { get; private set; }
        public bool Failed { get => failed_[0]; }

        [Flags] public enum Mode : byte { Input = 1, Output = 2, InOut = 3 }


        public JobDataInterface(in Allocator allocator)
        {
            allocator_ = allocator;
            failed_ = new bool[1];
        }

        public NativeArray<T> Register<T>(in T[] array, Mode mode) where T : struct
        {
            if (arrayMap_.ContainsKey(array))
                throw new ArgumentException("Array already registered.");
            var native = new NativeArrayWrapper<T>(array, allocator_);
            arrayMap_.Add(array, (mode, native));
            return native.Native;
        }
        public NativeList<T> Register<T>(in List<T> list, Mode mode) where T : unmanaged
        {
            if (listMap_.ContainsKey(list))
                throw new ArgumentException("List already registered.");
            var native = new NativeListWrapper<T>(list, allocator_);
            listMap_.Add(list, (mode, native));
            return native.Native;
        }

        public NativeArray<bool> RegisterFailed()
        {
            return Register(failed_, Mode.Output);
        }

        public void RegisterHandle(MonoBehaviour owner, JobHandle handle)
        {
            owner.StartCoroutine(WaitAndFinish(handle));
        }
        IEnumerator WaitAndFinish(JobHandle handle)
        {
            yield return new WaitUntil(() => handle.IsCompleted);
            handle.Complete();
            foreach (var pair in arrayMap_)
            {
                (Array array, (Mode mode, NativeArrayWrapper native)) = pair;
                if (mode.HasFlag(Mode.Output))
                {
                    native.CopyTo(array);
                }
                native.Dispose();
            }
            foreach (var pair in listMap_)
            {
                (IList list, (Mode mode, NativeListWrapper native)) = pair;
                if (mode.HasFlag(Mode.Output))
                {
                    native.CopyTo(list);
                }
                native.Dispose();
            }
            IsFinished = true;
        }

        public abstract class NativeArrayWrapper
        {
            public abstract Type Type();
            public abstract void CopyTo(Array array);
            public abstract void Dispose();
        }

        public class NativeArrayWrapper<T> : NativeArrayWrapper where T : struct
        {
            NativeArray<T> native_;
            public NativeArray<T> Native { get => native_; }
            public NativeArrayWrapper(T[] array, in Allocator allocator)
            {
                native_ = new(array, allocator);
            }
            public override Type Type()
            {
                return typeof(T);
            }

            public override void CopyTo(Array array)
            {
                native_.CopyTo((T[])array);
            }

            public override void Dispose()
            {
                native_.Dispose();
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
            NativeList<T> native_;
            public NativeList<T> Native { get => native_; }
            public NativeListWrapper(IReadOnlyCollection<T> list, in Allocator allocator)
            {
                native_ = new(list.Count, allocator);
                foreach (var t in list)
                {
                    native_.AddNoResize(t);
                }
            }
            public override Type Type()
            {
                return typeof(T);
            }

            public override void CopyTo(IList list)
            {
                var realList = (List<T>)list;
                realList.Clear();
                realList.AddRange(native_.ToArray());
            }

            public override void Dispose()
            {
                native_.Dispose();
            }
        }
    }
}
