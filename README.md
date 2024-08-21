ModelFlow.NET is a .NET library for virtualizing models and viewmodel from a datasource, i.e. a database, webapi, or any data source.

ModelFlow.NET provides the following features:

- Asynchronous Data virtualization or paging
  Data is asynchronously requested while your UI is immediately supplied with a page of data to display to the user.
  Data is loaded in pages and when the async request completes your models will automatically update providing a seamless exprience to the user.
- Filtering and sorting of Data
  Data sources use IQueryable to defer sorting and filtering of data in realtime, to your datasource or database, instead of loading
  all data and then filtering and sorting in memory. Which is not scalable with large data sets.
- Selection of Data
  Most data virtualization solutions struggle with selection, when placeholders are replaced with the loaded data selection often
  is reset causing bad UX. ModelFlow.NET solves this by updating item wrappers that are almost transparent to the user.
- Adding, Removing and Updating Data
  ModelFlow handles when items are inserted and removed from a datasource, ensuring the UI is kept in sync.
- Large data sets
  ModelFlow can handle billions of items to be displayed in a UI, without performance issues. You are in control of how many records
  are paged and how many pages can be cached, controlling memory.

ModelFlow.NET is a .NET Standard 2.0 library and can be used in any .NET application, including WPF, UWP, WinUI and Avalonia


Documentation and examples are coming soon!
