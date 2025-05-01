namespace SimplifiedMatchingEngine;

public static class ReaderWriterLockSlimExtensions
{
    public static IDisposable EnterReadScope(this ReaderWriterLockSlim rwLock)
    {
        rwLock.EnterReadLock();
        return new Releaser(rwLock, LockType.Read);
    }

    public static IDisposable EnterWriteScope(this ReaderWriterLockSlim rwLock)
    {
        rwLock.EnterWriteLock();
        return new Releaser(rwLock, LockType.Write);
    }

    public static IDisposable EnterUpgradeableReadScope(this ReaderWriterLockSlim rwLock)
    {
        rwLock.EnterUpgradeableReadLock();
        return new Releaser(rwLock, LockType.UpgradeableRead);
    }

    private enum LockType { Read, Write, UpgradeableRead }

    private readonly struct Releaser(ReaderWriterLockSlim rwLock, ReaderWriterLockSlimExtensions.LockType type) : IDisposable
    {
        public void Dispose()
        {
            switch (type)
            {
                case LockType.Read:
                    rwLock.ExitReadLock();
                    break;
                case LockType.Write:
                    rwLock.ExitWriteLock();
                    break;
                case LockType.UpgradeableRead:
                    rwLock.ExitUpgradeableReadLock();
                    break;
            }
        }
    }
}
