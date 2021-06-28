// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Logging;
using osu.Game.Graphics;
using osu.Game.Screens.Edit.Components.Timelines.Summary.Visualisations;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Screens.Edit.Components.Timelines.Summary.Parts
{
    /// <summary>
    /// The part of the timeline that displays bookmarks.
    /// </summary>
    public class BookmarkPart : TimelinePart
    {

        private Dictionary<double, BookmarkVisualisation> visualizations = new Dictionary<double, BookmarkVisualisation>(); // TODO(yyny): Try to just store a Dictionary/BalancedTree in the first place.

        protected override void LoadBeatmap(EditorBeatmap beatmap)
        {
            base.LoadBeatmap(beatmap);
            visualizations.Clear();
            foreach (int bookmark in beatmap.BeatmapInfo.Bookmarks)
            {
                Logger.Log($"Bookmark at ${bookmark}ms");
            }
            foreach (int bookmark in beatmap.BeatmapInfo.Bookmarks)
            {
                var visualization = new BookmarkVisualisation(bookmark);
                if (visualizations.TryAdd((double)bookmark, visualization))
                    Add(visualization);
                else
                    Logger.Log($"Attempted to add a bookmark at {bookmark}ms, but a bookmark at that position already exists! Is the beatmap corrupted?", level: LogLevel.Error);
            }

            beatmap.BookmarkAdded += bookmark => {
                var visualization = new BookmarkVisualisation(bookmark.TimePoint);
                if (visualizations.TryAdd(bookmark.TimePoint, visualization))
                    Add(visualization);
            };
            beatmap.BookmarkRemoved += bookmark => {
                BookmarkVisualisation visualization;
                if (visualizations.TryGetValue(bookmark.TimePoint, out visualization)) {
                    Remove(visualization);
                    visualizations.Remove(bookmark.TimePoint);
                }
            };
        }

        private class BookmarkVisualisation : PointVisualisation
        {
            public BookmarkVisualisation(double startTime)
                : base(startTime)
            {
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours) => Colour = colours.Blue;
        }
    }
}
