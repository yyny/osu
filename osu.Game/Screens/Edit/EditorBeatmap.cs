// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Skinning;
using osu.Game.Utils;

namespace osu.Game.Screens.Edit
{
    public class EditorBeatmap : TransactionalCommitComponent, IBeatmap, IBeatSnapProvider
    {
        /// <summary>
        /// Invoked when a <see cref="HitObject"/> is added to this <see cref="EditorBeatmap"/>.
        /// </summary>
        public event Action<HitObject> HitObjectAdded;

        /// <summary>
        /// Invoked when a <see cref="HitObject"/> is removed from this <see cref="EditorBeatmap"/>.
        /// </summary>
        public event Action<HitObject> HitObjectRemoved;

        /// <summary>
        /// Invoked when a <see cref="HitObject"/> is updated.
        /// </summary>
        public event Action<HitObject> HitObjectUpdated;

        /// <summary>
        /// All currently selected <see cref="HitObject"/>s.
        /// </summary>
        public readonly BindableList<HitObject> SelectedHitObjects = new BindableList<HitObject>();

        /// <summary>
        /// The current placement. Null if there's no active placement.
        /// </summary>
        public readonly Bindable<HitObject> PlacementObject = new Bindable<HitObject>();

        /// <summary>
        /// Invoked when a <see cref="Bookmark"/> is added to this <see cref="EditorBeatmap"/>.
        /// </summary>
        public event Action<Bookmark> BookmarkAdded;

        /// <summary>
        /// Invoked when a <see cref="Bookmark"/> is removed from this <see cref="EditorBeatmap"/>.
        /// </summary>
        public event Action<Bookmark> BookmarkRemoved;

        public readonly IBeatmap PlayableBeatmap;

        [CanBeNull]
        public readonly ISkin BeatmapSkin;

        [Resolved]
        private BindableBeatDivisor beatDivisor { get; set; }

        private readonly IBeatmapProcessor beatmapProcessor;

        private readonly Dictionary<HitObject, Bindable<double>> startTimeBindables = new Dictionary<HitObject, Bindable<double>>();

        public EditorBeatmap(IBeatmap playableBeatmap, ISkin beatmapSkin = null)
        {
            PlayableBeatmap = playableBeatmap;
            BeatmapSkin = beatmapSkin;
            bookmarks = new BalancedTree<Bookmark>(PlayableBeatmap.BeatmapInfo.Bookmarks.Select(time => new Bookmark(time)));

            beatmapProcessor = playableBeatmap.BeatmapInfo.Ruleset?.CreateInstance().CreateBeatmapProcessor(PlayableBeatmap);

            foreach (var obj in HitObjects)
                trackStartTime(obj);
        }

        public BeatmapInfo BeatmapInfo
        {
            get => PlayableBeatmap.BeatmapInfo;
            set => PlayableBeatmap.BeatmapInfo = value;
        }

        public BeatmapMetadata Metadata => PlayableBeatmap.Metadata;

        public ControlPointInfo ControlPointInfo
        {
            get => PlayableBeatmap.ControlPointInfo;
            set => PlayableBeatmap.ControlPointInfo = value;
        }

        public List<BreakPeriod> Breaks => PlayableBeatmap.Breaks;

        public double TotalBreakTime => PlayableBeatmap.TotalBreakTime;

        public IReadOnlyList<HitObject> HitObjects => PlayableBeatmap.HitObjects;

        public IEnumerable<BeatmapStatistic> GetStatistics() => PlayableBeatmap.GetStatistics();

        public double GetMostCommonBeatLength() => PlayableBeatmap.GetMostCommonBeatLength();

        public IBeatmap Clone() => (EditorBeatmap)MemberwiseClone();

        private IList mutableHitObjects => (IList)PlayableBeatmap.HitObjects;

        private BalancedTree<Bookmark> bookmarks;

        private readonly List<Bookmark> batchPendingBookmarkInserts = new List<Bookmark>();

        private readonly List<Bookmark> batchPendingBookmarkDeletes = new List<Bookmark>();

        private readonly List<HitObject> batchPendingInserts = new List<HitObject>();

        private readonly List<HitObject> batchPendingDeletes = new List<HitObject>();

        private readonly HashSet<HitObject> batchPendingUpdates = new HashSet<HitObject>();

        /// <summary>
        /// Adds a <see cref="Bookmark"/> to this <see cref="EditorBeatmap"/>.
        /// </summary>
        public void AddBookmark(Bookmark bookmark)
        {
            bookmarks.Add(bookmark);

            BeginChange();
            batchPendingBookmarkInserts.Add(bookmark);
            EndChange();
        }

