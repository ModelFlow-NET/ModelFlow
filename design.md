# ModelFlow Data Virtualization Design

## Overview
ModelFlow provides a reusable data-virtualization layer intended to back UI controls (such as grids or list views) that must render large logical datasets without materializing every record. The core focus areas are:

- **Lazy materialization of data** backed by pageable providers.
- **Thread-aware work scheduling** so background fetches can marshal results back to the UI.
- **Observable collections** that surface virtualized items through WPF/WinUI-friendly APIs.
- **Extensible data sources** that encapsulate filtering, sorting, CRUD, and placeholder materialization.

The architecture is split across cooperating components that manage UI binding, paging, provider orchestration, and background maintenance.

---

## Key Components

| Component | Responsibility |
| --- | --- |
| `VirtualizingObservableCollection<T>` | Virtualized collection exposed to UI bindings. Serves placeholder items, raises `INotifyCollectionChanged`, and delegates item retrieval to a pagination manager. |
| `PaginationManager<T>` | Central paging engine. Tracks page cache, outstanding requests, deltas, and coordinates synchronous vs. asynchronous providers. Reclaims pages via scheduled actions. |
| `VirtualizationManager` | Singleton scheduler and coordination hub. Queues `IVirtualizationAction` instances and executes them on the UI thread or background threads. |
| `DataSource<TViewModel, TModel>` | High-level abstraction for application data. Wraps a `VirtualizingObservableCollection<DataItem<TViewModel>>`, provides filtering, sorting, CRUD hooks, and placeholder generation. |
| `DataItem<T>` | Lightweight wrapper that tracks loading state and exposes a mutable view-model reference. Supports placeholder semantics. |
| `Actions/*` | Background work items implementing reclamation and reset behaviors, registered with `VirtualizationManager`. |
| `Interfaces/*` | Contracts that define provider behaviors (paged, observable, async, reclaimable) plus notifications (`INotifyImmediately`, `INotifyCountChanged`, etc.). |
| `Pageing/*` helpers | Expiry comparers, reset providers, page abstractions, and base implementations for synchronous providers. |

---

## Data Flow

1. **Consumer Binding** – A UI layer binds to `DataSource.Collection`, an `IReadOnlyObservableCollection<DataItem<TViewModel>>` backed by `VirtualizingObservableCollection<DataItem<TViewModel>>`.
2. **Item Access** – When UI requests an index, the collection defers to `PaginationManager<T>` via internal `GetAt`/`GetCount` methods.
3. **Page Resolution** – `PaginationManager<T>` determines the page containing the requested index. If absent, it initiates a page fetch using `IPagedSourceProvider{Async}` implementations defined by the datasource. Placeholders are returned immediately while the async fetch runs.
4. **Provider Fetch** – The configured provider (`DataSource` subclass or custom `IPagedSourceProvider`) retrieves just the requested slice and returns `DataItem<T>` instances.
5. **Materialization** – When data arrives, the `VirtualizingObservableCollection` swaps placeholder entries with real data and raises appropriate collection and property changed notifications.
6. **Maintenance** – Background actions (e.g., `ReclaimPagesWA`) periodically free stale pages to respect cache limits. `VirtualizationManager.ProcessActions` orchestrates these on the correct threads.

---

## Pagination & Caching Strategy

`PaginationManager<T>` is the system’s caching coordinator:

- **Page Dictionary** – Maintains `ISourcePage<T>` instances keyed by page number, storing items and metadata (touch timestamps, size).
- **Delta Tracking** – Records `PageDelta` adjustments to reconcile page indexes after inserts/removes while caching remains in memory.
- **Async Awareness** – Handles both synchronous and asynchronous providers. For async providers it issues cancellation tokens per page request and exposes `GetCountAsync` for non-blocking counts.
- **Reclamation** – Integrates with `IPageReclaimer<T>` strategies (default `PageReclaimOnTouched<T>`) to discard pages when `MaxPages`, `MaxDistance`, or `MaxDeltas` limits are exceeded.
- **Reset Hooks** – Implements `IAsyncResetProvider` and `IProviderPreReset` so hosts can force refreshes and prepare for resets.

The manager exposes editing operations (`IEditableProvider*` interfaces) so writes can be staged locally while deltas are reconciled against the provider.

---

## Observable Collection Behavior

`VirtualizingObservableCollection<T>` implements the full set of collection interfaces so existing WPF/Avalonia controls can bind without modification:

- **Enumeration** – The custom enumerator triggers count checks and loads items on demand. Placeholders keep iteration responsive even if data is pending.
- **Index Access** – `InternalGetValue`, `InternalInsertAt`, `InternalRemoveAt`, etc. delegate to `PaginationManager`. Page-level timestamp checks allow stale updates to be ignored.
- **Change Notifications** – Uses `INotifyCollectionChanged` to raise Adds/Inserts/Removes only when the UI can reflect them. For observable providers (`IPagedSourceObservableProvider`), `CollectionChanged` events flow from the provider through the pagination layer into the collection.
- **Thread Marshalling** – Relies on `VirtualizationManager.Instance.UiThreadExcecuteAction` to ensure notifications land on the UI thread.
- **Count Semantics** – `EnsureCountIsGotNonaSync` and related helpers allow optimistic counts that may be recalculated asynchronously to avoid blocking the UI.

