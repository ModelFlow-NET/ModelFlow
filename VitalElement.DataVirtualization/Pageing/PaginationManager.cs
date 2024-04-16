namespace VitalElement.DataVirtualization.Pageing
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Actions;
    using DataManagement;
    using Interfaces;

    internal class PaginationManager<T> : IItemSourceProvider<T>, INotifyImmediately, IEditableProvider<T>,
        IEditableProviderIndexBased<T>, IEditableProviderItemBased<T>, IReclaimableService,
        IAsyncResetProvider, IProviderPreReset, INotifyCountChanged, INotifyCollectionChanged,
        ICollection where T : DataItem
    {
        private readonly Dictionary<int, PageDelta> _deltas = new Dictionary<int, PageDelta>();
        private readonly Dictionary<int, ISourcePage<T>> _pages = new Dictionary<int, ISourcePage<T>>();
        private readonly IPageReclaimer<T> _reclaimer;

        private readonly Dictionary<int, CancellationTokenSource> _tasks =
            new Dictionary<int, CancellationTokenSource>();

        protected object PageLock = new object();
        private int _basePage;
        private bool _hasGotCount;

        private int _localCount;
        private int _pageSize = 100;
        
        public PaginationManager(
            IPagedSourceProviderAsync<T> provider,
            IPageReclaimer<T> reclaimer = null,
            IPageExpiryComparer expiryComparer = null,
            int pageSize = 100,
            int maxPages = 100,
            int maxDeltas = -1,
            int maxDistance = -1,
            string sectionContext = "")
        {
            PageSize = pageSize;
            MaxPages = maxPages;
            MaxDeltas = maxDeltas;
            MaxDistance = maxDistance;

            ProviderAsync = provider;

            _reclaimer = reclaimer ?? new PageReclaimOnTouched<T>();

            ExpiryComparer = expiryComparer;

            VirtualizationManager.Instance.AddAction(new ReclaimPagesWA(this, sectionContext));
        }


        /// <summary>
        ///     Initializes a new instance of the <see cref="PaginationManager{T}" /> class.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="reclaimer">The reclaimer.</param>
        /// <param name="expiryComparer">The expiry comparer.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="maxPages">The maximum pages.</param>
        /// <param name="maxDeltas">The maximum deltas.</param>
        /// <param name="maxDistance">The maximum distance.</param>
        /// <param name="sectionContext">The section context.</param>
        public PaginationManager(
            IPagedSourceProvider<T> provider,
            IPageReclaimer<T> reclaimer = null,
            IPageExpiryComparer expiryComparer = null,
            int pageSize = 100,
            int maxPages = 100,
            int maxDeltas = -1,
            int maxDistance = -1,
            string sectionContext = "")
        {
            PageSize = pageSize;
            MaxPages = maxPages;
            MaxDeltas = maxDeltas;
            MaxDistance = maxDistance;
            if (provider is IPagedSourceProviderAsync<T> async)
            {
                ProviderAsync = async;
            }
            else
            {
                Provider = provider;
            }

            _reclaimer = reclaimer ?? new PageReclaimOnTouched<T>();

            ExpiryComparer = expiryComparer;

            VirtualizationManager.Instance.AddAction(new ReclaimPagesWA(this, sectionContext));
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PaginationManager{T}" /> class.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="reclaimer">The reclaimer.</param>
        /// <param name="expiryComparer">The expiry comparer.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="maxPages">The maximum pages.</param>
        /// <param name="maxDeltas">The maximum deltas.</param>
        /// <param name="maxDistance">The maximum distance.</param>
        /// <param name="sectionContext">The section context.</param>
        public PaginationManager(
            IPagedSourceObservableProvider<T> provider,
            IPageReclaimer<T> reclaimer = null,
            IPageExpiryComparer expiryComparer = null,
            int pageSize = 100,
            int maxPages = 100,
            int maxDeltas = -1,
            int maxDistance = -1,
            string sectionContext = "") : this(provider as IPagedSourceProvider<T>, reclaimer, expiryComparer, pageSize,
            maxPages, maxDeltas, maxDistance, sectionContext)
        {
            provider.CollectionChanged += OnProviderCollectionChanged;
        }

        public IPageExpiryComparer ExpiryComparer { get; set; }

        /// <summary>
        ///     Gets or sets the maximum deltas.
        /// </summary>
        /// <value>
        ///     The maximum deltas.
        /// </value>
        public int MaxDeltas { get; set; } = -1;

        /// <summary>
        ///     Gets or sets the maximum distance.
        /// </summary>
        /// <value>
        ///     The maximum distance.
        /// </value>
        public int MaxDistance { get; set; } = -1;

        /// <summary>
        ///     Gets or sets the maximum pages.
        /// </summary>
        /// <value>
        ///     The maximum pages.
        /// </value>
        public int MaxPages { get; set; } = 100;

        /// <summary>
        ///     Gets or sets the size of the page.
        /// </summary>
        /// <value>
        ///     The size of the page.
        /// </value>
        public int PageSize
        {
            get => _pageSize;
            set
            {
                DropAllDeltasAndPages();
                _pageSize = value;
            }
        }

        /// <summary>
        ///     Gets or sets the provider.
        /// </summary>
        /// <value>
        ///     The provider.
        /// </value>
        public IPagedSourceProvider<T> Provider { get; set; }

        /// <summary>
        ///     Gets or sets the provider asynchronous.
        /// </summary>
        /// <value>
        ///     The provider asynchronous.
        /// </value>
        public IPagedSourceProviderAsync<T> ProviderAsync { get; set; }

        public int StepToJumpThreashold { get; set; } = 10;

        private int AddNotificationsCount { get; set; }

        private int LocalCount
        {
            get => _localCount;
            set => _localCount = value;
        }

        public async Task<int> GetCountAsync()
        {
            _hasGotCount = true;
            if (!IsAsync)
            {
                return Provider.Count;
            }

            return await ProviderAsync.GetCountAsync();
        }


        /// <summary>
        ///     Resets the specified count.
        /// </summary>
        /// <param name="count">The count.</param>
        public void OnReset(int count)
        {
            CancelAllRequests();

            lock (PageLock)
            {
                DropAllDeltasAndPages();
            }

            if (count < 0)
            {
                _hasGotCount = false;
                LocalCount = 0;
            }
            else
            {
                //TODO <-lock (this.SyncRoot)
                lock (SyncRoot)
                {
                    LocalCount = count;
                    _hasGotCount = true;
                }
            }

            if (!IsAsync)
            {
                Provider.OnReset(count);
            }
            else
            {
                ProviderAsync.OnReset(count);
            }

            if (count >= -1)
            {
                RaiseCountChanged(true, count);
            }
        }


        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            // Attempt to get the item from the pages, else call  the provider to get it..
            lock (PageLock)
            {
                foreach (var p in _pages)
                {
                    var o = p.Value.IndexOf(item);
                    if (o >= 0)
                    {
                        return true;
                    }
                }
            }

            return !IsAsync
                ? Provider.Contains(item)
                : ProviderAsync.ContainsAsync(item).GetAwaiter().GetResult();
        }

        public T GetAt(int index, object voc)
        {
            return GetAt(index, voc, 10);
        }

        /// <summary>
        ///     Gets the count.
        /// </summary>
        /// <value>
        ///     The count.
        /// </value>
        public int GetCount(bool asyncOk)
        {
            if (_hasGotCount) return LocalCount;

            //TODO<-lock(this.SyncRoot)
            lock (this)
            {
                if (!IsAsync)
                {
                    LocalCount = Provider.Count;
                }
                else if (!asyncOk)
                {
                    LocalCount = ProviderAsync.GetCountAsync().GetAwaiter().GetResult();
                }
                else
                {
                    var cts = StartPageRequest(int.MinValue);
                    GetCountAsync(cts);
                }
            }

            _hasGotCount = true;
            return LocalCount;
        }

        /// <summary>
        ///     Gets the Index of item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>the index of the item, or -1 if not found</returns>
        public int IndexOf(T item)
        {
            // Attempt to get the item from the pages, else call  the provider to get it..
            lock (PageLock)
            {
                foreach (var p in _pages)
                {
                    var o = p.Value.IndexOf(item);
                    if (o >= 0)
                    {
                        return o + ((p.Key - _basePage) * PageSize) + (from d in _deltas.Values
                                   where d.Page < p.Key
                                   select d.Delta).Sum();
                    }
                }
            }

            if (!IsAsync)
            {
                return Provider.IndexOf(item);
            }
            else
            {
                var result = Task.Run(async () => await ProviderAsync.IndexOfAsync(item)).GetAwaiter().GetResult();

                return result;
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        ///     Occurs when [count changed].
        /// </summary>
        public event OnCountChanged CountChanged;

        public bool IsNotifyImmediately
        {
            get => Provider is INotifyImmediately iNotifyImmediatelyProvider &&
                   iNotifyImmediatelyProvider.IsNotifyImmediately;
            set
            {
                if (Provider is INotifyImmediately iNotifyImmediatelyProvider)
                {
                    iNotifyImmediatelyProvider.IsNotifyImmediately = value;
                }
            }
        }


        public void OnBeforeReset()
        {
            if (!IsAsync)
            {
                (Provider as IProviderPreReset)?.OnBeforeReset();
            }
            else
            {
                (ProviderAsync as IProviderPreReset)?.OnBeforeReset();
            }
        }

        public void RunClaim(string sectionContext = "")
        {
            if (_reclaimer == null) return;
            lock (PageLock)
            {
                var needed = Math.Max(0, _pages.Count - MaxPages);
                if (needed == 0) return;
                var reclaimedPages = _reclaimer.ReclaimPages(_pages.Values, needed, sectionContext).ToList();

                foreach (var reclaimedPage in reclaimedPages)
                {
                    if (reclaimedPage.Page == _basePage) continue;
                    lock (_pages)
                    {
                        if (!_pages.ContainsKey(reclaimedPage.Page)) continue;
                        _pages.Remove(reclaimedPage.Page);
                        _reclaimer.OnPageReleased(reclaimedPage);
                    }
                }
            }
        }

        /// <summary>
        ///     Adds the or update adjustment.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="offsetChange">The offset change.</param>
        public int AddOrUpdateAdjustment(int page, int offsetChange)
        {
            var ret = 0;

            lock (PageLock)
            {
                if (!_deltas.ContainsKey(page))
                {
                    if (MaxDeltas == -1 || _deltas.Count < MaxDeltas)
                    {
                        ret = offsetChange;
                        _deltas.Add(page, new PageDelta {Page = page, Delta = offsetChange});
                    }
                    else
                    {
                        DropAllDeltasAndPages();
                    }
                }
                else
                {
                    var adjustment = _deltas[page];
                    adjustment.Delta += offsetChange;

                    if (adjustment.Delta == 0)
                    {
                        _deltas.Remove(page);
                    }

                    ret = adjustment.Delta;
                }
            }

            return ret;
        }

        /// <summary>
        ///     Gets at.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="voc">The voc.</param>
        /// <returns></returns>
        public T GetAt(int index, object voc, int nullTryCount = 10)
        {
            var ret = default(T);

            CalculateFromIndex(index, out var page, out var offset);

            var datapage = SafeGetPage(page, voc, index);

            if (datapage != null)
            {
                ret = datapage.GetAt(offset);
            }

            if (ret == null)
            {
                //return this.ProviderAsync.GetPlaceHolder(0, 0,0);
                Debugger.Break();
                //TODO <-
                if (nullTryCount <= 0) //inconsistency, notify reset collection
                {
                    OnProviderCollectionChanged(Provider,
                        new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    return ret;
                }

                Thread.Sleep(100);
                return GetAt(index, voc, --nullTryCount);
            }

            return ret;
        }


        protected void CalculateFromIndex(int index, out int page, out int inneroffset)
        {
            // First work out the base page from the index and the offset inside that page
            var basepage = page = (index / PageSize) + _basePage;
            inneroffset = (index + (_basePage * PageSize)) - (page * PageSize);

            // We only need to do the rest if there have been modifications to the page sizes on pages (deltas)
            if (_deltas.Count <= 0) return;

            // Get the adjustment BEFORE checking for a short page, because we are going to adjust for that first..
            var adjustment = 0;

            lock (PageLock)
            {
                // First, get the total adjustments for any pages BEFORE the current page..
                adjustment = (from d in _deltas.Values
                    where d.Page < basepage
                    select d.Delta).Sum();
            }

            // Now check to see if we are currently in a short page - in which case we need to adjust for that
            if (_deltas.ContainsKey(page))
            {
                var delta = _deltas[page].Delta;

                if (delta < 0)
                {
                    // In a short page, are we over the edge ?
                    if (inneroffset >= PageSize + delta)
                    {
                        var step = inneroffset - (PageSize + delta - 1);
                        inneroffset -= step;
                        DoStepForward(ref page, ref inneroffset, step);
                    }
                }
            }

            // If we do have adjustments...
            if (adjustment == 0) return;

            if (adjustment > 0)
            {
                // items have been added to earlier pages, so we need to step back
                DoStepBackwards(ref page, ref inneroffset, adjustment);
            }
            else
            {
                // items have been removed from earlier pages, so we need to step forward
                DoStepForward(ref page, ref inneroffset, Math.Abs(adjustment));
            }
        }

        protected void CancelAllRequests()
        {
            lock (PageLock)
            {
                var cancellationTokenSources = _tasks.Values.ToList();
                foreach (var cancellationTokenSource in cancellationTokenSources)
                {
                    try
                    {
                        cancellationTokenSource.Cancel(false);
                    }
                    catch (Exception)
                    {
                        Debugger.Break();
                    }
                }

                _tasks.Clear();
            }
        }


        protected void CancelPageRequest(int page)
        {
            lock (PageLock)
            {
                if (!_tasks.ContainsKey(page))
                {
                    return;
                }

                try
                {
                    _tasks[page].Cancel();
                }
                catch (Exception)
                {
                    Debugger.Break();
                }

                try
                {
                    _tasks.Remove(page);
                }
                catch (Exception)
                {
                    Debugger.Break();
                }
            }
        }

        /// <summary>
        ///     Drops all deltas and pages.
        /// </summary>
        protected void DropAllDeltasAndPages()
        {
            lock (PageLock)
            {
                _deltas.Clear();
                _pages.Clear();
                _basePage = 0;
                CancelAllRequests();
            }
        }


        /// <summary>
        ///     Gets the provider as editable.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException"></exception>
        protected IEditableProvider<T> GetProviderAsEditable()
        {
            if (Provider != null)
            {
                return Provider as IEditableProvider<T>;
            }

            return ProviderAsync as IEditableProvider<T>;
        }


        /// <summary>
        ///     Raises the count changed.
        /// </summary>
        /// <param name="count">The count.</param>
        protected void RaiseCountChanged(bool needsReset, int count)
        {
            //TODO<-this._hasGotCount = false;
            var evnt = CountChanged;
            evnt?.Invoke(this, new CountChangedEventArgs
            {
                NeedsReset = needsReset,
                Count = count
            });
        }

        protected void RemovePageRequest(int page)
        {
            lock (PageLock)
            {
                if (!_tasks.ContainsKey(page)) return;
                try
                {
                    _tasks.Remove(page);
                }
                catch (Exception)
                {
                    Debugger.Break();
                }
            }
        }

        protected CancellationTokenSource StartPageRequest(int page)
        {
            var cts = new CancellationTokenSource();

            CancelPageRequest(page);

            lock (PageLock)
            {
                if (!_tasks.ContainsKey(page))
                {
                    _tasks.Add(page, cts);
                }
                else
                {
                    _tasks[page] = cts;
                }
            }

            return cts;
        }


        private void DoStepBackwards(ref int page, ref int offset, int stepAmount)
        {
            var done = false;
            var ignoreSteps = -1;
            //TODO <-lock (this.PageLock)
            //{
            while (!done)
            {
                if (stepAmount > PageSize * StepToJumpThreashold && ignoreSteps <= 0)
                {
                    var targetPage = page - stepAmount / PageSize;
                    var sourcePage = page;
                    var adj = (from a in _deltas.Values
                        where a.Page >= targetPage && a.Page <= sourcePage
                        orderby a.Page
                        select a).ToArray();
                    if (!adj.Any())
                    {
                        page = targetPage;
                        stepAmount -= (sourcePage - targetPage) * PageSize;

                        if (stepAmount == 0)
                        {
                            done = true;
                        }
                    }
                    else if (adj.Last().Page < page - 2)
                    {
                        targetPage = adj.Last().Page + 1;
                        page = targetPage;
                        stepAmount -= (sourcePage - targetPage) * PageSize;

                        if (stepAmount == 0)
                        {
                            done = true;
                        }
                    }
                    else
                    {
                        ignoreSteps = sourcePage - adj.Last().Page;
                    }
                }

                if (done) continue;

                if (offset - stepAmount < 0)
                {
                    stepAmount -= (offset + 1);
                    page--;
                    var items = PageSize;
                    if (_deltas.ContainsKey(page))
                    {
                        items += _deltas[page].Delta;
                    }

                    offset = items - 1;
                }
                else
                {
                    offset -= stepAmount;
                    done = true;
                }

                ignoreSteps--;
            }

            // }
        }

        private void DoStepForward(ref int page, ref int offset, int stepAmount)
        {
            var done = false;
            var ignoreSteps = -1;
            //TODO <-lock (this.PageLock)
            //{
            while (!done)
            {
                if (stepAmount > PageSize * StepToJumpThreashold && ignoreSteps <= 0)
                {
                    var targetPage = page + stepAmount / PageSize;
                    var sourcePage = page;
                    var adj = (from a in _deltas.Values
                        where a.Page <= targetPage && a.Page >= sourcePage
                        orderby a.Page
                        select a).ToArray();
                    if (!adj.Any())
                    {
                        page = targetPage;
                        stepAmount -= (targetPage - sourcePage) * PageSize;

                        if (stepAmount == 0)
                        {
                            done = true;
                        }
                    }
                    else if (adj.Last().Page > page - 2)
                    {
                        targetPage = adj.Last().Page - 1;
                        page = targetPage;
                        stepAmount -= (targetPage - sourcePage) * PageSize;

                        if (stepAmount == 0)
                        {
                            done = true;
                        }
                    }
                    else
                    {
                        ignoreSteps = adj.Last().Page - sourcePage;
                    }
                }

                if (done) continue;

                var items = PageSize;
                if (_deltas.ContainsKey(page))
                {
                    items += _deltas[page].Delta;
                }

                if (items <= offset + stepAmount)
                {
                    stepAmount -= (items) - offset;
                    offset = 0;
                    page++;
                }
                else
                {
                    offset += stepAmount;
                    done = true;
                }

                ignoreSteps--;
            }

            //}
        }

        /// <summary>
        ///     Fills the page.
        /// </summary>
        /// <param name="newPage">The new page.</param>
        /// <param name="pageOffset">The page offset.</param>
        private void FillPage(ISourcePage<T> newPage, int pageOffset)
        {;
            var data = new PagedSourceItemsPacket<T>(Provider.GetItemsAt(pageOffset, newPage.ItemsPerPage));
            newPage.WiredDateTime = data.LoadedAt;
            foreach (var o in data.Items)
            {
                newPage.Append(o, null, ExpiryComparer);
            }

            newPage.PageFetchState = PageFetchStateEnum.Fetched;
        }

        /// <summary>
        ///     Fills the page from asynchronous provider.
        /// </summary>
        /// <param name="newPage">The new page.</param>
        /// <param name="pageOffset">The page offset.</param>
        /*private void FillPageFromAsyncProvider(ISourcePage<T> newPage, int pageOffset)
        {
            var data = ProviderAsync.GetItemsAt(pageOffset, newPage.ItemsPerPage, false);
            newPage.WiredDateTime = data.LoadedAt;
            foreach (var o in data.Items)
            {
                newPage.Append(o, null, ExpiryComparer);
            }

            newPage.PageFetchState = PageFetchStateEnum.Fetched;
        }*/

        private async void GetCountAsync(CancellationTokenSource cts)
        {
            if (!cts.IsCancellationRequested)
            {
                var ret = await ProviderAsync.GetCountAsync();

                if (!cts.IsCancellationRequested)
                {
                    //TODO<-lock (this.SyncRoot)
                    lock (this)
                    {
                        _hasGotCount = true;
                        LocalCount = ret;
                    }
                }

                if (!cts.IsCancellationRequested)
                {
                    RaiseCountChanged(true, LocalCount);
                }
            }

            RemovePageRequest(int.MinValue);
        }


        private void OnProviderCollectionChanged(object sender,
            NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            //lock(this._addLock)
            //{
            switch (notifyCollectionChangedEventArgs.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in notifyCollectionChangedEventArgs.NewItems)
                    {
                        if (!(item is T newItem)) continue;

                        AddNotificationsCount++;
                        OnAppend(newItem, DateTime.Now, true, true);
                    }

                    CollectionChanged?.Invoke(sender,
                        notifyCollectionChangedEventArgs); // check if this.OnAppend does not raise collection change as well
                    //this.RaiseCountChanged(true, this._localCount);
                    break;
                case NotifyCollectionChangedAction.Reset:
                case NotifyCollectionChangedAction.Remove: //TODO
                    lock (PageLock)
                    {
                        _hasGotCount = false;
                        CancelAllRequests();
                        DropAllDeltasAndPages();
                    }

                    CollectionChanged?.Invoke(sender, notifyCollectionChangedEventArgs);
                    break;

                case NotifyCollectionChangedAction.Replace: //TODO
                case NotifyCollectionChangedAction.Move: //TODO
                default:
                    break;
            }

            //}
        }


        /// <summary>
        ///     Called when [append].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="isAlreadyInSourceCollection"></param>
        /// <param name="createPageIfNotExist"></param>
        /// <returns></returns>
        public int OnAppend(T item, object timestamp, bool isAlreadyInSourceCollection = false,
            bool createPageIfNotExist = false)
        {
            var index = LocalCount;

            if (!_hasGotCount)
            {
                lock (SyncRoot)
                {
                    EnsureCount();
                    if (isAlreadyInSourceCollection)
                    {
                        Interlocked.Decrement(ref _localCount);
                    }
                }
            }

            CalculateFromIndex(index, out var page, out _);

            if (IsPageWired(page))
            {
                var shortpage = false;
                var dataPage = SafeGetPage(page, null, index);
                if (dataPage.ItemsPerPage < PageSize)
                {
                    shortpage = true;
                }

                dataPage.Append(item, timestamp, ExpiryComparer);

                if (shortpage)
                {
                    dataPage.ItemsPerPage++;
                }
                else
                {
                    AddOrUpdateAdjustment(page, 1);
                }
            }
            else if (createPageIfNotExist)
            {
                var dataPage = CreateNewPage(page, out _, out _);
                dataPage.Append(item, timestamp, ExpiryComparer);
            }

            Interlocked.Increment(ref _localCount);

            var edit = GetProviderAsEditable();
            if (edit != null && !isAlreadyInSourceCollection)
            {
                //==>edit.OnInsert(index, item, timestamp);
                //TODO<-edit.OnAppend(item, timestamp);
                edit.OnAppend(item, timestamp);
            }
            else if (!isAlreadyInSourceCollection)
            {
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index);
                CollectionChanged?.Invoke(this, args);
            }

            return index;
        }

        /// <summary>
        ///     Gets the page, if use placeholders is false - then gets page sync else async.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="voc">The voc.</param>
        /// <param name="index">The index that this page refers to (effectively the pageoffset.</param>
        /// <returns></returns>
        protected ISourcePage<T> SafeGetPage(int page, object voc, int index)
        {
            ISourcePage<T> ret = null;

            lock (PageLock)
            {
                if (_pages.ContainsKey(page))
                {
                    ret = _pages[page];
                    _reclaimer.OnPageTouched(ret);
                }
                else
                {
                    var newPage = CreateNewPage(page, out var pageSize, out var pageOffset);

                    if (!IsAsync)
                    {
                        FillPage(newPage, pageOffset);

                        ret = newPage;
                    }
                    else
                    {
                        if (voc != null)
                        {
                            // Fill with placeholders
                            for (var loop = 0; loop < pageSize; loop++)
                            {
                                var placeHolder = ProviderAsync.GetPlaceHolder(newPage.Page * pageSize + loop,
                                    newPage.Page, loop);
                                newPage.Append(placeHolder, null, ExpiryComparer);
                            }

                            ret = newPage;

                            var cts = StartPageRequest(newPage.Page);
                            Task.Run(() => DoRealPageGet(voc, newPage, pageOffset, index, cts), cts.Token)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            //FillPageFromAsyncProvider(newPage, pageOffset);
                            ret = newPage;
                        }
                    }
                }
            }

            return ret;
        }

        private ISourcePage<T> CreateNewPage(int page, out int pageSize, out int pageOffset)
        {
            PageDelta delta = null;
            if (_deltas.ContainsKey(page))
            {
                delta = _deltas[page];
            }

            pageOffset = (page - _basePage) * PageSize + (from d in _deltas.Values
                             where d.Page < page
                             select d.Delta).Sum();
            pageSize = Math.Min(this.PageSize, this.GetCount(false) - pageOffset);
            if (delta != null)
            {
                pageSize += delta.Delta;
            }

            var newPage = _reclaimer.MakePage(page, pageSize);
            _pages.Add(page, newPage);
            return newPage;
        }

        private async Task DoRealPageGet(object voc, ISourcePage<T> page, int pageOffset, int index,
            CancellationTokenSource cts)
        {
            var realVoc = (VirtualizingObservableCollection<T>) voc;
            var listOfReplaces = new List<PlaceholderReplaceWA>();

            if (realVoc != null)
            {
                if (cts.IsCancellationRequested)
                {
                    return;
                }

                var data = new PagedSourceItemsPacket<T>(await ProviderAsync.GetItemsAtAsync(pageOffset, page.ItemsPerPage));

                if (cts.IsCancellationRequested)
                {
                    return;
                }

                page.WiredDateTime = data.LoadedAt;

                var i = 0;
                foreach (var item in data.Items)
                {
                    if (cts.IsCancellationRequested)
                    {
                        RemovePageRequest(page.Page);
                        return;
                    }

                    if (page.ReplaceNeeded(i))
                    {
                        var oldItem = page.ReplaceAt(i, item, null, null);
                        listOfReplaces.Add(new PlaceholderReplaceWA(oldItem, item));
                    }
                    else
                    {
                        page.ReplaceAt(i, item, null, null);
                    }

                    i++;
                }
            }

            page.PageFetchState = PageFetchStateEnum.Fetched;

            VirtualizationManager.Instance.RunOnUi(() =>
            {
                if (cts.IsCancellationRequested)
                {
                    RemovePageRequest(page.Page);
                    return;
                }
                
                foreach (var replace in listOfReplaces)
                {
                    replace.Execute();
                }
            });

            RemovePageRequest(page.Page);
        }

        protected bool IsPageWired(int page)
        {
            var wired = false;

            lock (PageLock)
            {
                if (_pages.ContainsKey(page))
                {
                    wired = true;
                }
            }

            return wired;
        }

        public int OnAppend(T item, object timestamp) => OnAppend(item, timestamp, false, false);

        public void OnInsert(int index, T item, object timestamp)
        {
            if (!_hasGotCount)
            {
                EnsureCount();
            }

            CalculateFromIndex(index, out var page, out var offset);

            if (IsPageWired(page))
            {
                var dataPage = SafeGetPage(page, null, index);
                dataPage.InsertAt(offset, item, timestamp, ExpiryComparer);
            }

            var adj = AddOrUpdateAdjustment(page, 1);

            if (page == _basePage && adj == PageSize * 2)
            {
                lock (PageLock)
                {
                    if (IsPageWired(page))
                    {
                        var dataPage = SafeGetPage(page, null, index);
                        ISourcePage<T> newdataPage = null;
                        if (IsPageWired(page - 1))
                        {
                            newdataPage = SafeGetPage(page - 1, null, index);
                        }
                        else
                        {
                            newdataPage = _reclaimer.MakePage(page - 1, PageSize);
                            _pages.Add(page - 1, newdataPage);
                        }

                        for (var loop = 0; loop < PageSize; loop++)
                        {
                            var i = dataPage.GetAt(0);

                            dataPage.RemoveAt(0, null, null);
                            newdataPage.Append(i, null, null);
                        }
                    }

                    AddOrUpdateAdjustment(page, -PageSize);

                    _basePage--;
                }
            }

            var edit = GetProviderAsEditable();
            if (edit != null)
            {
                edit.OnInsert(index, item, timestamp);
            }
            else
            {
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index);
                CollectionChanged?.Invoke(this, args);
            }

            Interlocked.Increment(ref _localCount);
        }

        public void OnReplace(int index, T oldItem, T newItem, object timestamp)
        {
            CalculateFromIndex(index, out var page, out var offset);

            if (IsPageWired(page))
            {
                var dataPage = SafeGetPage(page, null, index);
                dataPage.ReplaceAt(offset, newItem, timestamp, ExpiryComparer);
            }
            else
            {
                oldItem = new PagedSourceItemsPacket<T>(Provider.GetItemsAt(index, 1)).Items.FirstOrDefault();
                if (oldItem != default(T)) Debugger.Break();
            }

            if (Provider is IEditableProvider<T> editableProvider)
            {
                editableProvider.OnReplace(index, oldItem, newItem, timestamp);
            }
            else
            {
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItem, oldItem,
                    index);
                CollectionChanged?.Invoke(this, args);
            }
        }

        private void EnsureCount()
        {
            GetCount(false);
        }

        protected bool IsAsync => ProviderAsync != null ? true : false;

        /// <inheritdoc />
        /// <summary>
        ///     Copies the elements of the <see cref="T:System.Collections.ICollection" /> to an <see cref="T:System.Array" />,
        ///     starting at a particular <see cref="T:System.Array" /> index.
        /// </summary>
        /// <param name="array">
        ///     The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied
        ///     from <see cref="T:System.Collections.ICollection" />. The <see cref="T:System.Array" /> must have zero-based
        ///     indexing.
        /// </param>
        /// <param name="index">The zero-based index in <paramref name="array" /> at which copying begins. </param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="array" /> is null. </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index" /> is less than zero. </exception>
        /// <exception cref="T:System.ArgumentException">
        ///     <paramref name="array" /> is multidimensional.-or- The number of elements
        ///     in the source <see cref="T:System.Collections.ICollection" /> is greater than the available space from
        ///     <paramref name="index" /> to the end of the destination <paramref name="array" />.-or-The type of the source
        ///     <see cref="T:System.Collections.ICollection" /> cannot be cast automatically to the type of the destination
        ///     <paramref name="array" />.
        /// </exception>
        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        /// <summary>
        ///     Gets the number of elements contained in the <see cref="T:System.Collections.ICollection" />.
        /// </summary>
        /// <returns>
        ///     The number of elements contained in the <see cref="T:System.Collections.ICollection" />.
        /// </returns>
        public int Count => GetCount(false);

        /// <inheritdoc cref="" />
        /// <summary>
        ///     Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />.
        /// </summary>
        /// <returns>
        ///     An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />.
        /// </returns>
        public object SyncRoot => Provider?.SyncRoot ?? ProviderAsync.SyncRoot;
        //public object SyncRoot => this.PageLock ?? this.ProviderAsync.SyncRoot;

        /// <summary>
        ///     Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection" /> is synchronized
        ///     (thread safe).
        /// </summary>
        /// <returns>
        ///     true if access to the <see cref="T:System.Collections.ICollection" /> is synchronized (thread safe); otherwise,
        ///     false.
        /// </returns>
        public bool IsSynchronized => Provider.IsSynchronized;

        public T OnRemove(int index, object timestamp)
        {
            T item;

            if (!_hasGotCount)
            {
                EnsureCount();
            }

            CalculateFromIndex(index, out var page, out var offset);

            if (IsPageWired(page))
            {
                var dataPage = SafeGetPage(page, null, index);
                dataPage.RemoveAt(offset, timestamp, ExpiryComparer);
            }

            AddOrUpdateAdjustment(page, -1);

            if (page == _basePage)
            {
                var items = PageSize;
                if (_deltas.ContainsKey(page))
                {
                    items += _deltas[page].Delta;
                }

                if (items == 0)
                {
                    _deltas.Remove(page);
                    _basePage++;
                }
            }

            if (Provider is IEditableProviderIndexBased<T> editableProvider)
            {
                item = editableProvider.OnRemove(index, timestamp);
            }
            else
            {
                item = GetAt(index, Provider);
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index);
                CollectionChanged?.Invoke(this, args);
            }

            Interlocked.Decrement(ref _localCount);

            return item;
        }

        public T OnReplace(int index, T newItem, object timestamp)
        {
            T? oldItem;

            CalculateFromIndex(index, out var page, out var offset);

            if (IsPageWired(page))
            {
                var dataPage = SafeGetPage(page, null, index);
                oldItem = dataPage.ReplaceAt(offset, newItem, timestamp, ExpiryComparer);
            }
            else
            {
                oldItem = new PagedSourceItemsPacket<T>(Provider.GetItemsAt(index, 1)).Items.FirstOrDefault();
                if (oldItem != default(T)) Debugger.Break();
            }

            if (Provider is IEditableProviderIndexBased<T> editableProvider)
            {
                editableProvider.OnReplace(index, newItem, timestamp);
            }
            else
            {
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItem, oldItem,
                    index);
                CollectionChanged?.Invoke(this, args);
            }

            return oldItem;
        }

        public int OnRemove(T item, object timestamp)
        {
            if (!_hasGotCount)
            {
                EnsureCount();
            }

            int index = -1;
            lock (PageLock)
            {
                for (int i = 0; i < _pages.Count; i++)
                {
                    var dataPage = _pages[i];

                    index = dataPage.IndexOf(item);

                    if (index != -1)
                    {
                        dataPage.RemoveAt(index, DateTime.Now, ExpiryComparer);

                        AddOrUpdateAdjustment(i, -1);

                        if (i == _basePage)
                        {
                            var items = PageSize;
                            if (_deltas.ContainsKey(i))
                            {
                                items += _deltas[i].Delta;
                            }

                            if (items == 0)
                            {
                                _deltas.Remove(i);
                                _basePage++;
                            }
                        }

                        break;
                    }
                }
            }

            if (Provider is IEditableProviderItemBased<T> editableProvider)
            {
                editableProvider.OnRemove(item, timestamp);
            }
            else
            {
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index);
                CollectionChanged?.Invoke(this, args);
            }

            Interlocked.Decrement(ref _localCount);

            return index;
        }

        public int OnReplace(T oldItem, T newItem, object timestamp)
        {
            var index = Provider.IndexOf(oldItem);

            CalculateFromIndex(index, out var page, out var offset);

            if (IsPageWired(page))
            {
                var dataPage = SafeGetPage(page, null, index);
                dataPage.ReplaceAt(offset, newItem, timestamp, ExpiryComparer);
            }


            if (Provider is IEditableProviderItemBased<T> editableProvider)
            {
                editableProvider.OnReplace(oldItem, newItem, timestamp);
            }
            else
            {
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItem, oldItem,
                    index);
                CollectionChanged?.Invoke(this, args);
            }

            return index;
        }
    }
}