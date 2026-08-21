using System;
using System.Collections.Generic;
using JREMonitors.Core.Managers;
using JREMonitors.Core.Reactive;
using JREMonitors.Core.State;

namespace JREMonitors.Core.Widgets
{
    public abstract class ViewModel : IDisposable
    {
        private readonly DisposableStack _disposableStack = new DisposableStack();
        private List<ViewModel> _subViewModels;
        public bool IsInitialized { get; private set; }

        public virtual void Dispose()
        {
            _disposableStack.Dispose();
            if (_subViewModels != null)
            {
                for (var i = 0; i < _subViewModels.Count; i++)
                    if (_subViewModels[i] is IDisposable disposable)
                        disposable.Dispose();

                _subViewModels.Clear();
            }
        }

        protected void RegisterResource(IDisposable resource)
        {
            _disposableStack.AddResource(resource);
        }

        protected Computed<T> CreateComputed<T>(
            Func<T> supplier,
            bool enableDynamicUnbinding = true,
            Func<T, T> converter = null
        )
        {
            var computed = new Computed<T>(supplier, enableDynamicUnbinding, converter);
            RegisterResource(computed);
            return computed;
        }

        protected PropertySlot<T> CreatePropertySlot<T>(T initial = default)
        {
            var slot = new PropertySlot<T>(initial);
            RegisterResource(slot);
            return slot;
        }

        protected PropertySlot<T> CreatePropertySlot<T>(IValueSignal<T> source)
        {
            var slot = new PropertySlot<T>(source);
            RegisterResource(slot);
            return slot;
        }

        protected ReactiveList<T> CreateReactiveList<T>(IEnumerable<T> initial = null, int capacity = 0)
        {
            var list = new ReactiveList<T>(initial, capacity);
            RegisterResource(list);
            return list;
        }

        protected ReactiveList<T> CreateReactiveList<T>(IValueSignal<IReadOnlyList<T>> source)
        {
            var list = new ReactiveList<T>(source);
            RegisterResource(list);
            return list;
        }

        protected ReactiveArray<T> CreateReactiveArray<T>(int length)
        {
            var array = new ReactiveArray<T>(length);
            RegisterResource(array);
            return array;
        }

        protected ReactiveArray<T> CreateReactiveArray<T>(int length, IValueSignal<IReadOnlyList<T>> source)
        {
            var array = new ReactiveArray<T>(length, source);
            RegisterResource(array);
            return array;
        }

        protected void AddSubViewModel(ViewModel subViewModel)
        {
            if (_subViewModels == null) _subViewModels = new List<ViewModel>();

            _subViewModels.Add(subViewModel);
        }

        public void Initialize(DataHub dataHub)
        {
            if (IsInitialized) return;
            OnInitialize(dataHub);
            IsInitialized = true;
            if (_subViewModels == null) return;
            for (var i = 0; i < _subViewModels.Count; i++) _subViewModels[i].Initialize(dataHub);
        }

        protected virtual void OnInitialize(DataHub dataHub)
        {
        }

        public void Enter()
        {
            OnEnter();
        }

        protected virtual void OnEnter()
        {
        }

        public void Update(TimeSpan elapsed)
        {
            OnUpdate(elapsed);
            if (_subViewModels == null) return;
            for (var i = 0; i < _subViewModels.Count; i++) _subViewModels[i].Update(elapsed);
        }

        public void Exit()
        {
            OnExit();
            if (_subViewModels == null) return;
            for (var i = 0; i < _subViewModels.Count; i++) _subViewModels[i].Exit();
        }

        protected virtual void OnUpdate(TimeSpan elapsed)
        {
        }


        protected virtual void OnExit()
        {
        }

        public void Reset()
        {
            OnReset();
            if (_subViewModels == null) return;
            for (var i = 0; i < _subViewModels.Count; i++) _subViewModels[i].Reset();
        }

        protected virtual void OnReset()
        {
        }
    }
}