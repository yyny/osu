// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;

namespace osu.Game.Screens.Edit.Components.Menus
{
    internal class ToggleEditorMenuItem : StatefulEditorMenuItem<bool>
    {
        /// <summary>
        /// Creates a new <see cref="ToggleEditorMenuItem"/>.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="accelerator">The menu accelerator, or null.</param>
        /// <param name="type">The type of action which this <see cref="ToggleEditorMenuItem"/> performs.</param>
        public ToggleEditorMenuItem(string text, IAccelerator accelerator = null, MenuItemType type = MenuItemType.Standard)
            : this(text, accelerator, type, null)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ToggleEditorMenuItem"/>.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="accelerator">The menu accelerator, or null.</param>
        /// <param name="type">The type of action which this <see cref="ToggleEditorMenuItem"/> performs.</param>
        /// <param name="action">A delegate to be invoked when this <see cref="ToggleEditorMenuItem"/> is pressed.</param>
        public ToggleEditorMenuItem(string text, IAccelerator accelerator, MenuItemType type, Action<bool> action)
            : base(text, accelerator, value => !value, type, action)
        {
        }

        public override IconUsage? GetIconForState(bool state) => state ? (IconUsage?)FontAwesome.Solid.Check : null;
    }
}