        /// <summary>
        /// Removes a <see cref="Bookmark"/> to this <see cref="EditorBeatmap"/>.
        /// </summary>
        public void RemoveBookmark(Bookmark bookmark)
        {
            bookmarks.Remove(bookmark);

            BeginChange();
            batchPendingBookmarkDeletes.Add(bookmark);
            EndChange();
        }

        /// <summary>
        /// Removes the nearest <see cref="Bookmark"/> (within <paramref name="treshold"/> seconds) to <paramref name="time"/>
        /// </summary>
        public void RemoveNearestBookmark(double time, double treshold)
        {
            Bookmark bookmark = findNearestBookmark(time, treshold);
            if (bookmark != null)
            {
                RemoveBookmark(bookmark);
            }
        }

        public bool HasBookmarkNear(double time, double treshold)
        {
            Bookmark bookmark = findNearestBookmark(time, treshold);
            return bookmark != null;
        }

        private Bookmark findNearestBookmark(double time)
        {
            Bookmark nearest;
            // FIXME: This wastes an allocation for no reason
            if (!bookmarks.FindNearest(new Bookmark(time), out nearest))
                return null;
            return nearest;
        }
        private Bookmark findNearestBookmark(double time, double treshold)
        {
            Bookmark bookmark = findNearestBookmark(time);
            if (bookmark == null)
                return null;
            double distance = Math.Abs(bookmark.TimePoint - time);
            if (distance > treshold)
                return null;
            return bookmark;
        }

        /// <summary>
        /// Perform the provided action on every selected hitobject.
        /// Changes will be grouped as one history action.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        public void PerformOnSelection(Action<HitObject> action)
        {
            if (SelectedHitObjects.Count == 0)
                return;

            BeginChange();
            foreach (var h in SelectedHitObjects)
                action(h);
            EndChange();
        }

        /// <summary>
        /// Adds a collection of <see cref="HitObject"/>s to this <see cref="EditorBeatmap"/>.
        /// </summary>
        /// <param name="hitObjects">The <see cref="HitObject"/>s to add.</param>
        public void AddRange(IEnumerable<HitObject> hitObjects)
        {
            BeginChange();
            foreach (var h in hitObjects)
                Add(h);
            EndChange();
        }

        /// <summary>
        /// Adds a <see cref="HitObject"/> to this <see cref="EditorBeatmap"/>.
        /// </summary>
        /// <param name="hitObject">The <see cref="HitObject"/> to add.</param>
        public void Add(HitObject hitObject)
        {
            // Preserve existing sorting order in the beatmap
            var insertionIndex = findInsertionIndex(PlayableBeatmap.HitObjects, hitObject.StartTime);
            Insert(insertionIndex + 1, hitObject);
        }

        /// <summary>
        /// Inserts a <see cref="HitObject"/> into this <see cref="EditorBeatmap"/>.
        /// </summary>
        /// <remarks>
        /// It is the invoker's responsibility to make sure that <see cref="HitObject"/> sorting order is maintained.
        /// </remarks>
        /// <param name="index">The index to insert the <see cref="HitObject"/> at.</param>
        /// <param name="hitObject">The <see cref="HitObject"/> to insert.</param>
        public void Insert(int index, HitObject hitObject)
        {
            trackStartTime(hitObject);

            mutableHitObjects.Insert(index, hitObject);

            BeginChange();
            batchPendingInserts.Add(hitObject);
            EndChange();
        }

        /// <summary>
        /// Updates a <see cref="HitObject"/>, invoking <see cref="HitObject.ApplyDefaults"/> and re-processing the beatmap.
        /// </summary>
        /// <param name="hitObject">The <see cref="HitObject"/> to update.</param>
        public void Update([NotNull] HitObject hitObject)
        {
            // updates are debounced regardless of whether a batch is active.
            batchPendingUpdates.Add(hitObject);
        }

        /// <summary>
        /// Update all hit objects with potentially changed difficulty or control point data.
        /// </summary>
        public void UpdateAllHitObjects()
        {
            foreach (var h in HitObjects)
                batchPendingUpdates.Add(h);
        }

