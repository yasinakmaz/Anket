using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.Core;

namespace Anket.Services
{
    public class FileSaverService : IFileSaver
    {
        public Task<FileSaverResult> SaveAsync(string initialPath, Stream stream, CancellationToken cancellationToken = default)
        {
            return FileSaver.Default.SaveAsync(initialPath, stream, cancellationToken);
        }

        public Task<FileSaverResult> SaveAsync(string fileName, string initialPath, Stream stream, CancellationToken cancellationToken = default)
        {
            return FileSaver.Default.SaveAsync(fileName, initialPath, stream, cancellationToken);
        }

        public Task<FileSaverResult> SaveAsync(string initialPath, Stream stream, IProgress<double> progress, CancellationToken cancellationToken = default)
        {
            return FileSaver.Default.SaveAsync(initialPath, stream, progress, cancellationToken);
        }

        public Task<FileSaverResult> SaveAsync(string fileName, string initialPath, Stream stream, IProgress<double> progress, CancellationToken cancellationToken = default)
        {
            return FileSaver.Default.SaveAsync(fileName, initialPath, stream, progress, cancellationToken);
        }
    }
} 