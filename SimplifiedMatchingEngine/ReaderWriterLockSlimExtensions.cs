namespace SimplifiedMatchingEngine;

public static class ReaderWriterLockSlimExtensions
{
    private static readonly ThreadLocal<LockType?> inLock = new(() => null);

    private static IDisposable EnterLockScope(ReaderWriterLockSlim rwLock, LockType type)
    {
        if (inLock.Value.HasValue && inLock.Value >= type)
        {
            return DummyReleaser.Instance;
        }
        switch (type)
        {
            case LockType.Read:
                rwLock.EnterReadLock();
                break;
            case LockType.Write:
                rwLock.EnterWriteLock();
                break;
            case LockType.UpgradeableRead:
                rwLock.EnterUpgradeableReadLock();
                break;
        }
        inLock.Value = type;
        return new Releaser(rwLock, type);
    }

    public static IDisposable EnterReadScope(this ReaderWriterLockSlim rwLock) =>
        EnterLockScope(rwLock, LockType.Read);

    public static IDisposable EnterWriteScope(this ReaderWriterLockSlim rwLock) =>
        EnterLockScope(rwLock, LockType.Write);

    public static IDisposable EnterUpgradeableReadScope(this ReaderWriterLockSlim rwLock) =>
        EnterLockScope(rwLock, LockType.UpgradeableRead);

    private enum LockType { Read, UpgradeableRead, Write }

    private readonly struct Releaser(ReaderWriterLockSlim rwLock, LockType type) : IDisposable
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
            inLock.Value = null;
        }
    }

    private readonly struct DummyReleaser : IDisposable
    {
        public static readonly DummyReleaser Instance = new();
        public void Dispose() { }
    }
}