        /// <summary>
        /// Removes a <see cref="HitObject"/> from this <see cref="EditorBeatmap"/>.
        /// </summary>
        /// <param name="hitObject">The <see cref="HitObject"/> to remove.</param>
        /// <returns>True if the <see cref="HitObject"/> has been removed, false otherwise.</returns>
        public bool Remove(HitObject hitObject)
        {
            int index = FindIndex(hitObject);

            if (index == -1)
                return false;

            RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Removes a collection of <see cref="HitObject"/>s to this <see cref="EditorBeatmap"/>.
        /// </summary>
        /// <param name="hitObjects">The <see cref="HitObject"/>s to remove.</param>
        public void RemoveRange(IEnumerable<HitObject> hitObjects)
        {
            BeginChange();
            foreach (var h in hitObjects)
                Remove(h);
            EndChange();
        }

        /// <summary>
        /// Finds the index of a <see cref="HitObject"/> in this <see cref="EditorBeatmap"/>.
        /// </summary>
        /// <param name="hitObject">The <see cref="HitObject"/> to search for.</param>
        /// <returns>The index of <paramref name="hitObject"/>.</returns>
        public int FindIndex(HitObject hitObject) => mutableHitObjects.IndexOf(hitObject);

        /// <summary>
        /// Removes a <see cref="HitObject"/> at an index in this <see cref="EditorBeatmap"/>.
        /// </summary>
        /// <param name="index">The index of the <see cref="HitObject"/> to remove.</param>
        public void RemoveAt(int index)
        {
            var hitObject = (HitObject)mutableHitObjects[index];

            mutableHitObjects.RemoveAt(index);

            var bindable = startTimeBindables[hitObject];
            bindable.UnbindAll();
            startTimeBindables.Remove(hitObject);

            BeginChange();
            batchPendingDeletes.Add(hitObject);
            EndChange();
        }

        protected override void Update()
        {
            base.Update();

            if (batchPendingUpdates.Count > 0)
                UpdateState();
        }

        protected override void UpdateState()
        {
            if (batchPendingUpdates.Count == 0 && batchPendingDeletes.Count == 0 && batchPendingInserts.Count == 0 && batchPendingBookmarkDeletes.Count == 0 && batchPendingBookmarkInserts.Count == 0)
                return;

            beatmapProcessor?.PreProcess();

            foreach (var h in batchPendingDeletes) processHitObject(h);
            foreach (var h in batchPendingInserts) processHitObject(h);
            foreach (var h in batchPendingUpdates) processHitObject(h);

            beatmapProcessor?.PostProcess();

            // callbacks may modify the lists so let's be safe about it
            var deletes = batchPendingDeletes.ToArray();
            batchPendingDeletes.Clear();

            var inserts = batchPendingInserts.ToArray();
            batchPendingInserts.Clear();

            var updates = batchPendingUpdates.ToArray();
            batchPendingUpdates.Clear();

            var bookmarkDeletes = batchPendingBookmarkDeletes.ToArray();
            batchPendingBookmarkDeletes.Clear();

            var bookmarkInserts = batchPendingBookmarkInserts.ToArray();
            batchPendingBookmarkInserts.Clear();

            foreach (var h in deletes) HitObjectRemoved?.Invoke(h);
            foreach (var h in inserts) HitObjectAdded?.Invoke(h);
            foreach (var h in updates) HitObjectUpdated?.Invoke(h);

            foreach (var b in bookmarkDeletes) BookmarkRemoved?.Invoke(b);
            foreach (var b in bookmarkInserts) BookmarkAdded?.Invoke(b);

            updateBookmarks();
        }

        void updateBookmarks()
        {
            int[] array = new int[bookmarks.Count];
            int idx = 0;
            foreach (var bookmark in bookmarks) {
                array[idx++] = (int)bookmark.TimePoint;
            }
            PlayableBeatmap.BeatmapInfo.Bookmarks = array;
        }

        /// <summary>
        /// Clears all <see cref="HitObjects"/> from this <see cref="EditorBeatmap"/>.
        /// </summary>
        public void Clear() => RemoveRange(HitObjects.ToArray());

        private void processHitObject(HitObject hitObject) => hitObject.ApplyDefaults(ControlPointInfo, BeatmapInfo.BaseDifficulty);

        private void trackStartTime(HitObject hitObject)
        {
            startTimeBindables[hitObject] = hitObject.StartTimeBindable.GetBoundCopy();
            startTimeBindables[hitObject].ValueChanged += _ =>
            {
                // For now we'll remove and re-add the hitobject. This is not optimal and can be improved if required.
                mutableHitObjects.Remove(hitObject);

                var insertionIndex = findInsertionIndex(PlayableBeatmap.HitObjects, hitObject.StartTime);
                mutableHitObjects.Insert(insertionIndex + 1, hitObject);

                Update(hitObject);
            };
        }

        private int findInsertionIndex(IReadOnlyList<HitObject> list, double startTime)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].StartTime > startTime)
                    return i - 1;
            }

            return list.Count - 1;
        }

        public double SnapTime(double time, double? referenceTime) => ControlPointInfo.GetClosestSnappedTime(time, BeatDivisor, referenceTime);

        public double GetBeatLengthAtTime(double referenceTime) => ControlPointInfo.TimingPointAt(referenceTime).BeatLength / BeatDivisor;

        public int BeatDivisor => beatDivisor?.Value ?? 1;
    }
}