---

## DataSource Abstraction

`DataSource<TViewModel, TModel>` is a façade that owns the virtualizing collection and defines repository-like operations:

- **Filtering & Sorting** – Consumers set `Func<IQueryable<TModel>, IQueryable<TModel>>` filters and update `SortDescriptionList`. These invalidations clear the collection, forcing subsequent materialization to honor new queries.
- **CRUD Lifecycle** – Provides `CreateAsync`, `UpdateAsync`, and `DeleteAsync` workflows that interact with optional `IDataSourceCallbacks`. Successful mutations update the collection via the editable provider interfaces.
- **Placeholders & Selection** – Implementors supply placeholder instances and optional model lookup logic (`GetModelForViewModel`, `ModelsEqual`) so selection/highlighting can operate without fully realizing the item.
- **Initialization** – Tracks `IsInitialised` and `IsActive` to let UI know when data is ready or when the datasource is being used.

This layer is the primary extension point for application-specific behavior such as EF Core queries, remote API calls, or domain validation.

---

## Threading Model

- **UI Thread Dispatch** – `VirtualizationManager.UiThreadExcecuteAction` must be set by the host (typically to `Dispatcher.InvokeAsync`). Many operations assume this is configured before any collection is constructed; failure to do so throws at runtime.
- **Background Fetches** – `PaginationManager` issues background tasks for page fetches, using cancellation tokens per request to avoid stale work.
- **Synchronization Primitives** – Several code paths lock on `PageLock` or intrinsic locks to protect dictionaries. `AutoResetEvent` is used to coordinate filter operations.
- **Reactive Integration** – Optional `IScheduler UiThreadScheduler` enables RX-friendly throttled property sync (`PropertySyncThrottleTime`).

---

## Extensibility Points

- Custom `IPagedSourceProvider{Async}` implementations (e.g., remote API, SQL provider).
- Swap in different `IPageReclaimer<T>` strategies for cache eviction policies.
- Extend `DataSource` to surface domain-specific conditions, additional commands, or telemetry.
- Plug additional `IVirtualizationAction` items into `VirtualizationManager` for maintenance tasks like prefetch or warm-up.
- Override `PaginationManager.StepToJumpThreashold` and similar knobs to tune jump behavior for very large datasets.

---

## Observations & Potential Issues

1. **UI Thread Dependency** – Construction of `VirtualizingObservableCollection` throws if the host hasn’t set `UiThreadExcecuteAction`. Consider delaying validation until first UI-bound operation to make unit testing easier.
2. **Synchronous Waits** – Some async provider paths call `.GetAwaiter().GetResult()` (e.g., `PaginationManager.Contains`, `IndexOf`). On UI threads this risks deadlocks or responsiveness drops. Introducing truly async flows or `ConfigureAwait(false)` patterns would improve resiliency.
3. **Enumerator Overhead** – `VirtualizingObservableCollectionEnumerator` generates a new `Guid` per `MoveNext`, which appears accidental and adds allocation churn.
4. **Exception Swallowing** – Broad `catch (Exception)` blocks (e.g., enumeration, CRUD operations) can hide bugs. Logging hooks or propagation strategies would aid diagnostics.
5. **Thread Safety Gaps** – Locking is uneven; some dictionaries are accessed without locks (`_actions` list copy in `VirtualizationManager.ProcessActions`). Concurrent modifications could still race if actions mutate the list while iterating.
6. **Cancellation Leaks** – Page-level `CancellationTokenSource` instances are stored but not always disposed after completion. Ensuring tokens are cleaned up prevents resource leaks during long sessions.
7. **Observable Provider Coupling** – When using `IPagedSourceObservableProvider`, the pagination manager subscribes to `CollectionChanged` but there’s no explicit unsubscribe on disposal, risking memory retention.
8. **Testing & Tooling** – The solution includes tests under `ModelFlow.Tests`, but coverage for async paging edge cases appears sparse. Additional load and stress tests would help evaluate high-volume scenarios for the planned high-performance grid.

---

## Recommendations for High-Performance Grid Integration

- **Prefetch Strategies** – Implement predictive `IVirtualizationAction` tasks to pre-load pages around the user’s viewport. The existing action framework can host these.
- **Async-First API Surface** – Expose asynchronous enumeration/indexer methods so grid UI can await page materialization without blocking.
- **Instrumentation** – Add optional telemetry (fetch durations, cache hit/miss ratios) to guide tuning when embedding in the new grid control.
- **Placeholder Rendering** – Ensure grid templates can react to `DataItem.IsLoading` to display shimmer/skeleton states while data is pending.
- **Disposal Hooks** – Provide a way for hosts to dispose or reset datasources, unsubscribing handlers and cancelling outstanding requests when a view is torn down.
- **Thread Affinity Abstraction** – Encapsulate UI thread dispatch behind an interface so different UI frameworks (WPF, Avalonia, WinUI) can plug in dispatchers while maintaining testability.

---

## Next Steps

1. Address the synchronous wait patterns to make the pipeline fully asynchronous.
2. Audit cancellation token lifetime management.
3. Add comprehensive documentation around extension points and threading expectations.
4. Build performance benchmarks that exercise rapid scrolling, large result sets, and mutation-heavy workloads to validate suitability for the new datagrid control.
