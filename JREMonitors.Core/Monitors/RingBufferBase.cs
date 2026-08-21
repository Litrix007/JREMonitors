using System;

namespace JREMonitors.Core.Monitors
{
    public abstract class RingBufferBase : IDisposable
    {
        protected readonly int Length;
        protected int ActiveMapIndex = -1;
        protected bool IsFirstCall = true;
        protected int ReadIndex;
        protected bool SubmittedThisFrame;
        protected int WriteIndex;

        protected RingBufferBase(int length)
        {
            if (length < 1) throw new ArgumentOutOfRangeException(nameof(length), "Length must be at least 1.");
            Length = length;
            Reset();
        }

        public int UnmappedCount { get; protected set; }
        public abstract void Dispose();

        public void Reset()
        {
            WriteIndex = 0;
            ReadIndex = 0;
            UnmappedCount = 0;
            SubmittedThisFrame = false;
            IsFirstCall = true;
            ActiveMapIndex = -1;
        }

        protected int ResolveReadIndex()
        {
            int targetMapIndex;

            if (UnmappedCount == 0)
            {
                if (IsFirstCall)
                    targetMapIndex = 0;
                else
                    targetMapIndex = (ReadIndex - 1 + Length) % Length;
            }
            else
            {
                var shouldProgress = !SubmittedThisFrame || UnmappedCount >= Length;
                if (shouldProgress)
                {
                    targetMapIndex = ReadIndex;
                    ReadIndex = (ReadIndex + 1) % Length;
                    UnmappedCount--;
                    IsFirstCall = false;
                }
                else
                {
                    if (IsFirstCall)
                        targetMapIndex = 0;
                    else
                        targetMapIndex = (ReadIndex - 1 + Length) % Length;
                }
            }

            SubmittedThisFrame = false;
            return targetMapIndex;
        }
    }
}