using AutoEncodeServer.Data;
using AutoEncodeServer.Models.Interfaces;

namespace AutoEncodeServer.Factories;

public interface ISourceFileModelFactory
{
    /// <summary>Creates an <see cref="ISourceFileModel"/> using the given <see cref="SourceFile"/></summary>
    /// <param name="sourceFile">Source file data.</param>
    /// <returns><see cref="ISourceFileModel"/></returns>
    ISourceFileModel Create(SourceFile sourceFile);

    void Release(ISourceFileModel sourceFileModel);
}
