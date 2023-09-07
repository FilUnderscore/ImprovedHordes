using GamePath;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;

namespace ImprovedHordes
{
    public abstract class ThreadSafeAStarPathFinderThread : PathFinderThread
    {
        private readonly ConcurrentQueue<int> waitQueue = new ConcurrentQueue<int>();
        private readonly ConcurrentDictionary<int, PathInfo> finishedPaths = new ConcurrentDictionary<int, PathInfo>();

        public ThreadSafeAStarPathFinderThread()
        {
            Instance = this;
        }

        protected abstract void Shutdown();

        public override void Cleanup()
        {
            this.Shutdown();
            while (this.waitQueue.TryDequeue(out _)) { }
            this.finishedPaths.Clear();
        }

        public override int GetFinishedCount() => this.finishedPaths.Count;

        public override int GetQueueCount() => this.waitQueue.Count;

        protected void Update()
        {
            while (waitQueue.TryDequeue(out int entityId))
            {
                if (this.finishedPaths.TryGetValue(entityId, out PathInfo pathInfo))
                {
                    if (pathInfo == null || pathInfo.entity == null || pathInfo.entity.navigator == null)
                    {
                        this.finishedPaths.TryRemove(entityId, out _);
                        continue;
                    }

                    pathInfo.entity.navigator.GetPathTo(pathInfo);

                    if (pathInfo.state == PathInfo.State.Queued)
                        this.finishedPaths.TryRemove(entityId, out _);
                }
            }
        }

        public override void FindPath(EntityAlive _entity, Vector3 _startPos, Vector3 _targetPos, float _speed, bool _canBreak, EAIBase _aiTask)
        {
            PathInfo pathInfo = null;
            if (this.finishedPaths.TryAdd(_entity.entityId, pathInfo = new PathInfo(_entity, _targetPos, _canBreak, _speed, _aiTask)))
            {
                pathInfo.SetStartPos(_startPos);
                this.waitQueue.Enqueue(_entity.entityId);
            }
        }

        public override void FindPath(EntityAlive _entity, Vector3 _targetPos, float _speed, bool _canBreak, EAIBase _aiTask)
        {
            if (this.finishedPaths.TryAdd(_entity.entityId, new PathInfo(_entity, _targetPos, _canBreak, _speed, _aiTask)))
                this.waitQueue.Enqueue(_entity.entityId);
        }

        public override PathInfo GetPath(int _entityId)
        {
            if (!this.finishedPaths.TryGetValue(_entityId, out PathInfo path) || path.state != PathInfo.State.Done)
                return PathInfo.Empty;

            this.finishedPaths.TryRemove(_entityId, out path);
            return path;
        }

        public override bool IsCalculatingPath(int _entityId)
        {
            return this.finishedPaths.ContainsKey(_entityId);
        }

        public override void RemovePathsFor(int _entityId)
        {
            this.finishedPaths.TryRemove(_entityId, out _);
        }
    }

    public sealed class ThreadedThreadSafeAStarPathFinderThread : ThreadSafeAStarPathFinderThread
    {
        private ThreadManager.ThreadInfo threadInfo;
        private bool running;

        public override void StartWorkerThreads()
        {
            this.threadInfo = ThreadManager.StartThread("IH-ThreadSafeAStarPathFinder", StartThread, LoopThread, EndThread, System.Threading.ThreadPriority.Lowest, null, null, false);
        }

        protected override void Shutdown()
        {
            this.running = false;
        }

        private void StartThread(ThreadManager.ThreadInfo threadInfo)
        {
            this.running = true;
        }

        private int LoopThread(ThreadManager.ThreadInfo threadInfo)
        {
            while (running)
            {
                this.Update();

                return 100;
            }

            return -1;
        }

        private void EndThread(ThreadManager.ThreadInfo threadInfo, bool exitForException)
        {
        }
    }

    public sealed class CoroutinedThreadSafeAStarPathFinderThread : ThreadSafeAStarPathFinderThread
    {
        private Coroutine coroutine;

        public override void StartWorkerThreads()
        {
            this.coroutine = GameManager.Instance.StartCoroutine(this.FindPaths());
        }

        private IEnumerator FindPaths()
        {
            while(true)
            {
                this.Update();
                yield return null;
            }
        }

        protected override void Shutdown()
        {
            GameManager.Instance.StopCoroutine(this.coroutine);
        }
    }
}
