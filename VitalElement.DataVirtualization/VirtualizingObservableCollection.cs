namespace VitalElement.DataVirtualization
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using DataManagement;
    using Interfaces;
    using Pageing;

    public class VirtualizingObservableCollection<T> : IEnumerable, IEnumerable<T>, ICollection, ICollection<T>, IList, IReadOnlyList<T>, IReadOnlyObservableCollection<T>,
        IList<T>, IObservableCollection<T>, INotifyCollectionChanged, INotifyPropertyChanged where T : class, IDataItem
    {
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator() => new VirtualizingObservableCollectionEnumerator<T>(this);

        public class VirtualizingObservableCollectionEnumerator<T> : IEnumerator<T> where T : class, IDataItem
        {
            private int _iLoop;

            /// <summary>
            ///     Initializes a new instance of the <see cref="T:System.Object" /> class.
            /// </summary>
            public VirtualizingObservableCollectionEnumerator(VirtualizingObservableCollection<T> baseCollection)
            {
                BaseCollection = baseCollection;
            }

            public VirtualizingObservableCollection<T> BaseCollection { get; }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                var sc = Guid.NewGuid	().ToString();

                BaseCollection.EnsureCountIsGotNonaSync();

                try
                {
                    var count = BaseCollection.InternalGetCount();
                    if (_iLoop >= count) return false;

                    Current = BaseCollection.InternalGetValue(_iLoop++, sc);
                    if (Current == null) Debugger.Break();
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            public void Reset()
            {
                _iLoop = 0;
                Current = null;
            }

            /// <inheritdoc />
            /// <summary>
            ///     Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            /// <returns>
            ///     The element in the collection at the current position of the enumerator.
            /// </returns>
            public T Current { get; private set; }

            object IEnumerator.Current => Current;
        }


        /// <summary>
        ///     Initializes a new instance of the <see cref="VirtualizingObservableCollection{T}" /> class.
        /// </summary>
        /// <param name="provider">The provider.</param>
        public VirtualizingObservableCollection(IItemSourceProvider<T> provider) : this()
        {
            Provider = provider;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="VirtualizingObservableCollection{T}" /> class.
        /// </summary>
        /// <param name="asyncProvider">The asynchronous provider.</param>
        public VirtualizingObservableCollection(IItemSourceProviderAsync<T> asyncProvider) : this()
        {
            ProviderAsync = asyncProvider;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="VirtualizingObservableCollection{T}" /> class.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="reclaimer">The optional reclaimer.</param>
        /// <param name="expiryComparer">The optional expiry comparer.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="maxPages">The maximum pages.</param>
        /// <param name="maxDeltas">The maximum deltas.</param>
        /// <param name="maxDistance">The maximum distance.</param>
        public VirtualizingObservableCollection(
            IPagedSourceProvider<T> provider,
            IPageReclaimer<T> reclaimer = null,
            IPageExpiryComparer expiryComparer = null,
            int pageSize = 100,
            int maxPages = 100,
            int maxDeltas = -1,
            int maxDistance = -1) : this()
        {
            Provider = new PaginationManager<T>(provider, reclaimer, expiryComparer, pageSize, maxPages, maxDeltas,
                maxDistance);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="VirtualizingObservableCollection{T}" /> class.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="reclaimer">The optional reclaimer.</param>
        /// <param name="expiryComparer">The optional expiry comparer.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="maxPages">The maximum pages.</param>
        /// <param name="maxDeltas">The maximum deltas.</param>
        /// <param name="maxDistance">The maximum distance.</param>
        public VirtualizingObservableCollection(
            IPagedSourceObservableProvider<T> provider,
            IPageReclaimer<T> reclaimer = null,
            IPageExpiryComparer expiryComparer = null,
            int pageSize = 100,
            int maxPages = 100,
            int maxDeltas = -1,
            int maxDistance = -1) : this()
        {
            Provider = new PaginationManager<T>(provider, reclaimer, expiryComparer, pageSize, maxPages, maxDeltas,
                maxDistance);
            ((PaginationManager<T>) Provider).CollectionChanged +=
                VirtualizingObservableCollection_CollectionChanged;
            IsSourceObservable = true;
        }

        public bool IsSourceObservable { get; set; }


        protected VirtualizingObservableCollection()
        {
            //To enable reset in case that noone set UiThreadExcecuteAction
            if (VirtualizationManager.Instance.UiThreadExcecuteAction == null)
            {
                throw new Exception("VirtualizationManager.Instance.UiThreadExcecuteAction is not set.");
            }
        }


        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        /// <summary>
        ///     Gets the number of elements contained in the <see cref="T:System.Collections.ICollection" />.
        /// </summary>
        /// <returns>The number of elements contained in the <see cref="T:System.Collections.ICollection" />.</returns>
        public int Count => InternalGetCount();

        /// <inheritdoc />
        /// <summary>
        ///     Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection" /> is synchronized
        ///     (thread safe).
        /// </summary>
        /// <returns>
        ///     true if access to the <see cref="T:System.Collections.ICollection" /> is synchronized (thread safe);
        ///     otherwise, false.
        /// </returns>
        public bool IsSynchronized => false;

        /// <inheritdoc />
        /// <summary>
        ///     Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />.
        /// </summary>
        /// <returns>An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />.</returns>
        public object SyncRoot { get; } = new object();

        /// <summary>
        ///     Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        public void Add(T item)
        {
            InternalAdd(item, null);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Resets the collection - aka forces a get all data, including the count
        ///     <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        public void Clear()
        {
            InternalClear();
        }

        /// <inheritdoc />
        /// <summary>
        ///     Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>
        ///     true if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.ICollection`1" />;
        ///     otherwise, false.
        /// </returns>
        public bool Contains(T item)
        {
            return Provider.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach (var item in this)
            {
                array[arrayIndex++] = item;
            }
        }

        /// <inheritdoc cref="" />
        /// <summary>
        ///     Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </summary>
        /// <returns>true if the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only; otherwise, false.</returns>
        public bool IsReadOnly => false;

        /// <inheritdoc />
        /// <summary>
        ///     Removes the first occurrence of a specific object from the
        ///     <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>
        ///     true if <paramref name="item" /> was successfully removed from the
        ///     <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false. This method also returns false if
        ///     <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </returns>
        public bool Remove(T item)
        {
            return InternalRemove(item);
        }

        /// <summary>
        ///     Removes the specified item - extended to only remove the item if the page was not pulled before the updatedat
        ///     DateTime.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updatedAt">The updated at.</param>
        /// <returns></returns>
        public bool Remove(T item, object updatedAt)
        {
            return InternalRemove(item, updatedAt);
        }

        /// <summary>
        ///     Removes at the given index - extended to only remove the item if the page was not pulled before the updatedat
        ///     DateTime.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="updatedAt">The updated at.</param>
        /// <returns></returns>
        public bool RemoveAt(int index, object updatedAt)
        {
            return InternalRemoveAt(index, updatedAt);
        }

        /// <summary>
        ///     Adds (appends) the specified item - extended to only add the item if the page was not pulled before the updatedat
        ///     DateTime.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updatedAt">The updated at.</param>
        /// <returns></returns>
        public int Add(T item, object updatedAt)
        {
            return InternalAdd(item, updatedAt);
        }

        /// <summary>
        ///     Inserts the specified index - extended to only insert the item if the page was not pulled before the updatedat
        ///     DateTime.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <param name="updatedAt">The updated at.</param>
        public void Insert(int index, T item, object updatedAt)
        {
            InternalInsertAt(index, item, updatedAt);
        }

        /// <summary>
        ///     Adds the range.
        /// </summary>
        /// <param name="newValues">The new values.</param>
        /// <param name="timestamp">The updatedat object.</param>
        /// <returns>Index of the last appended object</returns>
        public int AddRange(IEnumerable<T> newValues, object timestamp = null)
        {
            var edit = GetProviderAsEditable();

            var index = -1;
            var items = new List<T>();

            foreach (var item in newValues)
            {
                items.Add(item);
                index = edit.OnAppend(item, timestamp);
                if (IsSourceObservable) continue;

                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index);
                RaiseCollectionChangedEvent(args);
            }

            OnCountTouched();

            return index;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Adds an item to the <see cref="T:System.Collections.IList" />.
        /// </summary>
        /// <param name="value">The object to add to the <see cref="T:System.Collections.IList" />.</param>
        /// <returns>
        ///     The position into which the new element was inserted, or -1 to indicate that the item was not inserted into the
        ///     collection.
        /// </returns>
        public int Add(object value)
        {
            return InternalAdd((T) value, null);
        }

        bool IObservableCollection<T>.Remove(object item)
        {
            Remove(item);
            return true;
        }

        bool IObservableCollection.Remove(object item)
        {
            Remove(item);
            return true;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Determines whether the <see cref="T:System.Collections.IList" /> contains a specific value.
        /// </summary>
        /// <param name="value">The object to locate in the <see cref="T:System.Collections.IList" />.</param>
        /// <returns>
        ///     true if the <see cref="T:System.Object" /> is found in the <see cref="T:System.Collections.IList" />; otherwise,
        ///     false.
        /// </returns>
        public bool Contains(object value)
        {
            return value != null && Contains((T) value);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Determines the index of a specific item in the <see cref="T:System.Collections.IList" />.
        /// </summary>
        /// <param name="value">The object to locate in the <see cref="T:System.Collections.IList" />.</param>
        /// <returns>
        ///     The index of <paramref name="value" /> if found in the list; otherwise, -1.
        /// </returns>
        public int IndexOf(object value)
        {
            return IndexOf((T) value);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Inserts an item to the <see cref="T:System.Collections.IList" /> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="value" /> should be inserted.</param>
        /// <param name="value">The object to insert into the <see cref="T:System.Collections.IList" />.</param>
        public void Insert(int index, object value)
        {
            Insert(index, (T) value);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Gets a value indicating whether the <see cref="T:System.Collections.IList" /> has a fixed size.
        /// </summary>
        /// <returns>true if the <see cref="T:System.Collections.IList" /> has a fixed size; otherwise, false.</returns>
        public bool IsFixedSize => false;

        void IObservableCollection.Add(object item)
        {
            Add(item);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Removes the first occurrence of a specific object from the <see cref="T:System.Collections.IList" />.
        /// </summary>
        /// <param name="value">The object to remove from the <see cref="T:System.Collections.IList" />.</param>
        public void Remove(object value)
        {
            Remove((T) value);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Removes the <see cref="T:System.Collections.IList" /> item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        public void RemoveAt(int index)
        {
            InternalRemoveAt(index);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        object IList.this[int index]
        {
            get => InternalGetValue(index, DefaultSelectionContext);
            set => InternalSetValue(index, (T) value);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1" />.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1" />.</param>
        /// <returns>
        ///     The index of <paramref name="item" /> if found in the list; otherwise, -1.
        /// </returns>
        public int IndexOf(T item)
        {
            return InternalIndexOf(item);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Inserts an item to the <see cref="T:System.Collections.Generic.IList`1" /> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
        /// <param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1" />.</param>
        public void Insert(int index, T item)
        {
            InternalInsertAt(index, item);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public T this[int index]
        {
            get => InternalGetValue(index, DefaultSelectionContext);
            set => InternalSetValue(index, value);
        }

        /// <summary>
        ///     Gets or sets the provider if its asynchronous.
        /// </summary>
        /// <value>
        ///     The provider asynchronous.
        /// </value>
        public IItemSourceProviderAsync<T> ProviderAsync
        {
            get => _providerAsync;
            set
            {
                ClearCountChangedHooks();
                _providerAsync = value;
                if (_providerAsync is INotifyCountChanged)
                {
                    (_providerAsync as INotifyCountChanged).CountChanged +=
                        VirtualizingObservableCollection_CountChanged;
                }
            }
        }

        private void VirtualizingObservableCollection_CountChanged(object sender, CountChangedEventArgs args)
        {
            if (args.NeedsReset)
            {
                // Send a reset..
                RaiseCollectionChangedEvent(CcResetArgs);
            }

            OnCountTouched();
        }

        /// <summary>
        ///     Gets or sets the provider if its not asynchronous.
        /// </summary>
        /// <value>
        ///     The provider.
        /// </value>
        public IItemSourceProvider<T> Provider
        {
            get => _provider;
            set
            {
                ClearCountChangedHooks();
                _provider = value;

                if (_provider is INotifyCountChanged changed)
                {
                    changed.CountChanged +=
                        VirtualizingObservableCollection_CountChanged;
                } //TODO check this commented code

                //if(this._provider is INotifyCollectionChanged) {
                //    (this._provider as INotifyCollectionChanged).CollectionChanged += this.VirtualizingObservableCollection_CollectionChanged;
                //}
            }
        }

        private void VirtualizingObservableCollection_CollectionChanged(object sender,
            NotifyCollectionChangedEventArgs e)
        {
            RaiseCollectionChangedEvent(e);
            OnCountTouched();
        }

        private void ClearCountChangedHooks()
        {
            if (_provider is INotifyCountChanged changed)
            {
                changed.CountChanged -=
                    VirtualizingObservableCollection_CountChanged;
            }

            if (_providerAsync is INotifyCountChanged)
            {
                (_providerAsync as INotifyCountChanged).CountChanged -=
                    VirtualizingObservableCollection_CountChanged;
            }

            //TODO check this commented code
            //if(this._provider is INotifyCollectionChanged) {
            //    (this._provider as INotifyCollectionChanged).CollectionChanged -= this.VirtualizingObservableCollection_CollectionChanged;
            //}
            //if(this._providerAsync is INotifyCollectionChanged) {
            //    (this._providerAsync as INotifyCollectionChanged).CollectionChanged -= this.VirtualizingObservableCollection_CollectionChanged;
            //}
        }

        public bool SupressEventErrors { get; set; } = false;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        event NotifyCollectionChangedEventHandler INotifyCollectionChanged.CollectionChanged
        {
            add => CollectionChanged += value;
            remove => CollectionChanged -= value;
        }

        /// <summary>
        ///     Raises the collection changed event.
        /// </summary>
        /// <param name="args">The <see cref="NotifyCollectionChangedEventArgs" /> instance containing the event data.</param>
        internal void RaiseCollectionChangedEvent(NotifyCollectionChangedEventArgs args)
        {
            if (_bulkCount > 0)
            {
                return;
            }

            var eventHandler = CollectionChanged;
            if (eventHandler == null)
            {
                return;
            }

            // Walk thru invocation list.
            var delegates = eventHandler.GetInvocationList();

            foreach (var @delegate in delegates)
            {
                var handler = (NotifyCollectionChangedEventHandler) @delegate;

                // If the subscriber is a DispatcherObject and different thread.
                /*var dispatcherObject = handler.Target as DispatcherObject;
                try
                {
                    if (dispatcherObject != null && !dispatcherObject.CheckAccess())
                    {
                        // Invoke handler in the target dispatcher's thread... 
                        // asynchronously for better responsiveness.
                        dispatcherObject.Dispatcher.BeginInvoke(DispatcherPriority.DataBind, handler, this, args);
                    }
                    else
                    {
                        // Execute handler as is.
                        handler(this, args);
                    }
                }
                catch (Exception ex
                ) //WTF? exception catch during remove operations with collection, try add and remove investigation
                {
                    Debug.WriteLine(ex.Message);
                    Debugger.Break();
                }*/
                
                handler(this, args);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add => PropertyChanged += value;
            remove => PropertyChanged -= value;
        }

        private static readonly PropertyChangedEventArgs PcCountArgs = new PropertyChangedEventArgs("Count");

        private static readonly NotifyCollectionChangedEventArgs CcResetArgs =
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);

        private void OnCountTouched()
        {
            RaisePropertyChanged(PcCountArgs);
        }

        protected void RaisePropertyChanged(PropertyChangedEventArgs args)
        {
            if (_bulkCount > 0)
            {
                return;
            }

            var evnt = PropertyChanged;
            evnt?.Invoke(this, args);
        }

        /// <summary>
        ///     Releases the bulk mode.
        /// </summary>
        internal void ReleaseBulkMode()
        {
            if (_bulkCount > 0)
            {
                _bulkCount--;
            }

            if (_bulkCount != 0) return;

            RaiseCollectionChangedEvent(CcResetArgs);
            RaisePropertyChanged(PcCountArgs);
        }

        /// <summary>
        ///     Enters the bulk mode.
        /// </summary>
        /// <returns></returns>
        public BulkMode EnterBulkMode()
        {
            _bulkCount++;

            return new BulkMode(this);
        }

        /// <inheritdoc />
        /// <summary>
        ///     The Bulk mode IDisposable proxy
        /// </summary>
        public class BulkMode : IDisposable
        {
            private readonly VirtualizingObservableCollection<T> _voc;
            private bool _isDisposed;

            public BulkMode(VirtualizingObservableCollection<T> voc)
            {
                _voc = voc;
            }

            public void Dispose()
            {
                OnDispose();
            }

            private void OnDispose()
            {
                if (_isDisposed) return;

                _isDisposed = true;
                _voc?.ReleaseBulkMode();
            }

            ~BulkMode()
            {
                OnDispose();
            }
        }

        protected string DefaultSelectionContext = new Guid().ToString();
        private IItemSourceProvider<T> _provider;
        private IItemSourceProviderAsync<T> _providerAsync;
        private int _bulkCount;

        /// <summary>
        ///     Gets the provider as editable.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException"></exception>
        protected IEditableProvider<T> GetProviderAsEditable()
        {
            IEditableProvider<T> ret = null;

            if (Provider != null)
            {
                ret = Provider as IEditableProvider<T>;
            }
            else
            {
                ret = ProviderAsync as IEditableProvider<T>;
            }

            if (ret == null)
            {
                throw new NotSupportedException();
            }

            return ret;
        }

        /// <summary>
        ///     Replaces oldValue with newValue at index if updatedat is newer or null.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        /// <param name="timestamp">The timestamp.</param>
        internal void ReplaceAt(int index, T oldValue, T newValue, object timestamp)
        {
            var edit = GetProviderAsEditable();

            if (edit == null) return;
            edit.OnReplace(index, oldValue, newValue, timestamp);

            if (IsSourceObservable) return;
            var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newValue,
                oldValue, index);
            RaiseCollectionChangedEvent(args);
        }

        private void InternalClear()
        {
            if (Provider != null)
            {
                (Provider as IProviderPreReset)?.OnBeforeReset();

                Provider.OnReset(-1);
            }
            else
            {
                if (ProviderAsync is IProviderPreReset)
                {
                    (ProviderAsync as IProviderPreReset).OnBeforeReset();
                }

                ProviderAsync.OnReset(-1);
            }
        }

        private CancellationTokenSource _resetToken;

        public void Reset()
        {
            ResetAsync().Wait();
        }

        public async Task ResetAsync()
        {
            CancellationTokenSource cts = null;

            lock (this)
            {
                if (_resetToken != null)
                {
                    _resetToken.Cancel();
                    _resetToken = null;
                }

                cts = _resetToken = new CancellationTokenSource();
            }

            if (Provider != null)
            {
                if (Provider is IProviderPreReset reset)
                {
                    reset.OnBeforeReset();
                    if (cts.IsCancellationRequested)
                    {
                        return;
                    }
                }
                //TODO find why this was commented on git?
                //this.Provider.OnReset(-2);

                await Task.Run(async () =>
                {
                    if (Provider is IAsyncResetProvider provider)
                    {
                        var count = await provider.GetCountAsync();
                        if (!cts.IsCancellationRequested)
                        {
                            VirtualizationManager.Instance.RunOnUi(() =>
                                Provider.OnReset(count));
                        }
                    }
                    else
                    {
                        var count = Provider.GetCount(false);
                        if (!cts.IsCancellationRequested)
                        {
                            VirtualizationManager.Instance.RunOnUi(() =>
                                Provider.OnReset(count));
                        }
                    }
                }, cts.Token);
            }
            else
            {
                if (ProviderAsync is IProviderPreReset)
                {
                    (ProviderAsync as IProviderPreReset).OnBeforeReset();
                }

                ProviderAsync.OnReset(await ProviderAsync.Count);
            }

            lock (this)
            {
                if (_resetToken == cts)
                {
                    _resetToken = null;
                }
            }
        }

        private T InternalGetValue(int index, string selectionContext)
        {
            if (Provider != null)
            {
                return Provider.GetAt(index, this);
            }

            return Task.Run(() => ProviderAsync.GetAt(index, this)).GetAwaiter().GetResult();
        }

        private T InternalSetValue(int index, T newValue)
        {
            var oldValue = InternalGetValue(index, DefaultSelectionContext);
            var edit = GetProviderAsEditable();
            edit.OnReplace(index, oldValue, newValue, null);

            var newItems = new List<T> {newValue};
            var oldItems = new List<T> {oldValue};

            if (IsSourceObservable) return oldValue;

            var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItems,
                oldItems, index);
            RaiseCollectionChangedEvent(args);

            return oldValue;
        }

        private int InternalAdd(T newValue, object timestamp)
        {
            var edit = GetProviderAsEditable();
            var index = edit.OnAppend(newValue, timestamp);
            OnCountTouched();

            if (IsSourceObservable) return index;

            var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newValue, index);
            RaiseCollectionChangedEvent(args);

            return index;
        }

        private int InternalGetCount()
        {
            var ret = 0;

            ret = Provider?.GetCount(true) ?? ProviderAsync.Count.GetAwaiter().GetResult();

            return ret;
        }

        private void InternalInsertAt(int index, T item, object timestamp = null)
        {
            var edit = GetProviderAsEditable();
            edit.OnInsert(index, item, timestamp);

            OnCountTouched();

            if (!IsSourceObservable)
            {
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index);
                RaiseCollectionChangedEvent(args);
            }
        }

        private bool InternalRemoveAt(int index, object timestamp = null)
        {
            if (!(Provider is IEditableProviderIndexBased<T> edit)) return false;

            var oldValue = edit.OnRemove(index, timestamp);

            OnCountTouched();

            if (IsSourceObservable) return true;

            var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldValue, index);
            RaiseCollectionChangedEvent(args);

            return true;
        }

        private bool InternalRemove(T item, object timestamp = null)
        {
            if (item == null)
            {
                return false;
            }

            if (!(Provider is IEditableProviderItemBased<T> edit))
                return false;
            var index = edit.OnRemove(item, timestamp);
            OnCountTouched();

            if (IsSourceObservable) return true;

            var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index);
            RaiseCollectionChangedEvent(args);

            return true;
        }

        private int InternalIndexOf(T item)
        {
            return Provider?.IndexOf(item) ??
                   Task.Run(() => ProviderAsync.IndexOf(item)).GetAwaiter().GetResult();
        }

        private void EnsureCountIsGotNonaSync()
        {
            Provider?.GetCount(false);
        }
    }
}