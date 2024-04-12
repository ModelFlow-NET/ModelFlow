namespace VitalElement.DataVirtualization.Pageing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Interfaces;

    public class PagedSourceProviderMakeAsync<T> : BasePagedSourceProvider<T>, IPagedSourceProviderAsync<T>,
        IProviderPreReset
    {
        public PagedSourceProviderMakeAsync()
        {
        }

        public PagedSourceProviderMakeAsync(
            Func<int, int, IEnumerable<T>> funcGetItemsAt = null,
            Func<int> funcGetCount = null,
            Func<T, Task<int>> funcIndexOfAsync = null,
            Func<T, bool> funcContains = null,
            Func<T, Task<bool>> funcContainsAsync = null,
            Action<int> actionOnReset = null,
            Func<int, int, int, T> funcGetPlaceHolder = null,
            Action actionOnBeforeReset = null
        ) : base(funcGetItemsAt, funcGetCount, null, funcContains, actionOnReset)
        {
            FuncGetPlaceHolder = funcGetPlaceHolder;
            ActionOnBeforeReset = actionOnBeforeReset;
            FuncIndexOfAsync = funcIndexOfAsync;
            FuncContainsAsync = funcContainsAsync;
        }

        public Action ActionOnBeforeReset { get; set; }
        public Func<T, Task<bool>> FuncContainsAsync { get; set; }

        public Func<int, int, int, T> FuncGetPlaceHolder { get; set; }

        public Func<T, Task<int>> FuncIndexOfAsync { get; set; }

        public void Replace(T old, T newItem)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ContainsAsync(T item)
        {
            return FuncContainsAsync?.Invoke(item) ?? default(Task<bool>);
        }

        public Task<int> GetCountAsync()
        {
            var tcs = new TaskCompletionSource<int>();

            try
            {
                tcs.SetResult(Count);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }

            return tcs.Task;
        }

        public Task<IEnumerable<T>> GetItemsAtAsync(int pageoffset, int count)
        {
            var tcs = new TaskCompletionSource<IEnumerable<T>>();

            try
            {
                tcs.SetResult(GetItemsAt(pageoffset, count));
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }

            return tcs.Task;
        }

        public virtual T GetPlaceHolder(int index, int page, int offset)
        {
            var ret = default(T);

            if (FuncGetPlaceHolder != null)
                ret = FuncGetPlaceHolder.Invoke(index, page, offset);

            return ret;
        }

        public Task<int> IndexOfAsync(T item)
        {
            return FuncIndexOfAsync?.Invoke(item) ?? default(Task<int>);
        }

        public virtual void OnBeforeReset()
        {
            ActionOnBeforeReset?.Invoke();
        }
    }
}