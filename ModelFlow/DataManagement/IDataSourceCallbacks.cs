namespace ModelFlow.DataVirtualization.DataManagement;

using System;
using System.Threading.Tasks;

public interface IDataSourceCallbacks
{
    /// <summary>
    /// Called before a Create operation.
    /// Intended to allow showing of a confirmation dialog.
    /// </summary>
    /// <param name="viewmodel"></param>
    /// <returns>True if the operation can continue, false if it should be aborted.</returns>
    Task<bool> OnBeforeCreateOperation(object viewmodel);

    /// <summary>
    /// Called before an update operation.
    /// Intended to allow showing of a confirmation dialog.
    /// </summary>
    /// <param name="viewmodel"></param>
    /// <returns>True if the operation can continue, false if it should be aborted.</returns>
    Task<bool> OnBeforeUpdateOperation(object viewmodel);

    /// <summary>
    /// Called before a delete operation.
    /// Intended to allow showing of a confirmation dialog.
    /// </summary>
    /// <param name="viewmodel"></param>
    /// <returns>True if the operation can continue, false if it should be aborted.</returns>
    Task<bool> OnBeforeDeleteOperation(object viewmodel);
    
    Task OnCreateOperationCompleted(object viewmodel, bool success);

    Task OnUpdateOperationCompleted(object viewmodel, bool success);

    Task OnDeleteOperationCompleted(object viewmodel, bool success);
    
    Task OnCreateException(object viewmodel, Exception e);
    
    Task OnUpdateException(object viewmodel, Exception e);

    Task OnDeleteException(object viewmodel, Exception e);
}