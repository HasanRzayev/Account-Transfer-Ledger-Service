using System.Collections.Concurrent;

namespace AccountTransferLedgerService.Infrastructure.Concurrency;

/// <summary>
/// Hesab ID-lərinə görə ardıcıl və təhlükəsiz asinxron kilidləmə mexanizmi.
/// Eyni hesaba göndərilən paralel sorğuları nizamlayır və tətbiq daxilində race condition-un qarşısını alır.
/// </summary>
public interface IKeyedAsyncLock
{
    Task<IDisposable> LockAsync(Guid firstKey, Guid secondKey, CancellationToken cancellationToken = default);
}

public class KeyedAsyncLock : IKeyedAsyncLock
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public async Task<IDisposable> LockAsync(Guid firstKey, Guid secondKey, CancellationToken cancellationToken = default)
    {
        // Deadlock-ların qarşısını almaq üçün kilidləri həmişə deterministik ardıcıllıqla əldə edirik
        var k1 = firstKey.CompareTo(secondKey) < 0 ? firstKey : secondKey;
        var k2 = firstKey.CompareTo(secondKey) < 0 ? secondKey : firstKey;

        var sem1 = _locks.GetOrAdd(k1, _ => new SemaphoreSlim(1, 1));
        var sem2 = _locks.GetOrAdd(k2, _ => new SemaphoreSlim(1, 1));

        await sem1.WaitAsync(cancellationToken);
        try
        {
            await sem2.WaitAsync(cancellationToken);
            return new Releaser(sem1, sem2);
        }
        catch
        {
            sem1.Release();
            throw;
        }
    }

    private sealed class Releaser : IDisposable
    {
        private readonly SemaphoreSlim _sem1;
        private readonly SemaphoreSlim _sem2;
        private bool _disposed;

        public Releaser(SemaphoreSlim sem1, SemaphoreSlim sem2)
        {
            _sem1 = sem1;
            _sem2 = sem2;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _sem2.Release();
                _sem1.Release();
                _disposed = true;
            }
        }
    }
}
