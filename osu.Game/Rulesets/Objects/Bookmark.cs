// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using Newtonsoft.Json;
using osu.Framework.Bindables;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Objects
{
    public class Bookmark : IComparable<Bookmark>
    {
        public readonly Bindable<double> TimePointBindable = new BindableDouble();

        /// <summary>
        /// The time point this Bookmark defines.
        /// </summary>
        public virtual double TimePoint
        {
            get => TimePointBindable.Value;
            set => TimePointBindable.Value = value;
        }

        public Bookmark(double timepoint)
        {
            this.TimePoint = timepoint;
        }

        public int CompareTo(Bookmark other)
        {
            return (int)(this.TimePoint - other.TimePoint);
        }
    }
}
